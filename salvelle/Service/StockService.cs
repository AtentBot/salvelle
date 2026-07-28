using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Service;

public class StockService
{
    private readonly AppDbContext _context;

    public StockService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, StockMovement? Movement)> RegisterMovementAsync(
        Guid rawMaterialId,
        Guid establishmentId,
        string movementType,
        decimal quantity,
        Guid performedByEmployeeId,
        Guid? batchId = null,
        Guid? authorizedByEmployeeId = null,
        string? reason = null,
        Guid? manipulationOrderId = null,
        Guid? saleId = null,
        Guid? supplierId = null,
        string? documentNumber = null,
        string? prescriptionNumber = null,
        string? notificationNumber = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validar matéria-prima
            var rawMaterial = await _context.RawMaterials
                .FirstOrDefaultAsync(r => r.Id == rawMaterialId && r.EstablishmentId == establishmentId);

            if (rawMaterial == null)
                return (false, "Matéria-prima não encontrada", null);

            // Obter estoque atual
            var stockBefore = await GetCurrentStockAsync(rawMaterialId, batchId, establishmentId);

            // Calcular novo estoque
            decimal stockAfter = movementType switch
            {
                "ENTRADA" => stockBefore + quantity,
                "SAIDA" => stockBefore - quantity,
                "AJUSTE" => quantity, // Ajuste define valor absoluto
                "PERDA" => stockBefore - quantity,
                "VENCIMENTO" => stockBefore - quantity,
                "MANIPULACAO" => stockBefore - quantity,
                "VENDA" => stockBefore - quantity,
                _ => stockBefore
            };

            // Validar estoque suficiente para saídas
            if ((movementType == "SAIDA" || movementType == "PERDA" ||
                 movementType == "VENCIMENTO" || movementType == "MANIPULACAO" ||
                 movementType == "VENDA") && stockBefore < quantity)
            {
                return (false, $"Estoque insuficiente. Disponível: {stockBefore:N4} {rawMaterial.Unit}", null);
            }

            // Criar movimentação
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                RawMaterialId = rawMaterialId,
                BatchId = batchId,
                MovementType = movementType,
                Quantity = quantity,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = reason,
                ManipulationOrderId = manipulationOrderId,
                SaleId = saleId,
                SupplierId = supplierId,
                DocumentNumber = documentNumber,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = performedByEmployeeId,
                AuthorizedByEmployeeId = authorizedByEmployeeId,
                CreatedAt = DateTime.UtcNow,
                PrescriptionNumber = prescriptionNumber,
                NotificationNumber = notificationNumber
            };

            _context.StockMovements.Add(movement);

            // Atualizar batch se especificado
            if (batchId.HasValue)
            {
                var batch = await _context.Batches.FindAsync(batchId.Value);
                if (batch != null)
                {
                    batch.CurrentQuantity = stockAfter;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Verificar estoque baixo
            var alert = stockAfter <= rawMaterial.MinimumStock
                ? $" ALERTA: Estoque baixo ({stockAfter:N4} {rawMaterial.Unit})"
                : "";

            return (true, $"Movimentação registrada com sucesso.{alert}", movement);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao registrar movimentação: {ex.Message}", null);
        }
    }

    public async Task<decimal> GetCurrentStockAsync(Guid rawMaterialId, Guid? batchId, Guid establishmentId)
    {
        var query = _context.StockMovements
            .Where(m => m.RawMaterialId == rawMaterialId && m.EstablishmentId == establishmentId);

        if (batchId.HasValue)
            query = query.Where(m => m.BatchId == batchId.Value);

        var lastMovement = await query
            .OrderByDescending(m => m.MovementDate)
            .FirstOrDefaultAsync();

        return lastMovement?.StockAfter ?? 0;
    }

    public async Task<List<LowStockAlert>> GetLowStockItemsAsync(Guid establishmentId)
    {
        var rawMaterials = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .ToListAsync();

        var alerts = new List<LowStockAlert>();

        foreach (var rm in rawMaterials)
        {
            var currentStock = await GetCurrentStockAsync(rm.Id, null, establishmentId);

            if (currentStock <= rm.MinimumStock)
            {
                alerts.Add(new LowStockAlert
                {
                    RawMaterialId = rm.Id,
                    Name = rm.Name,
                    Unit = rm.Unit,
                    CurrentQuantity = currentStock,
                    MinimumStock = rm.MinimumStock,
                    MaximumStock = rm.MaximumStock,
                    IsOutOfStock = currentStock <= 0
                });
            }
        }

        return alerts.OrderBy(a => a.CurrentQuantity).ToList();
    }

    public async Task<(bool Success, string Message)> DeductStockForSaleAsync(
        Guid saleId,
        Guid establishmentId,
        Guid employeeId)
    {
        try
        {
            var saleItems = await _context.SaleItems
                .Where(si => si.SaleId == saleId)
                .ToListAsync();

            foreach (var item in saleItems)
            {
                if (item.ManipulationOrderId.HasValue)
                {
                    var order = await _context.ManipulationOrders
                        .Include(o => o.Formula)
                        .ThenInclude(f => f!.Components)
                        .ThenInclude(c => c.RawMaterial)
                        .FirstOrDefaultAsync(o => o.Id == item.ManipulationOrderId);

                    if (order?.Formula?.Components != null)
                    {
                        foreach (var component in order.Formula.Components)
                        {
                            var quantityNeeded = component.Quantity * item.Quantity;

                            var result = await RegisterMovementAsync(
                                rawMaterialId: component.RawMaterialId,
                                establishmentId: establishmentId,
                                movementType: "VENDA",
                                quantity: quantityNeeded,
                                performedByEmployeeId: employeeId,
                                saleId: saleId,
                                reason: $"Venda #{saleId} - Manipulação {order.OrderNumber}"
                            );

                            if (!result.Success)
                                return (false, $"Erro ao deduzir {component.RawMaterial?.Name}: {result.Message}");
                        }
                    }
                }
            }

            return (true, "Estoque deduzido com sucesso");
        }
        catch (Exception ex)
        {
            return (false, $"Erro ao deduzir estoque: {ex.Message}");
        }
    }

    public async Task<List<StockMovement>> GetMovementHistoryAsync(
        Guid? rawMaterialId,
        Guid establishmentId,
        DateTime? startDate,
        DateTime? endDate,
        string? movementType = null)
    {
        var query = _context.StockMovements
            .Include(m => m.RawMaterial)
            .Include(m => m.Batch)
            .Include(m => m.PerformedByEmployee)
            .Include(m => m.AuthorizedByEmployee)
            .Where(m => m.EstablishmentId == establishmentId);

        if (rawMaterialId.HasValue)
            query = query.Where(m => m.RawMaterialId == rawMaterialId);

        if (startDate.HasValue)
            query = query.Where(m => m.MovementDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(m => m.MovementDate <= endDate.Value);

        if (!string.IsNullOrEmpty(movementType))
            query = query.Where(m => m.MovementType == movementType);

        return await query
            .OrderByDescending(m => m.MovementDate)
            .ToListAsync();
    }
}

public class LowStockAlert
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsOutOfStock { get; set; }
}