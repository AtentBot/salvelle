using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Pharmacy.ManipulationOrders;
using Models.Pharmacy;
using System.Text.Json;
using Service;
using DTOs;

namespace Controllers;

/// <summary>
/// Extensão do ManipulationOrdersController com endpoints de workflow
/// VERSÃO CORRIGIDA - Adaptada aos models existentes (Batch, ManipulationOrder, StockMovement)
/// </summary>
public partial class ManipulationOrdersController
{
    // ===================================================================
    // HELPER METHODS - WORKFLOW
    // ===================================================================

    [HttpGet("{id}/calculate-expiry")]
    public async Task<ActionResult<ApiResponse<DateTime>>> CalculateExpiryDate(Guid id)
    {
        var service = new ManipulationService(_context);
        var expiryDate = await service.CalculateExpiryDate(id);
        return Ok(ApiResponse<DateTime>.SuccessResponse(expiryDate));
    }

    [HttpGet("{id}/generate-batch-number")]
    public ActionResult<ApiResponse<string>> GenerateBatchNumber(Guid id)
    {
        var service = new ManipulationService(_context);
        var establishmentId = GetEstablishmentId();
        var batchNumber = service.GenerateBatchNumber(establishmentId);
        return Ok(ApiResponse<string>.SuccessResponse(batchNumber));
    }

    [HttpGet("{id}/workflow-status")]
    public async Task<ActionResult<ApiResponse<object>>> GetWorkflowStatus(Guid id)
    {
        var service = new ManipulationService(_context);

        var steps = await _context.ManipulationSteps
            .Where(s => s.ManipulationOrderId == id)
            .GroupBy(s => s.StepType)
            .Select(g => new
            {
                StepType = g.Key,
                Status = g.OrderByDescending(s => s.CreatedAt).First().Status,
                CompletedAt = g.OrderByDescending(s => s.CreatedAt).First().CompletedAt,
                PassedCheck = g.OrderByDescending(s => s.CreatedAt).First().PassedIntermediateCheck
            })
            .ToListAsync();

        var allCompleted = await service.AllStepsCompleted(id);

        var result = new
        {
            Steps = steps,
            AllStepsCompleted = allCompleted,
            CanProceed = steps.Any() && steps.All(s => s.Status == "CONCLUIDA" && s.PassedCheck != false)
        };

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    // ===================================================================
    // WORKFLOW - GERAL
    // ===================================================================

    [HttpGet("{id}/weighing-sheet")]
    public async Task<ActionResult> GetWeighingSheet(Guid id)
    {
        try
        {
            var service = new WeighingSheetService(_context);
            var html = await service.GenerateWeighingSheetHtml(id);
            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse($"Erro ao gerar ficha: {ex.Message}"));
        }
    }

    [HttpGet("{id}/steps")]
    public async Task<ActionResult<ApiResponse<List<ManipulationStepDto>>>> GetOrderSteps(Guid id)
    {
        var steps = await _context.ManipulationSteps
            .Include(s => s.PerformedByEmployee)
            .Include(s => s.CheckedByEmployee)
            .Where(s => s.ManipulationOrderId == id)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new ManipulationStepDto
            {
                Id = s.Id,
                ManipulationOrderId = s.ManipulationOrderId,
                StepType = s.StepType,
                Status = s.Status,
                PerformedByEmployeeId = s.PerformedByEmployeeId,
                PerformedByEmployeeName = s.PerformedByEmployee!.FullName,
                StartedAt = s.StartedAt,
                CompletedAt = s.CompletedAt,
                StepData = s.StepData,
                Observations = s.Observations,
                PassedIntermediateCheck = s.PassedIntermediateCheck,
                CheckedByEmployeeId = s.CheckedByEmployeeId,
                CheckedByEmployeeName = s.CheckedByEmployee != null ? s.CheckedByEmployee.FullName : null,
                CheckNotes = s.CheckNotes,
                CheckedAt = s.CheckedAt,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<ManipulationStepDto>>.SuccessResponse(steps));
    }

    // ===================================================================
    // ETAPA 0: SEPARAÇÃO
    // ===================================================================

