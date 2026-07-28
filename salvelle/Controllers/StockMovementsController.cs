using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using DTOs;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockMovementsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<StockMovementsController> _logger;

    public StockMovementsController(AppDbContext db, ILogger<StockMovementsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // GET: api/stockmovements
    [HttpGet]
    public async Task<ActionResult<StockMovementListResponse>> GetMovements(
        [FromQuery] Guid? establishmentId,
        [FromQuery] Guid? rawMaterialId,
        [FromQuery] Guid? batchId,
        [FromQuery] string? movementType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] Guid? employeeId,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 100) pageSize = 100;

            var query = _db.StockMovements
                .Include(sm => sm.RawMaterial)
                .Include(sm => sm.Batch)
                .Include(sm => sm.PerformedByEmployee)
                .Include(sm => sm.AuthorizedByEmployee)
                .AsQueryable();

            if (establishmentId.HasValue)
                query = query.Where(sm => sm.EstablishmentId == establishmentId.Value);

            if (rawMaterialId.HasValue)
                query = query.Where(sm => sm.RawMaterialId == rawMaterialId.Value);

            if (batchId.HasValue)
                query = query.Where(sm => sm.BatchId == batchId.Value);

            if (!string.IsNullOrWhiteSpace(movementType))
                query = query.Where(sm => sm.MovementType == movementType.ToUpper());

            if (startDate.HasValue)
                query = query.Where(sm => sm.MovementDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sm => sm.MovementDate <= endDate.Value);

            if (employeeId.HasValue)
                query = query.Where(sm => sm.PerformedByEmployeeId == employeeId.Value || sm.AuthorizedByEmployeeId == employeeId.Value);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(sm =>
                    sm.RawMaterial!.Name.ToLower().Contains(searchTerm) ||

                    (sm.Batch != null && sm.Batch.BatchNumber.ToLower().Contains(searchTerm)) ||
                    (sm.DocumentNumber != null && sm.DocumentNumber.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var movements = await query
                .OrderByDescending(sm => sm.MovementDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    EstablishmentId = sm.EstablishmentId,
                    RawMaterialId = sm.RawMaterialId,
                    RawMaterialName = sm.RawMaterial!.Name,
                    BatchId = sm.BatchId,
                    BatchNumber = sm.Batch != null ? sm.Batch.BatchNumber : null,
                    MovementType = sm.MovementType,
                    Quantity = sm.Quantity,
                    StockBefore = sm.StockBefore,
                    StockAfter = sm.StockAfter,
                    Reason = sm.Reason,
                    ManipulationOrderId = sm.ManipulationOrderId,
                    SaleId = sm.SaleId,
                    SupplierId = sm.SupplierId,
                    DocumentNumber = sm.DocumentNumber,
                    MovementDate = sm.MovementDate,
                    PerformedByEmployeeId = sm.PerformedByEmployeeId,
                    PerformedByEmployeeName = sm.PerformedByEmployee != null ? sm.PerformedByEmployee.FullName : null,
                    AuthorizedByEmployeeId = sm.AuthorizedByEmployeeId,
                    AuthorizedByEmployeeName = sm.AuthorizedByEmployee != null ? sm.AuthorizedByEmployee.FullName : null,
                    CreatedAt = sm.CreatedAt,
                    PrescriptionNumber = sm.PrescriptionNumber,
                    NotificationNumber = sm.NotificationNumber
                })
                .ToListAsync();

            return Ok(new StockMovementListResponse
            {
                Movements = movements,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock movements");
            return StatusCode(500, new { message = "Error retrieving movements", error = ex.Message });
        }
    }

    // GET: api/stockmovements/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<StockMovementDto>> GetMovement(Guid id)
    {
        try
        {
            var movement = await _db.StockMovements
                .Include(sm => sm.RawMaterial)
                .Include(sm => sm.Batch)
                .Include(sm => sm.PerformedByEmployee)
                .Include(sm => sm.AuthorizedByEmployee)
                .FirstOrDefaultAsync(sm => sm.Id == id);

            if (movement == null)
                return NotFound(new { message = "Movement not found" });

            return Ok(new StockMovementDto
            {
                Id = movement.Id,
                EstablishmentId = movement.EstablishmentId,
                RawMaterialId = movement.RawMaterialId,
                RawMaterialName = movement.RawMaterial?.Name,
                BatchId = movement.BatchId,
                BatchNumber = movement.Batch?.BatchNumber,
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                StockBefore = movement.StockBefore,
                StockAfter = movement.StockAfter,
                Reason = movement.Reason,
                ManipulationOrderId = movement.ManipulationOrderId,
                SaleId = movement.SaleId,
                SupplierId = movement.SupplierId,
                DocumentNumber = movement.DocumentNumber,
                MovementDate = movement.MovementDate,
                PerformedByEmployeeId = movement.PerformedByEmployeeId,
                PerformedByEmployeeName = movement.PerformedByEmployee?.FullName,
                AuthorizedByEmployeeId = movement.AuthorizedByEmployeeId,
                AuthorizedByEmployeeName = movement.AuthorizedByEmployee?.FullName,
                CreatedAt = movement.CreatedAt,
                PrescriptionNumber = movement.PrescriptionNumber,
                NotificationNumber = movement.NotificationNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movement {MovementId}", id);
            return StatusCode(500, new { message = "Error retrieving movement", error = ex.Message });
        }
    }

    // POST: api/stockmovements/entrada
    [HttpPost("entrada")]
    public async Task<IActionResult> EntradaEstoque(
        [FromBody] EntradaEstoqueRequest request,
        [FromQuery] Guid employeeId,
        [FromQuery] Guid establishmentId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var material = await _db.RawMaterials.FindAsync(request.RawMaterialId);
            if (material == null)
                return NotFound(new { message = "Raw material not found" });

            var supplier = await _db.Suppliers.FindAsync(request.SupplierId);
            if (supplier == null)
                return NotFound(new { message = "Supplier not found" });

            // Verificar duplicidade de lote
            var existingBatch = await _db.Batches
                .AnyAsync(b => b.BatchNumber == request.BatchNumber && b.RawMaterialId == request.RawMaterialId);

            if (existingBatch)
                return BadRequest(new { message = "Batch number already exists for this raw material" });

            // Criar lote
            var batch = new Batch
            {
                Id = Guid.NewGuid(),
                RawMaterialId = request.RawMaterialId,
                SupplierId = request.SupplierId,
                BatchNumber = request.BatchNumber,
                InvoiceNumber = request.InvoiceNumber,
                ReceivedQuantity = request.Quantity,
                CurrentQuantity = request.Quantity,
                UnitCost = request.UnitCost,
                ReceivedDate = DateTime.UtcNow,
                ExpiryDate = request.ExpiryDate,
                ManufactureDate = request.ManufactureDate,
                Status = "QUARENTENA",
                CertificateNumber = request.CertificateNumber,
                CreatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId
            };

            _db.Batches.Add(batch);

            // Criar movimentação
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                RawMaterialId = request.RawMaterialId,
                BatchId = batch.Id,
                MovementType = "ENTRADA",
                Quantity = request.Quantity,
                StockBefore = material.CurrentStock,
                StockAfter = material.CurrentStock + request.Quantity,
                Reason = request.Reason ?? "Entrada de mercadoria",
                SupplierId = request.SupplierId,
                DocumentNumber = request.InvoiceNumber,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockMovements.Add(movement);

            // Atualizar estoque total
            material.CurrentStock += request.Quantity;
            material.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stock entry created - Batch: {BatchNumber}, Quantity: {Quantity}",
                batch.BatchNumber, request.Quantity);

            return Ok(new
            {
                message = "Entry created successfully",
                batchId = batch.Id,
                movementId = movement.Id,
                newStock = material.CurrentStock
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating stock entry");
            return StatusCode(500, new { message = "Error creating entry", error = ex.Message });
        }
    }

    // POST: api/stockmovements/saida
    [HttpPost("saida")]
    public async Task<IActionResult> SaidaEstoque(
        [FromBody] SaidaEstoqueRequest request,
        [FromQuery] Guid employeeId,
        [FromQuery] Guid establishmentId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var material = await _db.RawMaterials.FindAsync(request.RawMaterialId);
            if (material == null)
                return NotFound(new { message = "Raw material not found" });

            var batch = await _db.Batches.FindAsync(request.BatchId);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            if (batch.RawMaterialId != request.RawMaterialId)
                return BadRequest(new { message = "Batch does not belong to this raw material" });

            if (batch.Status != "APROVADO")
                return BadRequest(new { message = "Batch must be approved for use" });

            if (batch.CurrentQuantity < request.Quantity)
                return BadRequest(new { message = "Insufficient quantity in batch" });

            if (material.CurrentStock < request.Quantity)
                return BadRequest(new { message = "Insufficient stock" });

            // Criar movimentação
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                RawMaterialId = request.RawMaterialId,
                BatchId = request.BatchId,
                MovementType = "SAIDA",
                Quantity = -request.Quantity,
                StockBefore = material.CurrentStock,
                StockAfter = material.CurrentStock - request.Quantity,
                Reason = request.Reason,
                ManipulationOrderId = request.ManipulationOrderId,
                SaleId = request.SaleId,
                DocumentNumber = request.DocumentNumber,
                PrescriptionNumber = request.PrescriptionNumber,
                NotificationNumber = request.NotificationNumber,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = employeeId,
                AuthorizedByEmployeeId = request.AuthorizedByEmployeeId,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockMovements.Add(movement);

            // Atualizar quantidades
            batch.CurrentQuantity -= request.Quantity;
            material.CurrentStock -= request.Quantity;
            material.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stock exit created - Batch: {BatchNumber}, Quantity: {Quantity}",
                batch.BatchNumber, request.Quantity);

            return Ok(new
            {
                message = "Exit created successfully",
                movementId = movement.Id,
                newStock = material.CurrentStock,
                newBatchQuantity = batch.CurrentQuantity
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating stock exit");
            return StatusCode(500, new { message = "Error creating exit", error = ex.Message });
        }
    }

    // POST: api/stockmovements/ajuste
    [HttpPost("ajuste")]
    public async Task<IActionResult> AjusteEstoque(
        [FromBody] AjusteEstoqueRequest request,
        [FromQuery] Guid employeeId,
        [FromQuery] Guid establishmentId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var material = await _db.RawMaterials.FindAsync(request.RawMaterialId);
            if (material == null)
                return NotFound(new { message = "Raw material not found" });

            var batch = await _db.Batches.FindAsync(request.BatchId);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            var newBatchQuantity = batch.CurrentQuantity + request.QuantityAdjustment;
            var newMaterialStock = material.CurrentStock + request.QuantityAdjustment;

            if (newBatchQuantity < 0)
                return BadRequest(new { message = "Resulting batch quantity cannot be negative" });

            if (newMaterialStock < 0)
                return BadRequest(new { message = "Resulting stock cannot be negative" });

            // Criar movimentação
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                RawMaterialId = request.RawMaterialId,
                BatchId = request.BatchId,
                MovementType = "AJUSTE",
                Quantity = request.QuantityAdjustment,
                StockBefore = material.CurrentStock,
                StockAfter = newMaterialStock,
                Reason = request.Reason,
                DocumentNumber = request.DocumentNumber,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = employeeId,
                AuthorizedByEmployeeId = request.AuthorizedByEmployeeId,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockMovements.Add(movement);

            // Atualizar quantidades
            batch.CurrentQuantity = newBatchQuantity;
            material.CurrentStock = newMaterialStock;
            material.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stock adjustment created - Batch: {BatchNumber}, Adjustment: {Adjustment}",
                batch.BatchNumber, request.QuantityAdjustment);

            return Ok(new
            {
                message = "Adjustment created successfully",
                movementId = movement.Id,
                newStock = material.CurrentStock,
                newBatchQuantity = batch.CurrentQuantity,
                adjustment = request.QuantityAdjustment
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating stock adjustment");
            return StatusCode(500, new { message = "Error creating adjustment", error = ex.Message });
        }
    }

    // POST: api/stockmovements/perda
    [HttpPost("perda")]
    public async Task<IActionResult> PerdaEstoque(
        [FromBody] PerdaEstoqueRequest request,
        [FromQuery] Guid employeeId,
        [FromQuery] Guid establishmentId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var material = await _db.RawMaterials.FindAsync(request.RawMaterialId);
            if (material == null)
                return NotFound(new { message = "Raw material not found" });

            var batch = await _db.Batches.FindAsync(request.BatchId);
            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            if (batch.CurrentQuantity < request.Quantity)
                return BadRequest(new { message = "Insufficient quantity in batch" });

            if (material.CurrentStock < request.Quantity)
                return BadRequest(new { message = "Insufficient stock" });

            var reason = $"[{request.LossType}] {request.Reason}";

            // Criar movimentação
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                RawMaterialId = request.RawMaterialId,
                BatchId = request.BatchId,
                MovementType = "PERDA",
                Quantity = -request.Quantity,
                StockBefore = material.CurrentStock,
                StockAfter = material.CurrentStock - request.Quantity,
                Reason = reason,
                DocumentNumber = request.DocumentNumber,
                MovementDate = DateTime.UtcNow,
                PerformedByEmployeeId = employeeId,
                AuthorizedByEmployeeId = request.AuthorizedByEmployeeId,
                CreatedAt = DateTime.UtcNow
            };

            _db.StockMovements.Add(movement);

            // Atualizar quantidades
            batch.CurrentQuantity -= request.Quantity;
            material.CurrentStock -= request.Quantity;
            material.UpdatedAt = DateTime.UtcNow;

            // Se for vencimento, atualizar status do lote
            if (request.LossType == "VENCIMENTO")
            {
                batch.Status = "VENCIDO";
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Stock loss created - Type: {LossType}, Batch: {BatchNumber}, Quantity: {Quantity}",
                request.LossType, batch.BatchNumber, request.Quantity);

            return Ok(new
            {
                message = "Loss registered successfully",
                movementId = movement.Id,
                newStock = material.CurrentStock,
                newBatchQuantity = batch.CurrentQuantity
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error registering stock loss");
            return StatusCode(500, new { message = "Error registering loss", error = ex.Message });
        }
    }

    // POST: api/stockmovements/manipulacao
    [HttpPost("manipulacao")]
    public async Task<IActionResult> ConsumoManipulacao(
        [FromBody] ConsumoManipulacaoRequest request,
        [FromQuery] Guid employeeId,
        [FromQuery] Guid establishmentId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            var order = await _db.ManipulationOrders
                .Include(mo => mo.Formula)
                .FirstOrDefaultAsync(mo => mo.Id == request.ManipulationOrderId);

            if (order == null)
                return NotFound(new { message = "Manipulation order not found" });

            var movements = new List<StockMovement>();

            foreach (var item in request.Items)
            {
                var material = await _db.RawMaterials.FindAsync(item.RawMaterialId);
                if (material == null)
                    return NotFound(new { message = $"Raw material {item.RawMaterialId} not found" });

                var batch = await _db.Batches.FindAsync(item.BatchId);
                if (batch == null)
                    return NotFound(new { message = $"Batch {item.BatchId} not found" });

                if (batch.Status != "APROVADO")
                    return BadRequest(new { message = $"Batch {batch.BatchNumber} must be approved" });

                if (batch.CurrentQuantity < item.Quantity)
                    return BadRequest(new { message = $"Insufficient quantity in batch {batch.BatchNumber}" });

                if (material.CurrentStock < item.Quantity)
                    return BadRequest(new { message = $"Insufficient stock for {material.Name}" });

                // Criar movimentação
                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = establishmentId,
                    RawMaterialId = item.RawMaterialId,
                    BatchId = item.BatchId,
                    MovementType = "MANIPULACAO",
                    Quantity = -item.Quantity,
                    StockBefore = material.CurrentStock,
                    StockAfter = material.CurrentStock - item.Quantity,
                    Reason = $"Consumo - Ordem {order.OrderNumber}" + (string.IsNullOrWhiteSpace(item.Notes) ? "" : $" - {item.Notes}"),
                    ManipulationOrderId = request.ManipulationOrderId,
                    MovementDate = DateTime.UtcNow,
                    PerformedByEmployeeId = employeeId,
                    CreatedAt = DateTime.UtcNow
                };

                _db.StockMovements.Add(movement);
                movements.Add(movement);

                // Atualizar quantidades
                batch.CurrentQuantity -= item.Quantity;
                material.CurrentStock -= item.Quantity;
                material.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Manipulation consumption created - Order: {OrderNumber}, Items: {ItemsCount}",
                order.OrderNumber, request.Items.Count);

            return Ok(new
            {
                message = "Consumption registered successfully",
                movementIds = movements.Select(m => m.Id).ToList(),
                itemsProcessed = movements.Count
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error registering manipulation consumption");
            return StatusCode(500, new { message = "Error registering consumption", error = ex.Message });
        }
    }

    // GET: api/stockmovements/stats
    [HttpGet("stats")]
    public async Task<ActionResult<StockMovementStatsResponse>> GetStats(
        [FromQuery] Guid? establishmentId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var query = _db.StockMovements.AsQueryable();

            if (establishmentId.HasValue)
                query = query.Where(sm => sm.EstablishmentId == establishmentId.Value);

            if (startDate.HasValue)
                query = query.Where(sm => sm.MovementDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(sm => sm.MovementDate <= endDate.Value);

            var totalMovements = await query.CountAsync();
            var entradasCount = await query.CountAsync(sm => sm.MovementType == "ENTRADA");
            var saidasCount = await query.CountAsync(sm => sm.MovementType == "SAIDA");
            var ajustesCount = await query.CountAsync(sm => sm.MovementType == "AJUSTE");
            var perdasCount = await query.CountAsync(sm => sm.MovementType == "PERDA");

            var totalEntradas = await query.Where(sm => sm.MovementType == "ENTRADA").SumAsync(sm => sm.Quantity);
            var totalSaidas = await query.Where(sm => sm.MovementType == "SAIDA").SumAsync(sm => Math.Abs(sm.Quantity));
            var totalPerdas = await query.Where(sm => sm.MovementType == "PERDA").SumAsync(sm => Math.Abs(sm.Quantity));

            var movementsByType = await query
                .GroupBy(sm => sm.MovementType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Type, x => x.Count);

            return Ok(new StockMovementStatsResponse
            {
                TotalMovements = totalMovements,
                EntradasCount = entradasCount,
                SaidasCount = saidasCount,
                AjustesCount = ajustesCount,
                PerdasCount = perdasCount,
                TotalEntradas = totalEntradas,
                TotalSaidas = totalSaidas,
                TotalPerdas = totalPerdas,
                MovementsByType = movementsByType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock movement statistics");
            return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
        }
    }

    // GET: api/stockmovements/rastreabilidade/{batchId}
    [HttpGet("rastreabilidade/{batchId}")]
    public async Task<ActionResult<RastreabilidadeResponse>> GetRastreabilidade(Guid batchId)
    {
        try
        {
            var batch = await _db.Batches
                .Include(b => b.RawMaterial)
                .FirstOrDefaultAsync(b => b.Id == batchId);

            if (batch == null)
                return NotFound(new { message = "Batch not found" });

            var movements = await _db.StockMovements
                .Include(sm => sm.PerformedByEmployee)
                .Include(sm => sm.AuthorizedByEmployee)
                .Where(sm => sm.BatchId == batchId)
                .OrderBy(sm => sm.MovementDate)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    MovementType = sm.MovementType,
                    Quantity = sm.Quantity,
                    StockBefore = sm.StockBefore,
                    StockAfter = sm.StockAfter,
                    Reason = sm.Reason,
                    DocumentNumber = sm.DocumentNumber,
                    MovementDate = sm.MovementDate,
                    PerformedByEmployeeName = sm.PerformedByEmployee != null ? sm.PerformedByEmployee.FullName : null,
                    AuthorizedByEmployeeName = sm.AuthorizedByEmployee != null ? sm.AuthorizedByEmployee.FullName : null,
                    ManipulationOrderId = sm.ManipulationOrderId,
                    PrescriptionNumber = sm.PrescriptionNumber
                })
                .ToListAsync();

            var manipulationOrderIds = movements
                .Where(m => m.ManipulationOrderId.HasValue)
                .Select(m => m.ManipulationOrderId!.Value)
                .Distinct()
                .ToList();

            var orders = await _db.ManipulationOrders
                .Where(mo => manipulationOrderIds.Contains(mo.Id))
                .Select(mo => new ManipulationOrderSummary
                {
                    Id = mo.Id,
                    OrderNumber = mo.OrderNumber,
                    CustomerName = mo.CustomerName,
                    OrderDate = mo.OrderDate,
                    Status = mo.Status,
                    QuantityUsed = _db.StockMovements
                        .Where(sm => sm.ManipulationOrderId == mo.Id && sm.BatchId == batchId)
                        .Sum(sm => Math.Abs(sm.Quantity))
                })
                .ToListAsync();

            return Ok(new RastreabilidadeResponse
            {
                BatchId = batch.Id,
                BatchNumber = batch.BatchNumber,
                RawMaterialName = batch.RawMaterial!.Name,
                ReceivedQuantity = batch.ReceivedQuantity,
                CurrentQuantity = batch.CurrentQuantity,
                Movements = movements,
                ManipulationOrders = orders
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving batch traceability {BatchId}", batchId);
            return StatusCode(500, new { message = "Error retrieving traceability", error = ex.Message });
        }
    }

    // GET: api/stockmovements/by-raw-material/{rawMaterialId}
    [HttpGet("by-raw-material/{rawMaterialId}")]
    public async Task<ActionResult<List<StockMovementDto>>> GetByRawMaterial(
        Guid rawMaterialId,
        [FromQuery] int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var movements = await _db.StockMovements
                .Include(sm => sm.Batch)
                .Include(sm => sm.PerformedByEmployee)
                .Where(sm => sm.RawMaterialId == rawMaterialId && sm.MovementDate >= startDate)
                .OrderByDescending(sm => sm.MovementDate)
                .Select(sm => new StockMovementDto
                {
                    Id = sm.Id,
                    BatchNumber = sm.Batch != null ? sm.Batch.BatchNumber : null,
                    MovementType = sm.MovementType,
                    Quantity = sm.Quantity,
                    StockBefore = sm.StockBefore,
                    StockAfter = sm.StockAfter,
                    Reason = sm.Reason,
                    MovementDate = sm.MovementDate,
                    PerformedByEmployeeName = sm.PerformedByEmployee != null ? sm.PerformedByEmployee.FullName : null
                })
                .ToListAsync();

            return Ok(movements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving movements for raw material {RawMaterialId}", rawMaterialId);
            return StatusCode(500, new { message = "Error retrieving movements", error = ex.Message });
        }
    }
}