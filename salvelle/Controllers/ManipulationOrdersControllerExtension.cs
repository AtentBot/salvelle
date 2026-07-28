using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DTOs.Pharmacy.ManipulationOrders;
using Models.Pharmacy;
using Models.Employees;
using System.Text.Json;
using Service;
using DTOs;

namespace Controllers;

/// <summary>
/// Extensão do ManipulationOrdersController - Perdas, Sobras, Conferência Dupla e Produção
/// </summary>
public partial class ManipulationOrdersController
{
    /// <summary>
    /// Valida que a ordem pertence ao estabelecimento do funcionário autenticado (previne IDOR cross-tenant).
    /// </summary>
    private async Task<bool> OrderInScope(Guid orderId)
        => await _context.ManipulationOrders
            .AnyAsync(o => o.Id == orderId && o.EstablishmentId == GetEstablishmentId());

    // ===================================================================
    // PERDAS
    // ===================================================================

    /// <summary>
    /// Registra perda durante manipulação
    /// </summary>
    [HttpPost("{id}/losses")]
    public async Task<ActionResult<ApiResponse<ManipulationLossDto>>> RegisterLoss(
        Guid id, [FromBody] RegisterLossDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);
        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var employeeId = GetEmployeeId();

        var loss = new ManipulationLoss
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = id,
            RawMaterialId = dto.RawMaterialId,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Reason = dto.Reason,
            LossType = dto.LossType,
            BatchNumber = dto.BatchNumber,
            ValueLost = dto.ValueLost,
            RegisteredByEmployeeId = employeeId,
            RegisteredAt = DateTime.UtcNow
        };

        _context.ManipulationLosses.Add(loss);
        await _context.SaveChangesAsync();

        var result = new ManipulationLossDto
        {
            Id = loss.Id,
            ManipulationOrderId = loss.ManipulationOrderId,
            RawMaterialId = loss.RawMaterialId,
            Quantity = loss.Quantity,
            Unit = loss.Unit,
            Reason = loss.Reason,
            LossType = loss.LossType,
            BatchNumber = loss.BatchNumber,
            ValueLost = loss.ValueLost,
            RegisteredByEmployeeId = loss.RegisteredByEmployeeId,
            RegisteredAt = loss.RegisteredAt
        };

        return Ok(ApiResponse<ManipulationLossDto>.SuccessResponse(result, "Perda registrada com sucesso"));
    }

    /// <summary>
    /// Lista perdas de uma ordem
    /// </summary>
    [HttpGet("{id}/losses")]
    public async Task<ActionResult<ApiResponse<List<ManipulationLossDto>>>> GetLosses(Guid id)
    {
        if (!await OrderInScope(id))
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var losses = await _context.ManipulationLosses
            .Include(l => l.RawMaterial)
            .Include(l => l.RegisteredByEmployee)
            .Where(l => l.ManipulationOrderId == id)
            .OrderByDescending(l => l.RegisteredAt)
            .Select(l => new ManipulationLossDto
            {
                Id = l.Id,
                ManipulationOrderId = l.ManipulationOrderId,
                RawMaterialId = l.RawMaterialId,
                RawMaterialName = l.RawMaterial != null ? l.RawMaterial.Name : null,
                Quantity = l.Quantity,
                Unit = l.Unit,
                Reason = l.Reason,
                LossType = l.LossType,
                BatchNumber = l.BatchNumber,
                ValueLost = l.ValueLost,
                RegisteredByEmployeeId = l.RegisteredByEmployeeId,
                RegisteredByEmployeeName = l.RegisteredByEmployee != null ? l.RegisteredByEmployee.FullName : null,
                RegisteredAt = l.RegisteredAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ManipulationLossDto>>.SuccessResponse(losses));
    }

    /// <summary>
    /// Remove uma perda registrada
    /// </summary>
    [HttpDelete("losses/{lossId}")]
    public async Task<ActionResult<ApiResponse>> DeleteLoss(Guid lossId)
    {
        var loss = await _context.ManipulationLosses.FindAsync(lossId);
        if (loss == null)
            return NotFound(ApiResponse.ErrorResponse("Perda não encontrada"));

        if (!await OrderInScope(loss.ManipulationOrderId))
            return NotFound(ApiResponse.ErrorResponse("Perda não encontrada"));

        _context.ManipulationLosses.Remove(loss);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Perda removida com sucesso"));
    }

    // ===================================================================
    // SOBRAS
    // ===================================================================

    /// <summary>
    /// Registra sobra durante manipulação
    /// </summary>
    [HttpPost("{id}/leftovers")]
    public async Task<ActionResult<ApiResponse<ManipulationLeftoverDto>>> RegisterLeftover(
        Guid id, [FromBody] RegisterLeftoverDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);
        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var employeeId = GetEmployeeId();

        var leftover = new ManipulationLeftover
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = id,
            RawMaterialId = dto.RawMaterialId,
            Quantity = dto.Quantity,
            Unit = dto.Unit,
            Destination = dto.Destination,
            DestinationDetails = dto.DestinationDetails,
            BatchNumber = dto.BatchNumber,
            RegisteredByEmployeeId = employeeId,
            RegisteredAt = DateTime.UtcNow,
            ReintegratedToStock = false
        };

        _context.ManipulationLeftovers.Add(leftover);

        // Se for para reintegrar ao estoque
        if (dto.ReintegrateToStock && dto.RawMaterialId.HasValue)
        {
            // Buscar lote para reintegrar (criar movimento de entrada)
            var batch = await _context.Batches
                .Where(b => b.RawMaterialId == dto.RawMaterialId && 
                           b.BatchNumber == dto.BatchNumber &&
                           b.Status.ToUpper() == "APROVADO")
                .FirstOrDefaultAsync();

            if (batch != null)
            {
                batch.CurrentQuantity += dto.Quantity;
                
                var movement = new StockMovement
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = order.EstablishmentId,
                    BatchId = batch.Id,
                    RawMaterialId = dto.RawMaterialId.Value,
                    MovementType = "ENTRADA",
                    Quantity = dto.Quantity,
                    Reason = $"Reintegração de sobra - Ordem {order.OrderNumber}",
                    ManipulationOrderId = id,
                    DocumentNumber = order.OrderNumber,
                    PerformedByEmployeeId = employeeId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockMovements.Add(movement);
                leftover.ReintegratedToStock = true;
                leftover.ReintegrationDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();

        var result = new ManipulationLeftoverDto
        {
            Id = leftover.Id,
            ManipulationOrderId = leftover.ManipulationOrderId,
            RawMaterialId = leftover.RawMaterialId,
            Quantity = leftover.Quantity,
            Unit = leftover.Unit,
            Destination = leftover.Destination,
            DestinationDetails = leftover.DestinationDetails,
            BatchNumber = leftover.BatchNumber,
            ReintegratedToStock = leftover.ReintegratedToStock,
            ReintegrationDate = leftover.ReintegrationDate,
            RegisteredByEmployeeId = leftover.RegisteredByEmployeeId,
            RegisteredAt = leftover.RegisteredAt
        };

        return Ok(ApiResponse<ManipulationLeftoverDto>.SuccessResponse(result, "Sobra registrada com sucesso"));
    }

    /// <summary>
    /// Lista sobras de uma ordem
    /// </summary>
    [HttpGet("{id}/leftovers")]
    public async Task<ActionResult<ApiResponse<List<ManipulationLeftoverDto>>>> GetLeftovers(Guid id)
    {
        if (!await OrderInScope(id))
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var leftovers = await _context.ManipulationLeftovers
            .Include(l => l.RawMaterial)
            .Include(l => l.RegisteredByEmployee)
            .Where(l => l.ManipulationOrderId == id)
            .OrderByDescending(l => l.RegisteredAt)
            .Select(l => new ManipulationLeftoverDto
            {
                Id = l.Id,
                ManipulationOrderId = l.ManipulationOrderId,
                RawMaterialId = l.RawMaterialId,
                RawMaterialName = l.RawMaterial != null ? l.RawMaterial.Name : null,
                Quantity = l.Quantity,
                Unit = l.Unit,
                Destination = l.Destination,
                DestinationDetails = l.DestinationDetails,
                BatchNumber = l.BatchNumber,
                ReintegratedToStock = l.ReintegratedToStock,
                ReintegrationDate = l.ReintegrationDate,
                RegisteredByEmployeeId = l.RegisteredByEmployeeId,
                RegisteredByEmployeeName = l.RegisteredByEmployee != null ? l.RegisteredByEmployee.FullName : null,
                RegisteredAt = l.RegisteredAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ManipulationLeftoverDto>>.SuccessResponse(leftovers));
    }

    // ===================================================================
    // CONFERÊNCIA DUPLA
    // ===================================================================

    /// <summary>
    /// Inicia conferência dupla (primeira verificação)
    /// </summary>
    [HttpPost("{id}/dual-verification/start")]
    public async Task<ActionResult<ApiResponse<DualVerificationDto>>> StartDualVerification(
        Guid id, [FromBody] StartDualVerificationDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);
        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var employeeId = GetEmployeeId();
        var employee = await _context.Employees.FindAsync(employeeId);

        var verification = new DualVerification
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = id,
            VerificationType = dto.VerificationType,
            FirstVerifierId = employeeId,
            FirstVerificationAt = DateTime.UtcNow,
            FirstVerifierNotes = dto.Notes,
            FirstVerifierSignature = dto.Signature,
            ChecklistJson = dto.ChecklistItems != null 
                ? JsonSerializer.Serialize(dto.ChecklistItems) 
                : null,
            Approved = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.DualVerifications.Add(verification);
        await _context.SaveChangesAsync();

        var result = new DualVerificationDto
        {
            Id = verification.Id,
            ManipulationOrderId = id,
            VerificationType = verification.VerificationType,
            FirstVerifierId = verification.FirstVerifierId,
            FirstVerifierName = employee?.FullName,
            FirstVerificationAt = verification.FirstVerificationAt,
            FirstVerifierNotes = verification.FirstVerifierNotes,
            FirstVerifierSigned = !string.IsNullOrEmpty(verification.FirstVerifierSignature),
            Approved = false,
            Status = "PENDENTE",
            CreatedAt = verification.CreatedAt
        };

        return Ok(ApiResponse<DualVerificationDto>.SuccessResponse(result, "Primeira verificação registrada. Aguardando segunda verificação."));
    }

    /// <summary>
    /// Completa conferência dupla (segunda verificação)
    /// </summary>
    [HttpPost("{id}/dual-verification/complete")]
    public async Task<ActionResult<ApiResponse<DualVerificationDto>>> CompleteDualVerification(
        Guid id, [FromBody] CompleteDualVerificationDto dto)
    {
        var verification = await _context.DualVerifications
            .Include(v => v.FirstVerifier)
            .FirstOrDefaultAsync(v => v.Id == dto.VerificationId);

        if (verification == null)
            return NotFound(ApiResponse.ErrorResponse("Verificação não encontrada"));

        if (!await OrderInScope(verification.ManipulationOrderId))
            return NotFound(ApiResponse.ErrorResponse("Verificação não encontrada"));

        var employeeId = GetEmployeeId();
        var employee = await _context.Employees.FindAsync(employeeId);

        // Validar que não é o mesmo funcionário
        if (verification.FirstVerifierId == employeeId)
            return BadRequest(ApiResponse.ErrorResponse("A segunda verificação deve ser feita por outro funcionário"));

        verification.SecondVerifierId = employeeId;
        verification.SecondVerificationAt = DateTime.UtcNow;
        verification.SecondVerifierNotes = dto.Notes;
        verification.SecondVerifierSignature = dto.Signature;
        verification.Approved = dto.Approved;
        verification.RejectionReason = dto.RejectionReason;
        verification.UpdatedAt = DateTime.UtcNow;

        // Atualizar checklist se fornecido
        if (dto.ChecklistItems != null)
        {
            verification.ChecklistJson = JsonSerializer.Serialize(dto.ChecklistItems);
        }

        await _context.SaveChangesAsync();

        var result = new DualVerificationDto
        {
            Id = verification.Id,
            ManipulationOrderId = verification.ManipulationOrderId,
            VerificationType = verification.VerificationType,
            FirstVerifierId = verification.FirstVerifierId,
            FirstVerifierName = verification.FirstVerifier?.FullName,
            FirstVerificationAt = verification.FirstVerificationAt,
            FirstVerifierNotes = verification.FirstVerifierNotes,
            FirstVerifierSigned = !string.IsNullOrEmpty(verification.FirstVerifierSignature),
            SecondVerifierId = verification.SecondVerifierId,
            SecondVerifierName = employee?.FullName,
            SecondVerificationAt = verification.SecondVerificationAt,
            SecondVerifierNotes = verification.SecondVerifierNotes,
            SecondVerifierSigned = !string.IsNullOrEmpty(verification.SecondVerifierSignature),
            Approved = verification.Approved,
            RejectionReason = verification.RejectionReason,
            Status = verification.Approved ? "COMPLETA" : "REJEITADA",
            CreatedAt = verification.CreatedAt
        };

        var message = verification.Approved 
            ? "Conferência dupla aprovada com sucesso" 
            : "Conferência dupla rejeitada";

        return Ok(ApiResponse<DualVerificationDto>.SuccessResponse(result, message));
    }

    /// <summary>
    /// Lista verificações de uma ordem
    /// </summary>
    [HttpGet("{id}/dual-verifications")]
    public async Task<ActionResult<ApiResponse<List<DualVerificationDto>>>> GetDualVerifications(Guid id)
    {
        if (!await OrderInScope(id))
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var verifications = await _context.DualVerifications
            .Include(v => v.FirstVerifier)
            .Include(v => v.SecondVerifier)
            .Where(v => v.ManipulationOrderId == id)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new DualVerificationDto
            {
                Id = v.Id,
                ManipulationOrderId = v.ManipulationOrderId,
                VerificationType = v.VerificationType,
                FirstVerifierId = v.FirstVerifierId,
                FirstVerifierName = v.FirstVerifier != null ? v.FirstVerifier.FullName : null,
                FirstVerificationAt = v.FirstVerificationAt,
                FirstVerifierNotes = v.FirstVerifierNotes,
                FirstVerifierSigned = v.FirstVerifierSignature != null,
                SecondVerifierId = v.SecondVerifierId,
                SecondVerifierName = v.SecondVerifier != null ? v.SecondVerifier.FullName : null,
                SecondVerificationAt = v.SecondVerificationAt,
                SecondVerifierNotes = v.SecondVerifierNotes,
                SecondVerifierSigned = v.SecondVerifierSignature != null,
                Approved = v.Approved,
                RejectionReason = v.RejectionReason,
                Status = v.SecondVerificationAt == null ? "PENDENTE" : (v.Approved ? "COMPLETA" : "REJEITADA"),
                CreatedAt = v.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<DualVerificationDto>>.SuccessResponse(verifications));
    }

    // ===================================================================
    // REGISTRO DE PRODUÇÃO
    // ===================================================================

    /// <summary>
    /// Registra produção final (quantidade real, rendimento, etc)
    /// </summary>
    [HttpPost("{id}/production")]
    public async Task<ActionResult<ApiResponse<ProductionRecordDto>>> RegisterProduction(
        Guid id, [FromBody] RegisterProductionDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var employeeId = GetEmployeeId();
        var service = new ManipulationService(_context);

        // Calcular rendimento
        var yield = service.CalculateYield(order.QuantityToProduce, dto.ActualQuantity);
        var isYieldAcceptable = service.IsYieldAcceptable(yield);

        // Gerar número de lote se não fornecido
        var batchNumber = dto.BatchNumber ?? service.GenerateBatchNumber(order.EstablishmentId);

        // Calcular validade se não fornecida
        var expiryDate = dto.ExpiryDate ?? await service.CalculateExpiryDate(id);

        // Calcular tempo total
        var totalMinutes = (int)(dto.ProductionEnd - dto.ProductionStart).TotalMinutes;

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            ManipulationOrderId = id,
            ExpectedQuantity = order.QuantityToProduce,
            ActualQuantity = dto.ActualQuantity,
            Unit = dto.Unit,
            YieldPercentage = yield,
            IsYieldAcceptable = isYieldAcceptable,
            YieldDeviationReason = dto.YieldDeviationReason,
            BatchNumber = batchNumber,
            ExpiryDate = expiryDate,
            ProductionStart = dto.ProductionStart,
            ProductionEnd = dto.ProductionEnd,
            TotalProductionTimeMinutes = totalMinutes,
            ProducedByEmployeeId = employeeId,
            QualityNotes = dto.QualityNotes,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductionRecords.Add(record);

        // Atualizar ordem
        order.ExpiryDate = expiryDate;
        order.CompletionDate = dto.ProductionEnd;
        order.UpdatedAt = DateTime.UtcNow;

        // Registrar perdas se fornecidas
        if (dto.Losses != null)
        {
            foreach (var lossDto in dto.Losses)
            {
                var loss = new ManipulationLoss
                {
                    Id = Guid.NewGuid(),
                    ManipulationOrderId = id,
                    RawMaterialId = lossDto.RawMaterialId,
                    Quantity = lossDto.Quantity,
                    Unit = lossDto.Unit,
                    Reason = lossDto.Reason,
                    LossType = lossDto.LossType,
                    BatchNumber = lossDto.BatchNumber,
                    ValueLost = lossDto.ValueLost,
                    RegisteredByEmployeeId = employeeId,
                    RegisteredAt = DateTime.UtcNow
                };
                _context.ManipulationLosses.Add(loss);
            }
        }

        // Registrar sobras se fornecidas
        if (dto.Leftovers != null)
        {
            foreach (var leftoverDto in dto.Leftovers)
            {
                var leftover = new ManipulationLeftover
                {
                    Id = Guid.NewGuid(),
                    ManipulationOrderId = id,
                    RawMaterialId = leftoverDto.RawMaterialId,
                    Quantity = leftoverDto.Quantity,
                    Unit = leftoverDto.Unit,
                    Destination = leftoverDto.Destination,
                    DestinationDetails = leftoverDto.DestinationDetails,
                    BatchNumber = leftoverDto.BatchNumber,
                    RegisteredByEmployeeId = employeeId,
                    RegisteredAt = DateTime.UtcNow
                };
                _context.ManipulationLeftovers.Add(leftover);
            }
        }

        await _context.SaveChangesAsync();

        var result = new ProductionRecordDto
        {
            Id = record.Id,
            ManipulationOrderId = record.ManipulationOrderId,
            OrderNumber = order.OrderNumber,
            ExpectedQuantity = record.ExpectedQuantity,
            ActualQuantity = record.ActualQuantity,
            Unit = record.Unit,
            YieldPercentage = record.YieldPercentage,
            IsYieldAcceptable = record.IsYieldAcceptable,
            YieldDeviationReason = record.YieldDeviationReason,
            BatchNumber = record.BatchNumber,
            ExpiryDate = record.ExpiryDate,
            ProductionStart = record.ProductionStart,
            ProductionEnd = record.ProductionEnd,
            TotalProductionTimeMinutes = record.TotalProductionTimeMinutes,
            QualityNotes = record.QualityNotes,
            CreatedAt = record.CreatedAt
        };

        return Ok(ApiResponse<ProductionRecordDto>.SuccessResponse(result, "Produção registrada com sucesso"));
    }

    /// <summary>
    /// Obtém registro de produção de uma ordem
    /// </summary>
    [HttpGet("{id}/production")]
    public async Task<ActionResult<ApiResponse<ProductionRecordDto>>> GetProductionRecord(Guid id)
    {
        if (!await OrderInScope(id))
            return NotFound(ApiResponse<ProductionRecordDto>.ErrorResponse("Registro de produção não encontrado"));

        var record = await _context.ProductionRecords
            .Include(r => r.ManipulationOrder)
            .Include(r => r.ProducedByEmployee)
            .Include(r => r.VerifiedByEmployee)
            .Include(r => r.ApprovedByPharmacist)
            .FirstOrDefaultAsync(r => r.ManipulationOrderId == id);

        if (record == null)
            return NotFound(ApiResponse<ProductionRecordDto>.ErrorResponse("Registro de produção não encontrado"));

        // Buscar perdas e sobras
        var losses = await _context.ManipulationLosses
            .Include(l => l.RawMaterial)
            .Include(l => l.RegisteredByEmployee)
            .Where(l => l.ManipulationOrderId == id)
            .ToListAsync();

        var leftovers = await _context.ManipulationLeftovers
            .Include(l => l.RawMaterial)
            .Include(l => l.RegisteredByEmployee)
            .Where(l => l.ManipulationOrderId == id)
            .ToListAsync();

        var result = new ProductionRecordDto
        {
            Id = record.Id,
            ManipulationOrderId = record.ManipulationOrderId,
            OrderNumber = record.ManipulationOrder?.OrderNumber ?? "N/A",
            ExpectedQuantity = record.ExpectedQuantity,
            ActualQuantity = record.ActualQuantity,
            Unit = record.Unit,
            YieldPercentage = record.YieldPercentage,
            IsYieldAcceptable = record.IsYieldAcceptable,
            YieldDeviationReason = record.YieldDeviationReason,
            BatchNumber = record.BatchNumber,
            ExpiryDate = record.ExpiryDate,
            ProductionStart = record.ProductionStart,
            ProductionEnd = record.ProductionEnd,
            TotalProductionTimeMinutes = record.TotalProductionTimeMinutes,
            ProducedByEmployeeName = record.ProducedByEmployee?.FullName,
            VerifiedByEmployeeName = record.VerifiedByEmployee?.FullName,
            ApprovedByPharmacistName = record.ApprovedByPharmacist?.FullName,
            QualityNotes = record.QualityNotes,
            Losses = losses.Select(l => new ManipulationLossDto
            {
                Id = l.Id,
                ManipulationOrderId = l.ManipulationOrderId,
                RawMaterialId = l.RawMaterialId,
                RawMaterialName = l.RawMaterial?.Name,
                Quantity = l.Quantity,
                Unit = l.Unit,
                Reason = l.Reason,
                LossType = l.LossType,
                BatchNumber = l.BatchNumber,
                ValueLost = l.ValueLost,
                RegisteredByEmployeeId = l.RegisteredByEmployeeId,
                RegisteredByEmployeeName = l.RegisteredByEmployee?.FullName,
                RegisteredAt = l.RegisteredAt
            }).ToList(),
            Leftovers = leftovers.Select(l => new ManipulationLeftoverDto
            {
                Id = l.Id,
                ManipulationOrderId = l.ManipulationOrderId,
                RawMaterialId = l.RawMaterialId,
                RawMaterialName = l.RawMaterial?.Name,
                Quantity = l.Quantity,
                Unit = l.Unit,
                Destination = l.Destination,
                DestinationDetails = l.DestinationDetails,
                BatchNumber = l.BatchNumber,
                ReintegratedToStock = l.ReintegratedToStock,
                ReintegrationDate = l.ReintegrationDate,
                RegisteredByEmployeeId = l.RegisteredByEmployeeId,
                RegisteredByEmployeeName = l.RegisteredByEmployee?.FullName,
                RegisteredAt = l.RegisteredAt
            }).ToList(),
            CreatedAt = record.CreatedAt
        };

        return Ok(ApiResponse<ProductionRecordDto>.SuccessResponse(result));
    }

    // ===================================================================
    // VERIFICAÇÃO DE ESTOQUE
    // ===================================================================

    /// <summary>
    /// Verifica disponibilidade de estoque para manipulação
    /// </summary>
    [HttpGet("{id}/check-stock")]
    public async Task<ActionResult<ApiResponse<StockCheckDto>>> CheckStock(Guid id)
    {
        if (!await OrderInScope(id))
            return NotFound(ApiResponse<StockCheckDto>.ErrorResponse("Ordem não encontrada"));

        var service = new ManipulationService(_context);
        var availability = await service.CheckStockAvailability(id);

        var result = new StockCheckDto
        {
            OrderId = availability.OrderId,
            OrderNumber = availability.OrderNumber,
            AllAvailable = availability.AllAvailable,
            Items = new List<StockCheckItemDto>()
        };

        foreach (var item in availability.Items)
        {
            // Buscar lotes disponíveis
            var batches = await _context.Batches
                .Include(b => b.Supplier)
                .Where(b => b.RawMaterialId == item.RawMaterialId &&
                           b.Status.ToUpper() == "APROVADO" &&
                           b.CurrentQuantity > 0 &&
                           b.ExpiryDate > DateTime.UtcNow)
                .OrderBy(b => b.ExpiryDate)
                .Take(5)
                .Select(b => new AvailableBatchDto
                {
                    BatchId = b.Id,
                    BatchNumber = b.BatchNumber,
                    AvailableQuantity = b.CurrentQuantity,
                    ExpiryDate = b.ExpiryDate,
                    DaysUntilExpiry = (int)(b.ExpiryDate - DateTime.UtcNow).TotalDays,
                    SupplierName = b.Supplier != null ? b.Supplier.TradeName : null
                })
                .ToListAsync();

            // Buscar DCB code
            var rawMaterial = await _context.RawMaterials.FindAsync(item.RawMaterialId);

            result.Items.Add(new StockCheckItemDto
            {
                RawMaterialId = item.RawMaterialId,
                RawMaterialName = item.RawMaterialName,
                DcbCode = rawMaterial?.DcbCode,
                QuantityNeeded = item.QuantityNeeded,
                QuantityAvailable = item.QuantityAvailable,
                Unit = item.Unit,
                IsAvailable = item.IsAvailable,
                Shortage = item.Shortage,
                AvailableBatches = batches
            });
        }

        return Ok(ApiResponse<StockCheckDto>.SuccessResponse(result));
    }

    // ===================================================================
    // CUSTO DE MANIPULAÇÃO
    // ===================================================================

    /// <summary>
    /// Calcula custo de manipulação
    /// </summary>
    [HttpGet("{id}/cost")]
    public async Task<ActionResult<ApiResponse<ManipulationCostDto>>> CalculateCost(Guid id)
    {
        if (!await OrderInScope(id))
            return NotFound(ApiResponse<ManipulationCostDto>.ErrorResponse("Ordem não encontrada"));

        var service = new ManipulationService(_context);
        var cost = await service.CalculateManipulationCost(id);

        var order = await _context.ManipulationOrders.FindAsync(id);

        var result = new ManipulationCostDto
        {
            OrderId = cost.OrderId,
            OrderNumber = cost.OrderNumber,
            QuantityProduced = cost.QuantityProduced,
            Unit = order?.Unit ?? "UN",
            TotalMaterialCost = cost.TotalMaterialCost,
            LaborCost = cost.LaborCost,
            OverheadCost = cost.OverheadCost,
            TotalCost = cost.TotalCost,
            UnitCost = cost.UnitCost,
            SuggestedPrice = cost.TotalCost * 2.5m, // Margem de 150%
            ProfitMargin = 150,
            ComponentCosts = cost.ComponentCosts.Select(c => new ComponentCostDto
            {
                RawMaterialId = c.RawMaterialId,
                RawMaterialName = c.RawMaterialName,
                Quantity = c.Quantity,
                Unit = c.Unit,
                UnitCost = c.UnitCost,
                TotalCost = c.TotalCost,
                PercentageOfTotal = cost.TotalMaterialCost > 0 
                    ? (c.TotalCost / cost.TotalMaterialCost) * 100 
                    : 0
            }).ToList()
        };

        return Ok(ApiResponse<ManipulationCostDto>.SuccessResponse(result));
    }
}