    [HttpPost("{id}/steps/separacao/start")]
    public async Task<ActionResult<ApiResponse>> StartSeparacao(Guid id, [FromBody] StartSeparacaoDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        if (order.Status != "PENDENTE")
            return BadRequest(ApiResponse.ErrorResponse("Ordem deve estar PENDENTE para iniciar separação"));

        // Validar disponibilidade no estoque usando CurrentQuantity
        foreach (var item in dto.Items)
        {
            var batch = await _context.Batches
                .FirstOrDefaultAsync(b => b.Id == item.BatchId && b.RawMaterialId == item.RawMaterialId);

            if (batch == null)
                return BadRequest(ApiResponse.ErrorResponse($"Lote {item.BatchNumber} não encontrado"));

            if (batch.CurrentQuantity < item.QuantitySeparated)
                return BadRequest(ApiResponse.ErrorResponse(
                    $"Quantidade insuficiente no lote {item.BatchNumber}. Disponível: {batch.CurrentQuantity}"));
        }

        var stepData = new SeparacaoStepData
        {
            Items = dto.Items.Select(i => new ItemSeparado
            {
                RawMaterialId = i.RawMaterialId,
                BatchId = i.BatchId,
                LoteInsumo = i.BatchNumber,
                QuantidadeNecessaria = i.QuantityRequired,
                QuantidadeSeparada = i.QuantitySeparated,
                Unidade = i.Unit,
                LocalArmazenagem = i.StorageLocation,
                RequerRefrigeracao = i.RequiresRefrigeration,
                Controlado = i.IsControlled
            }).ToList(),
            AreaSeparacao = dto.AreaSeparacao,
            DataSeparacao = DateTime.UtcNow,
            TodosItensConferidos = true
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "SEPARACAO",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        order.Status = "SEPARACAO";
        if (!order.StartDate.HasValue)
            order.StartDate = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Separação de materiais concluída com sucesso"));
    }

    // ===================================================================
    // ETAPA 1: PESAGEM
    // ===================================================================

    [HttpPost("{id}/steps/pesagem/start")]
    public async Task<ActionResult<ApiResponse<ManipulationStepDto>>> StartPesagem(Guid id, [FromBody] StartPesagemDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse<ManipulationStepDto>.ErrorResponse("Ordem não encontrada"));

        if (order.Status != "PENDENTE" && order.Status != "EM_PRODUCAO" && order.Status != "SEPARACAO")
            return BadRequest(ApiResponse<ManipulationStepDto>.ErrorResponse(
                "Ordem deve estar PENDENTE, SEPARACAO ou EM_PRODUCAO"));

        var stepData = new PesagemStepData
        {
            Components = dto.Components.Select(c => new ComponentPesado
            {
                RawMaterialId = c.RawMaterialId,
                BatchId = c.BatchId,
                QuantidadeEsperada = c.ExpectedWeight,
                QuantidadePesada = c.ActualWeight,
                Unidade = c.Unit,
                LoteInsumo = c.BatchNumber
            }).ToList(),
            BalancaId = dto.ScaleId,
            BalancaNome = dto.ScaleName,
            AmbienteTemperatura = dto.EnvironmentTemperature,
            AmbienteUmidade = dto.EnvironmentHumidity
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "PESAGEM",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        order.Status = "PESAGEM";
        if (!order.StartDate.HasValue)
            order.StartDate = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<ManipulationStepDto>.SuccessResponse(null!, "Pesagem concluída com sucesso"));
    }

