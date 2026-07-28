using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.BatchQuality;
using Models.Pharmacy;

namespace Service.BatchQuality;

public class BatchQualityService
{
    private readonly AppDbContext _context;

    public BatchQualityService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message)> ApproveBatchAsync(
        Guid batchId,
        ApproveBatchDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var batch = await _context.Batches
            .Include(b => b.RawMaterial)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        if (batch == null)
            return (false, "Lote não encontrado");

        if (batch.RawMaterial?.EstablishmentId != establishmentId)
            return (false, "Lote não pertence a este estabelecimento");

        if (batch.Status?.ToUpper() != "QUARENTENA")
            return (false, $"Lote não pode ser aprovado no status {batch.Status}");

        if (batch.ExpiryDate <= DateTime.UtcNow)
            return (false, "Lote vencido não pode ser aprovado");

        batch.Status = "APROVADO";
        batch.CertificateNumber = dto.CertificateNumber;
        batch.QualityNotes = dto.QualityNotes;
        batch.ApprovalDate = DateTime.UtcNow;
        batch.ApprovedByEmployeeId = employeeId;

        await _context.SaveChangesAsync();
        return (true, "Lote aprovado com sucesso");
    }

    public async Task<(bool Success, string Message)> RejectBatchAsync(
        Guid batchId,
        RejectBatchDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var batch = await _context.Batches
                .Include(b => b.RawMaterial)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
                return (false, "Lote não encontrado");

            if (batch.RawMaterial?.EstablishmentId != establishmentId)
                return (false, "Lote não pertence a este estabelecimento");

            if (batch.Status?.ToUpper() != "QUARENTENA")
                return (false, $"Lote não pode ser reprovado no status {batch.Status}");

            batch.Status = "REPROVADO";
            batch.QualityNotes = $"REPROVADO: {dto.Reason}";
            if (!string.IsNullOrWhiteSpace(dto.QualityNotes))
                batch.QualityNotes += $" | {dto.QualityNotes}";
            batch.ApprovalDate = DateTime.UtcNow;
            batch.ApprovedByEmployeeId = employeeId;
            batch.CurrentQuantity = 0;

            var stockMovement = new StockMovement
            {
                BatchId = batch.Id,
                EstablishmentId = establishmentId,
                RawMaterialId = batch.RawMaterialId,
                MovementType = "PERDA",
                Quantity = batch.ReceivedQuantity,
                StockBefore = batch.ReceivedQuantity,
                StockAfter = 0,
                MovementDate = DateTime.UtcNow,
                Reason = $"Lote reprovado: {dto.Reason}",
                PerformedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(stockMovement);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Lote reprovado e descartado");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao reprovar lote: {ex.Message}");
        }
    }

    public async Task<QuarantineSummaryDto> GetQuarantineSummaryAsync(Guid establishmentId)
    {
        var batchesList = await _context.Batches
            .Include(b => b.RawMaterial)
            .Include(b => b.Supplier)
            .Include(b => b.CreatedByEmployee)
            .Where(b => b.RawMaterial!.EstablishmentId == establishmentId &&
                       b.Status.ToUpper() == "QUARENTENA")
            .Select(b => new BatchQualityResponseDto
            {
                Id = b.Id,
                RawMaterialId = b.RawMaterialId,
                RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : "",
                SupplierId = b.SupplierId,
                SupplierName = b.Supplier != null ? b.Supplier.CompanyName : "",
                BatchNumber = b.BatchNumber,
                InvoiceNumber = b.InvoiceNumber,
                ReceivedQuantity = b.ReceivedQuantity,
                CurrentQuantity = b.CurrentQuantity,
                UnitCost = b.UnitCost,
                ReceivedDate = b.ReceivedDate,
                ExpiryDate = b.ExpiryDate,
                ManufactureDate = b.ManufactureDate,
                Status = b.Status,
                CertificateNumber = b.CertificateNumber,
                ApprovalDate = b.ApprovalDate,
                QualityNotes = b.QualityNotes,
                CreatedAt = b.CreatedAt,
                CreatedByEmployeeName = b.CreatedByEmployee != null ? b.CreatedByEmployee.FullName : "",
                DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days
            })
            .OrderBy(b => b.ExpiryDate)
            .ToListAsync();

        var expiringIn30Days = batchesList.Count(b => b.DaysUntilExpiry <= 30 && b.DaysUntilExpiry > 0);
        var expiringIn60Days = batchesList.Count(b => b.DaysUntilExpiry <= 60 && b.DaysUntilExpiry > 30);
        var totalValue = batchesList.Sum(b => b.CurrentQuantity * b.UnitCost);

        return new QuarantineSummaryDto
        {
            TotalBatches = batchesList.Count,  // ? CORRIGIDO
            ExpiringIn30Days = expiringIn30Days,
            ExpiringIn60Days = expiringIn60Days,
            TotalValue = totalValue,
            Batches = batchesList  // ? CORRIGIDO
        };
    }
}
