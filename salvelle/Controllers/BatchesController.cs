using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Controllers.API;

[ApiController]
[Route("api/[controller]")]
public class BatchesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BatchesController(AppDbContext context)
    {
        _context = context;
    }

    // ========== CRUD BÁSICO ==========

    // GET /api/Batches
    [HttpGet]
    public async Task<IActionResult> GetBatches(
        [FromQuery] string? status,
        [FromQuery] DateTime? expiryFrom,
        [FromQuery] DateTime? expiryTo,
        [FromQuery] Guid? rawMaterialId,
        [FromQuery] Guid? supplierId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            // Query base com filtro de estabelecimento
            var query = _context.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.Supplier)
                .Include(b => b.CreatedByEmployee)
                .Include(b => b.ApprovedByEmployee)
                .Where(b => b.RawMaterial!.EstablishmentId == establishmentId.Value)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(status))
                query = query.Where(b => b.Status == status.ToUpper());

            if (expiryFrom.HasValue)
                query = query.Where(b => b.ExpiryDate >= expiryFrom.Value);

            if (expiryTo.HasValue)
                query = query.Where(b => b.ExpiryDate <= expiryTo.Value);

            if (rawMaterialId.HasValue)
                query = query.Where(b => b.RawMaterialId == rawMaterialId.Value);

            if (supplierId.HasValue)
                query = query.Where(b => b.SupplierId == supplierId.Value);

            if (!string.IsNullOrEmpty(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(b =>
                    b.BatchNumber.ToLower().Contains(searchLower) ||
                    b.InvoiceNumber.ToLower().Contains(searchLower) ||
                    (b.RawMaterial != null && b.RawMaterial.Name.ToLower().Contains(searchLower)) ||
                    (b.Supplier != null && b.Supplier.CompanyName.ToLower().Contains(searchLower))
                );
            }

            var totalCount = await query.CountAsync();

            var batches = await query
                .OrderByDescending(b => b.ReceivedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new
                {
                    b.Id,
                    b.RawMaterialId,
                    RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : "",
                    RawMaterialUnit = b.RawMaterial != null ? b.RawMaterial.Unit : "",
                    b.SupplierId,
                    SupplierName = b.Supplier != null ? b.Supplier.CompanyName : "",
                    b.BatchNumber,
                    b.InvoiceNumber,
                    b.ReceivedQuantity,
                    b.CurrentQuantity,
                    b.UnitCost,
                    TotalCost = b.ReceivedQuantity * b.UnitCost,
                    b.ReceivedDate,
                    b.ExpiryDate,
                    b.ManufactureDate,
                    b.Status,
                    StatusLabel = GetStatusLabel(b.Status),
                    b.CertificateNumber,
                    b.ApprovalDate,
                    ApprovedByEmployeeName = b.ApprovedByEmployee != null ? b.ApprovedByEmployee.FullName : null,
                    b.QualityNotes,
                    b.CreatedAt,
                    CreatedByEmployeeName = b.CreatedByEmployee != null ? b.CreatedByEmployee.FullName : "",
                    DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days,
                    IsExpired = b.ExpiryDate < DateTime.UtcNow,
                    IsNearExpiry = b.ExpiryDate < DateTime.UtcNow.AddDays(30),
                    IsLowStock = b.CurrentQuantity <= 0,
                    UsagePercentage = b.ReceivedQuantity > 0 ? ((b.ReceivedQuantity - b.CurrentQuantity) / b.ReceivedQuantity) * 100 : 0
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = batches,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao carregar lotes: {ex.Message}" });
        }
    }

    // GET /api/Batches/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBatch(Guid id)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var batch = await _context.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.Supplier)
                .Include(b => b.CreatedByEmployee)
                .Include(b => b.ApprovedByEmployee)
                .Where(b => b.Id == id && b.RawMaterial!.EstablishmentId == establishmentId.Value)
                .Select(b => new
                {
                    b.Id,
                    b.RawMaterialId,
                    RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : "",
                    RawMaterialUnit = b.RawMaterial != null ? b.RawMaterial.Unit : "",
                    b.SupplierId,
                    SupplierName = b.Supplier != null ? b.Supplier.CompanyName : "",
                    b.BatchNumber,
                    b.InvoiceNumber,
                    b.ReceivedQuantity,
                    b.CurrentQuantity,
                    b.UnitCost,
                    TotalCost = b.ReceivedQuantity * b.UnitCost,
                    b.ReceivedDate,
                    b.ExpiryDate,
                    b.ManufactureDate,
                    b.Status,
                    StatusLabel = GetStatusLabel(b.Status),
                    b.CertificateNumber,
                    b.ApprovalDate,
                    b.ApprovedByEmployeeId,
                    ApprovedByEmployeeName = b.ApprovedByEmployee != null ? b.ApprovedByEmployee.FullName : null,
                    b.QualityNotes,
                    b.CreatedAt,
                    b.CreatedByEmployeeId,
                    CreatedByEmployeeName = b.CreatedByEmployee != null ? b.CreatedByEmployee.FullName : "",
                    DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days,
                    IsExpired = b.ExpiryDate < DateTime.UtcNow,
                    IsNearExpiry = b.ExpiryDate < DateTime.UtcNow.AddDays(30),
                    IsLowStock = b.CurrentQuantity <= 0,
                    UsagePercentage = b.ReceivedQuantity > 0 ? ((b.ReceivedQuantity - b.CurrentQuantity) / b.ReceivedQuantity) * 100 : 0
                })
                .FirstOrDefaultAsync();

            if (batch == null)
                return NotFound(new { success = false, message = "Lote não encontrado" });

            return Ok(new { success = true, data = batch });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao carregar lote: {ex.Message}" });
        }
    }

    // POST /api/Batches
    [HttpPost]
    public async Task<IActionResult> CreateBatch([FromBody] CreateBatchDto dto)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE", "AUXILIAR" });
            if (!hasPermission)
                return Forbid();

            // Validações
            if (string.IsNullOrWhiteSpace(dto.BatchNumber))
                return BadRequest(new { message = "Número do lote é obrigatório" });

            if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                return BadRequest(new { message = "Número da nota fiscal é obrigatório" });

            if (dto.ReceivedQuantity <= 0)
                return BadRequest(new { message = "Quantidade recebida deve ser maior que zero" });

            if (dto.UnitCost < 0)
                return BadRequest(new { message = "Custo unitário não pode ser negativo" });

            if (dto.ExpiryDate <= DateTime.UtcNow)
                return BadRequest(new { message = "Data de validade deve ser futura" });

            var rawMaterial = await _context.RawMaterials
                .FirstOrDefaultAsync(rm => rm.Id == dto.RawMaterialId && rm.EstablishmentId == establishmentId.Value);

            if (rawMaterial == null)
                return NotFound(new { message = "Matéria-prima não encontrada" });

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.EstablishmentId == establishmentId.Value);

            if (supplier == null)
                return NotFound(new { message = "Fornecedor não encontrado" });

            var existingBatch = await _context.Batches
                .FirstOrDefaultAsync(b => b.BatchNumber == dto.BatchNumber && b.RawMaterialId == dto.RawMaterialId);

            if (existingBatch != null)
                return BadRequest(new { message = "Já existe um lote com este número para esta matéria-prima" });

            var batch = new Batch
            {
                Id = Guid.NewGuid(),
                RawMaterialId = dto.RawMaterialId,
                SupplierId = dto.SupplierId,
                BatchNumber = dto.BatchNumber.Trim(),
                InvoiceNumber = dto.InvoiceNumber.Trim(),
                ReceivedQuantity = dto.ReceivedQuantity,
                CurrentQuantity = dto.ReceivedQuantity,
                UnitCost = dto.UnitCost,
                ReceivedDate = dto.ReceivedDate,
                ExpiryDate = dto.ExpiryDate,
                ManufactureDate = dto.ManufactureDate,
                Status = "QUARENTENA",
                CertificateNumber = dto.CertificateNumber?.Trim(),
                QualityNotes = dto.QualityNotes?.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId.Value
            };

            _context.Batches.Add(batch);

            // Registrar movimentação de entrada
            var stockBefore = rawMaterial.CurrentStock;
            var stockAfter = stockBefore + dto.ReceivedQuantity;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId.Value,
                RawMaterialId = dto.RawMaterialId,
                BatchId = batch.Id,
                MovementType = "ENTRADA",
                Quantity = dto.ReceivedQuantity,
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = $"Recebimento NF {dto.InvoiceNumber}",
                DocumentNumber = dto.InvoiceNumber,
                SupplierId = dto.SupplierId,
                MovementDate = dto.ReceivedDate,
                PerformedByEmployeeId = employeeId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);
            rawMaterial.CurrentStock = stockAfter;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Lote criado com sucesso",
                data = new { batch.Id, batch.BatchNumber, batch.Status }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao criar lote: {ex.Message}" });
        }
    }

    // PUT /api/Batches/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBatch(Guid id, [FromBody] UpdateBatchDto dto)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE" });
            if (!hasPermission)
                return Forbid();

            var batch = await _context.Batches
                .Include(b => b.RawMaterial)
                .FirstOrDefaultAsync(b => b.Id == id && b.RawMaterial!.EstablishmentId == establishmentId.Value);

            if (batch == null)
                return NotFound(new { message = "Lote não encontrado" });

            if (batch.Status == "APROVADO" || batch.Status == "REPROVADO")
                return BadRequest(new { message = "Não é possível editar lotes aprovados ou reprovados" });

            if (!string.IsNullOrWhiteSpace(dto.CertificateNumber))
                batch.CertificateNumber = dto.CertificateNumber.Trim();

            if (!string.IsNullOrWhiteSpace(dto.QualityNotes))
                batch.QualityNotes = dto.QualityNotes.Trim();

            if (dto.ExpiryDate.HasValue && dto.ExpiryDate.Value > DateTime.UtcNow)
                batch.ExpiryDate = dto.ExpiryDate.Value;

            if (dto.ManufactureDate.HasValue)
                batch.ManufactureDate = dto.ManufactureDate.Value;

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Lote atualizado com sucesso" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao atualizar lote: {ex.Message}" });
        }
    }

    // DELETE /api/Batches/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBatch(Guid id)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var hasPermission = await HasPermission(employeeId.Value, new[] { "GERENTE" });
            if (!hasPermission)
                return Forbid();

            var batch = await _context.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.StockMovements)
                .FirstOrDefaultAsync(b => b.Id == id && b.RawMaterial!.EstablishmentId == establishmentId.Value);

            if (batch == null)
                return NotFound(new { message = "Lote não encontrado" });

            var hasOtherMovements = batch.StockMovements != null && batch.StockMovements.Count > 1;
            if (hasOtherMovements)
                return BadRequest(new { message = "Não é possível deletar lotes com movimentações registradas" });

            if (batch.Status == "APROVADO")
                return BadRequest(new { message = "Não é possível deletar lotes aprovados" });

            if (batch.RawMaterial != null)
                batch.RawMaterial.CurrentStock -= batch.CurrentQuantity;

            if (batch.StockMovements != null)
                _context.StockMovements.RemoveRange(batch.StockMovements);

            _context.Batches.Remove(batch);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Lote deletado com sucesso" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao deletar lote: {ex.Message}" });
        }
    }

    // ========== ENDPOINTS ESPECIAIS ==========

    // POST /api/Batches/{id}/adjust-quantity
    [HttpPost("{id}/adjust-quantity")]
    public async Task<IActionResult> AdjustBatchQuantity(Guid id, [FromBody] AdjustQuantityDto dto)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var hasPermission = await HasPermission(employeeId.Value, new[] { "FARMACEUTICO_RT", "GERENTE" });
            if (!hasPermission)
                return Forbid();

            var batch = await _context.Batches
                .Include(b => b.RawMaterial)
                .FirstOrDefaultAsync(b => b.Id == id && b.RawMaterial!.EstablishmentId == establishmentId.Value);

            if (batch == null)
                return NotFound(new { message = "Lote não encontrado" });

            if (batch.Status != "APROVADO")
                return BadRequest(new { message = "Apenas lotes aprovados podem ter ajustes" });

            var newQuantity = batch.CurrentQuantity + dto.Adjustment;

            if (newQuantity < 0)
                return BadRequest(new { message = "Quantidade resultante não pode ser negativa" });

            if (newQuantity > batch.ReceivedQuantity)
                return BadRequest(new { message = "Quantidade resultante não pode exceder quantidade recebida" });

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { message = "Motivo do ajuste é obrigatório" });

            var previousQuantity = batch.CurrentQuantity;
            batch.CurrentQuantity = newQuantity;

            // Registrar em QualityNotes
            var adjustmentNote = $"[AJUSTE {DateTime.UtcNow:dd/MM/yyyy HH:mm}] {dto.Adjustment:+0.####;-0.####} - {dto.Reason}";
            batch.QualityNotes = string.IsNullOrWhiteSpace(batch.QualityNotes)
                ? adjustmentNote
                : $"{batch.QualityNotes}\n{adjustmentNote}";

            // Criar movimentação
            var stockBefore = batch.RawMaterial!.CurrentStock;
            var stockAfter = stockBefore + dto.Adjustment;

            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId.Value,
                RawMaterialId = batch.RawMaterialId,
                BatchId = batch.Id,
                MovementType = dto.Adjustment > 0 ? "AJUSTE" : "AJUSTE",
                Quantity = Math.Abs(dto.Adjustment),
                StockBefore = stockBefore,
                StockAfter = stockAfter,
                Reason = dto.Reason,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = employeeId.Value,
                CreatedAt = DateTime.UtcNow
            };

            _context.StockMovements.Add(movement);

            // Ajustar estoque da matéria-prima
            batch.RawMaterial.CurrentStock = stockAfter;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Quantidade ajustada com sucesso",
                data = new
                {
                    previousQuantity,
                    currentQuantity = batch.CurrentQuantity,
                    adjustment = dto.Adjustment
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao ajustar quantidade: {ex.Message}" });
        }
    }

    // GET /api/Batches/stats
    [HttpGet("stats")]
    public async Task<IActionResult> GetBatchStats()
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var query = _context.Batches
                .Include(b => b.RawMaterial)
                .Where(b => b.RawMaterial!.EstablishmentId == establishmentId.Value);

            var totalBatches = await query.CountAsync();
            var quarantineBatches = await query.CountAsync(b => b.Status == "QUARENTENA");
            var approvedBatches = await query.CountAsync(b => b.Status == "APROVADO");
            var rejectedBatches = await query.CountAsync(b => b.Status == "REPROVADO");
            var expiredBatches = await query.CountAsync(b => b.ExpiryDate < DateTime.UtcNow);

            var nearExpirationDate = DateTime.UtcNow.AddDays(90);
            var nearExpirationBatches = await query.CountAsync(b =>
                b.ExpiryDate <= nearExpirationDate && b.ExpiryDate >= DateTime.UtcNow);

            var totalInventoryValue = await query
                .Where(b => b.Status == "APROVADO")
                .SumAsync(b => (decimal?)b.CurrentQuantity * b.UnitCost) ?? 0;

            var batchesByStatus = await query
                .GroupBy(b => b.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalBatches,
                    quarantineBatches,
                    approvedBatches,
                    rejectedBatches,
                    expiredBatches,
                    nearExpirationBatches,
                    totalInventoryValue,
                    batchesByStatus
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao carregar estatísticas: {ex.Message}" });
        }
    }

    // GET /api/Batches/expiring
    [HttpGet("expiring")]
    public async Task<IActionResult> GetExpiringBatches([FromQuery] int days = 90)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var thresholdDate = DateTime.UtcNow.AddDays(days);

            var batches = await _context.Batches
                .Include(b => b.RawMaterial)
                .Include(b => b.Supplier)
                .Where(b => b.RawMaterial!.EstablishmentId == establishmentId.Value &&
                           b.Status == "APROVADO" &&
                           b.ExpiryDate <= thresholdDate &&
                           b.ExpiryDate >= DateTime.UtcNow &&
                           b.CurrentQuantity > 0)
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    RawMaterialName = b.RawMaterial != null ? b.RawMaterial.Name : "",
                    SupplierName = b.Supplier != null ? b.Supplier.CompanyName : "",
                    b.ExpiryDate,
                    b.CurrentQuantity,
                    DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = batches,
                count = batches.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao carregar lotes vencendo: {ex.Message}" });
        }
    }

    // GET /api/Batches/by-raw-material/{rawMaterialId}
    [HttpGet("by-raw-material/{rawMaterialId}")]
    public async Task<IActionResult> GetBatchesByRawMaterial(
        Guid rawMaterialId,
        [FromQuery] bool onlyAvailable = false)
    {
        try
        {
            var employeeId = GetEmployeeId();
            if (!employeeId.HasValue)
                return Unauthorized(new { message = "Sessão inválida" });

            var establishmentId = await GetEstablishmentId(employeeId.Value);
            if (!establishmentId.HasValue)
                return NotFound(new { message = "Estabelecimento não encontrado" });

            var query = _context.Batches
                .Include(b => b.RawMaterial)
                .Where(b => b.RawMaterialId == rawMaterialId &&
                           b.RawMaterial!.EstablishmentId == establishmentId.Value);

            if (onlyAvailable)
            {
                query = query.Where(b => b.Status == "APROVADO" &&
                                        b.ExpiryDate > DateTime.UtcNow &&
                                        b.CurrentQuantity > 0);
            }

            var batches = await query
                .OrderBy(b => b.ExpiryDate)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.ExpiryDate,
                    b.CurrentQuantity,
                    b.Status,
                    StatusLabel = GetStatusLabel(b.Status),
                    DaysUntilExpiry = (b.ExpiryDate - DateTime.UtcNow).Days,
                    IsExpired = b.ExpiryDate < DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = batches,
                count = batches.Count
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Erro ao carregar lotes: {ex.Message}" });
        }
    }

    // ========== HELPER METHODS ==========

    private Guid? GetEmployeeId()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = _context.EmployeeSessions
            .FirstOrDefault(s => s.Token == sessionToken &&
                                s.ExpiresAt > DateTime.UtcNow &&
                                s.IsActive);

        return session?.EmployeeId;
    }

    private async Task<Guid?> GetEstablishmentId(Guid employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        return employee?.EstablishmentId;
    }

    private async Task<bool> HasPermission(Guid employeeId, string[] allowedPositions)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.JobPosition == null)
            return false;

        return allowedPositions.Contains(employee.JobPosition.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static string GetStatusLabel(string status)
    {
        return status switch
        {
            "QUARENTENA" => "Quarentena",
            "APROVADO" => "Aprovado",
            "REPROVADO" => "Reprovado",
            "VENCIDO" => "Vencido",
            _ => status
        };
    }
}

// ========== DTOs ==========

public class CreateBatchDto
{
    public Guid RawMaterialId { get; set; }
    public Guid SupplierId { get; set; }
    public string BatchNumber { get; set; } = default!;
    public string InvoiceNumber { get; set; } = default!;
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime ReceivedDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? QualityNotes { get; set; }
}

public class UpdateBatchDto
{
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? QualityNotes { get; set; }
}

public class AdjustQuantityDto
{
    public decimal Adjustment { get; set; }
    public string Reason { get; set; } = default!;
}