    [HttpPost("{id}/steps/pesagem/check")]
    public async Task<ActionResult<ApiResponse>> CheckPesagem(Guid id, [FromBody] CheckPesagemDto dto)
    {
        var step = await _context.ManipulationSteps
            .FirstOrDefaultAsync(s => s.ManipulationOrderId == id && s.StepType == "PESAGEM");

        if (step == null)
            return NotFound(ApiResponse.ErrorResponse("Etapa de pesagem não encontrada"));

        step.PassedIntermediateCheck = dto.Passed;
        step.CheckedByEmployeeId = dto.CheckedByEmployeeId;
        step.CheckNotes = dto.CheckNotes;
        step.CheckedAt = DateTime.UtcNow;
        step.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(dto.Passed ? "Pesagem aprovada" : "Pesagem reprovada"));
    }

    // ===================================================================
    // ETAPA 2: MISTURA
    // ===================================================================

    [HttpPost("{id}/steps/mistura/start")]
    public async Task<ActionResult<ApiResponse>> StartMistura(Guid id, [FromBody] StartMisturaDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new MisturaStepData
        {
            MetodoMistura = dto.MixingMethod,
            EquipamentoUtilizado = dto.Equipment,
            TempoMistura = dto.MixingDuration,
            VelocidadeMistura = dto.MixingSpeed,
            Observacoes = dto.Observations
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "MISTURA",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "MISTURA";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Mistura concluída com sucesso"));
    }

    // ===================================================================
    // ETAPA 3: ENVASE
    // ===================================================================

    [HttpPost("{id}/steps/envase/start")]
    public async Task<ActionResult<ApiResponse>> StartEnvase(Guid id, [FromBody] StartEnvaseDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new EnvaseStepData
        {
            TipoEmbalagem = dto.PackagingType,
            QuantidadeEnvasada = dto.PackagedQuantity,
            NumeroLote = dto.BatchNumber,
            DataFabricacao = dto.ManufacturingDate,
            DataValidade = dto.ExpiryDate,
            Observacoes = dto.Observations
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "ENVASE",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "ENVASE";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Envase concluído com sucesso"));
    }

    // ===================================================================
    // ETAPA 4: ROTULAGEM
    // ===================================================================

    [HttpPost("{id}/steps/rotulagem/start")]
    public async Task<ActionResult<ApiResponse>> StartRotulagem(Guid id, [FromBody] StartRotulagemDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new RotulagemStepData
        {
            NumeroLote = dto.BatchNumber,
            DataFabricacao = dto.ManufacturingDate,
            DataValidade = dto.ExpiryDate,
            InformacoesAdicionais = dto.AdditionalInfo,
            CodigoBarras = dto.Barcode
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "ROTULAGEM",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "ROTULAGEM";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Rotulagem concluída com sucesso"));
    }

    // ===================================================================
    // ETAPA 5: CONFERÊNCIA FINAL
    // ===================================================================

    [HttpPost("{id}/steps/conferencia/start")]
    public async Task<ActionResult<ApiResponse>> StartConferencia(Guid id, [FromBody] StartConferenciaDto dto)
    {
        var employeeId = GetEmployeeId();

        var stepData = new ConferenciaStepData
        {
            AspectosVisuais = dto.VisualAspects,
            EmbalagemIntegra = dto.PackagingIntact,
            RotuloCorreto = dto.LabelCorrect,
            QuantidadeCorreta = dto.QuantityCorrect,
            DocumentacaoCompleta = dto.DocumentationComplete,
            Observacoes = dto.Observations,
            AprovadoPorFarmaceutico = dto.ApprovedByPharmacist,
            FarmaceuticoResponsavel = dto.PharmacistName,
            CRF = dto.PharmacistCRF
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "CONFERENCIA",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            PassedIntermediateCheck = dto.ApprovedByPharmacist,
            CheckedByEmployeeId = dto.PharmacistEmployeeId,
            CheckedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = dto.ApprovedByPharmacist ? "FINALIZADO" : "CONFERENCIA";
            order.PassedQualityControl = dto.ApprovedByPharmacist;
            order.ApprovedByPharmacistId = dto.PharmacistEmployeeId;

            if (dto.ApprovedByPharmacist && !order.CompletionDate.HasValue)
                order.CompletionDate = DateTime.UtcNow;

            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            dto.ApprovedByPharmacist ? "Conferência aprovada - Ordem finalizada" : "Conferência registrada"));
    }

    // ===================================================================
    // FOTOS
    // ===================================================================

    [HttpPost("{id}/steps/{stepType}/photos")]
    public async Task<ActionResult<ApiResponse>> AddPhoto(Guid id, string stepType, [FromBody] AddPhotoDto dto)
    {
        var employeeId = GetEmployeeId();

        var photo = new ManipulationPhoto
        {
            ManipulationOrderId = id,
            StepType = stepType.ToUpper(),
            PhotoUrl = dto.PhotoUrl,
            ThumbnailUrl = dto.ThumbnailUrl,
            Description = dto.Description,
            CapturedByEmployeeId = employeeId,
            CapturedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.ManipulationPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Foto adicionada com sucesso"));
    }

    [HttpDelete("photos/{photoId}")]
    public async Task<ActionResult<ApiResponse>> DeletePhoto(Guid photoId)
    {
        var photo = await _context.ManipulationPhotos.FindAsync(photoId);

        if (photo == null)
            return NotFound(ApiResponse.ErrorResponse("Foto não encontrada"));

        _context.ManipulationPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Foto removida com sucesso"));
    }

    // ===================================================================
    // ETAPA 6: APROVAÇÃO FINAL DO FARMACÊUTICO
    // ===================================================================

    [HttpPost("{id}/steps/aprovacao/start")]
    public async Task<ActionResult<ApiResponse>> StartAprovacao(Guid id, [FromBody] StartAprovacaoDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var conferenciaStep = await _context.ManipulationSteps
            .FirstOrDefaultAsync(s => s.ManipulationOrderId == id && s.StepType == "CONFERENCIA");

        if (conferenciaStep == null || conferenciaStep.Status != "CONCLUIDA")
            return BadRequest(ApiResponse.ErrorResponse("Conferência deve ser concluída antes"));

        var pharmacist = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == dto.PharmacistEmployeeId);

        if (pharmacist == null)
            return BadRequest(ApiResponse.ErrorResponse("Farmacêutico não encontrado"));

        if (string.IsNullOrEmpty(dto.PharmacistCRF))
            return BadRequest(ApiResponse.ErrorResponse("CRF obrigatório"));

        var stepData = new AprovacaoStepData
        {
            FarmaceuticoId = dto.PharmacistEmployeeId,
            NomeFarmaceutico = dto.PharmacistName,
            CRF = dto.PharmacistCRF,
            InspecaoVisualOk = dto.VisualInspectionPassed,
            DocumentacaoCompleta = dto.DocumentationComplete,
            RotulagemCorreta = dto.LabelingCorrect,
            EmbalagemIntegra = dto.PackagingIntact,
            Aprovado = dto.Approved,
            MotivoRejeicao = dto.RejectionReason,
            AssinaturaDigital = dto.DigitalSignature,
            DataAprovacao = DateTime.UtcNow
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "APROVACAO",
            Status = dto.Approved ? "CONCLUIDA" : "REJEITADA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            PassedIntermediateCheck = dto.Approved,
            CheckedByEmployeeId = dto.PharmacistEmployeeId,
            CheckedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        // Usar QualityNotes para motivo de rejeição (ManipulationOrder não tem RejectionReason)
        order.Status = dto.Approved ? "APROVADO" : "REJEITADO";
        order.PassedQualityControl = dto.Approved;
        order.ApprovedByPharmacistId = dto.PharmacistEmployeeId;
        order.UpdatedAt = DateTime.UtcNow;

        if (!dto.Approved && !string.IsNullOrEmpty(dto.RejectionReason))
        {
            order.QualityNotes = $"REJEITADO: {dto.RejectionReason}";
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            dto.Approved ? "Aprovado pelo farmacêutico" : $"Rejeitado: {dto.RejectionReason}"));
    }

    // ===================================================================
    // ETAPA 7: EXPEDIÇÃO
    // ===================================================================

    [HttpPost("{id}/steps/expedicao/start")]
    public async Task<ActionResult<ApiResponse>> StartExpedicao(Guid id, [FromBody] StartExpedicaoDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var order = await _context.ManipulationOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        if (order.Status != "APROVADO" && order.Status != "FINALIZADO")
            return BadRequest(ApiResponse.ErrorResponse("Ordem deve estar APROVADA"));

        var stepData = new ExpedicaoStepData
        {
            MetodoEntrega = dto.DeliveryMethod,
            CodigoRastreio = dto.TrackingCode,
            NomeEntregador = dto.DeliveryPersonName,
            TelefoneEntregador = dto.DeliveryPersonPhone,
            EnderecoEntrega = dto.DeliveryAddress,
            PrevisaoEntrega = dto.EstimatedDeliveryDate,
            NomeRecebedor = dto.ReceiverName,
            DocumentoRecebedor = dto.ReceiverDocument,
            ClienteNotificado = dto.CustomerNotified,
            MetodoNotificacao = dto.NotificationMethod,
            EntregaConfirmada = false
        };

        var step = new ManipulationStep
        {
            ManipulationOrderId = id,
            StepType = "EXPEDICAO",
            Status = "CONCLUIDA",
            PerformedByEmployeeId = employeeId,
            StartedAt = dto.StartTime ?? DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            StepData = JsonSerializer.Serialize(stepData),
            Observations = dto.Observations,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationSteps.Add(step);

        order.Status = "FINALIZADO";
        order.CompletionDate = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        // Baixar estoque usando propriedades existentes do StockMovement
        var separacaoStep = await _context.ManipulationSteps
            .FirstOrDefaultAsync(s => s.ManipulationOrderId == id && s.StepType == "SEPARACAO");

        if (separacaoStep?.StepData != null)
        {
            var separacaoData = JsonSerializer.Deserialize<SeparacaoStepData>(separacaoStep.StepData);
            if (separacaoData?.Items != null)
            {
                foreach (var item in separacaoData.Items)
                {
                    var batch = await _context.Batches
                        .Include(b => b.RawMaterial)
                        .FirstOrDefaultAsync(b => b.Id == item.BatchId);

                    if (batch != null)
                    {
                        var stockBefore = batch.CurrentQuantity;
                        batch.CurrentQuantity -= item.QuantidadeSeparada;

                        // StockMovement usa propriedades: Reason, ManipulationOrderId, DocumentNumber
                        var movement = new StockMovement
                        {
                            EstablishmentId = establishmentId,
                            RawMaterialId = batch.RawMaterialId,
                            BatchId = batch.Id,
                            MovementType = "MANIPULACAO",
                            Quantity = -item.QuantidadeSeparada,
                            StockBefore = stockBefore,
                            StockAfter = batch.CurrentQuantity,
                            Reason = $"Baixa expedição - OM {order.OrderNumber}",
                            ManipulationOrderId = order.Id,
                            DocumentNumber = order.OrderNumber,
                            PerformedByEmployeeId = employeeId,
                            MovementDate = DateTime.UtcNow,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.StockMovements.Add(movement);
                    }
                }
            }
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Expedição registrada. Ordem finalizada."));
    }

    [HttpPost("{id}/steps/expedicao/confirm-delivery")]
    public async Task<ActionResult<ApiResponse>> ConfirmDelivery(Guid id, [FromBody] ConfirmDeliveryDto dto)
    {
        var step = await _context.ManipulationSteps
            .FirstOrDefaultAsync(s => s.ManipulationOrderId == id && s.StepType == "EXPEDICAO");

        if (step == null)
            return NotFound(ApiResponse.ErrorResponse("Expedição não encontrada"));

        if (step.StepData != null)
        {
            var stepData = JsonSerializer.Deserialize<ExpedicaoStepData>(step.StepData);
            if (stepData != null)
            {
                stepData.DataEntregaEfetiva = dto.DeliveryDate;
                stepData.NomeRecebedor = dto.ReceiverName;
                stepData.DocumentoRecebedor = dto.ReceiverDocument;
                stepData.AssinaturaRecebedor = dto.ReceiverSignature;
                stepData.EntregaConfirmada = true;

                step.StepData = JsonSerializer.Serialize(stepData);
                step.Observations = dto.Observations ?? step.Observations;
                step.UpdatedAt = DateTime.UtcNow;
            }
        }

        var order = await _context.ManipulationOrders.FindAsync(id);
        if (order != null)
        {
            order.Status = "ENTREGUE";
            order.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Entrega confirmada"));
    }

    // ===================================================================
    // DASHBOARD DO WORKFLOW
    // ===================================================================

    [HttpGet("workflow-dashboard")]
    public async Task<ActionResult<ApiResponse<WorkflowDashboardDto>>> GetWorkflowDashboard()
    {
        var establishmentId = GetEstablishmentId();
        var today = DateTime.UtcNow.Date;

        var orders = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId)
            .ToListAsync();

        // Usar ExpectedDate (ManipulationOrder não tem EstimatedCompletionDate)
        var result = new WorkflowDashboardDto
        {
            TotalPendentes = orders.Count(o => o.Status == "PENDENTE"),
            TotalEmProducao = orders.Count(o => o.Status != "PENDENTE" &&
                                                o.Status != "FINALIZADO" &&
                                                o.Status != "ENTREGUE" &&
                                                o.Status != "CANCELADO"),
            TotalFinalizadosHoje = orders.Count(o => o.Status == "FINALIZADO" &&
                                                     o.CompletionDate?.Date == today),
            TotalAtrasados = orders.Count(o => o.ExpectedDate < DateTime.UtcNow &&
                                               o.Status != "FINALIZADO" &&
                                               o.Status != "ENTREGUE" &&
                                               o.Status != "CANCELADO"),
            ProducaoHoje = orders.Count(o => o.StartDate?.Date == today),
            OrdersByStatus = orders
                .GroupBy(o => o.Status)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        return Ok(ApiResponse<WorkflowDashboardDto>.SuccessResponse(result));
    }
}

public class WorkflowDashboardDto
{
    public int TotalPendentes { get; set; }
    public int TotalEmProducao { get; set; }
    public int TotalFinalizadosHoje { get; set; }
    public int TotalAtrasados { get; set; }
    public int ProducaoHoje { get; set; }
    public Dictionary<string, int> OrdersByStatus { get; set; } = new();
}