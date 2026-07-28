using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;
using Models.Employees;
using DTOs;
using DTOs.Pharmacy.ManipulationOrders;
using System.Text.Json;

namespace Controllers;

/// <summary>
/// Controller do workflow de manipulação - etapas finais
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ManipulationWorkflowController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ManipulationWorkflowController> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ManipulationWorkflowController(
        AppDbContext context,
        ILogger<ManipulationWorkflowController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    // Métodos auxiliares para pegar informações do contexto
    private Guid GetEstablishmentId()
    {
        // Pega do cookie/claim/header - ajuste conforme sua implementação
        var establishmentIdStr = _httpContextAccessor.HttpContext?.Request.Cookies["EstablishmentId"];
        if (Guid.TryParse(establishmentIdStr, out var establishmentId))
            return establishmentId;

        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    private Guid GetEmployeeId()
    {
        var employeeIdStr = _httpContextAccessor.HttpContext?.Request.Cookies["EmployeeId"];
        if (Guid.TryParse(employeeIdStr, out var employeeId))
            return employeeId;

        throw new UnauthorizedAccessException("EmployeeId nao encontrado na sessao");
    }

    /// <summary>
    /// POST /api/ManipulationWorkflow/{id}/complete-mistura
    /// </summary>
    [HttpPost("{id}/complete-mistura")]
    public async Task<ActionResult<ApiResponse>> CompleteMistura(Guid id, [FromBody] StartMisturaDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var employeeId = GetEmployeeId();

            var order = await _context.ManipulationOrders
                .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

            if (order == null)
                return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

            // Verificar etapa anterior
            var pesagemConcluida = await _context.ManipulationSteps
                .AnyAsync(s => s.ManipulationOrderId == id &&
                              s.StepType == "PESAGEM" &&
                              s.Status == "CONCLUIDA");

            if (!pesagemConcluida)
                return BadRequest(ApiResponse.ErrorResponse("PESAGEM deve estar concluída"));

            var stepData = new MisturaStepData
            {
                MetodoMistura = dto.MixingMethod,
                EquipamentoUtilizado = dto.Equipment,
                TempoMistura = dto.MixingDuration,
                VelocidadeMistura = dto.MixingSpeed,
                Observacoes = dto.Observations,
                InicioMistura = DateTime.UtcNow,
                FimMistura = DateTime.UtcNow
            };

            var step = new ManipulationStep
            {
                ManipulationOrderId = id,
                StepType = "MISTURA",
                StepNumber = 2,
                Status = "CONCLUIDA",
                PerformedByEmployeeId = employeeId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                StepData = JsonSerializer.Serialize(stepData),
                Observations = dto.Observations,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationSteps.Add(step);
            order.Status = "MISTURA";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Mistura concluída - Ordem: {OrderId}", id);

            return Ok(ApiResponse.SuccessResponse("Mistura concluída com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar mistura");
            return StatusCode(500, ApiResponse.ErrorResponse("Erro interno"));
        }
    }

    /// <summary>
    /// POST /api/ManipulationWorkflow/{id}/complete-envase
    /// </summary>
    [HttpPost("{id}/complete-envase")]
    public async Task<ActionResult<ApiResponse>> CompleteEnvase(Guid id, [FromBody] StartEnvaseDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var employeeId = GetEmployeeId();

            var order = await _context.ManipulationOrders
                .Include(o => o.Formula)
                .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

            if (order == null)
                return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

            // Verificar etapa anterior
            var misturaConcluida = await _context.ManipulationSteps
                .AnyAsync(s => s.ManipulationOrderId == id &&
                              s.StepType == "MISTURA" &&
                              s.Status == "CONCLUIDA");

            if (!misturaConcluida)
                return BadRequest(ApiResponse.ErrorResponse("MISTURA deve estar concluída"));

            // Calcular rendimento
            decimal rendimento = 0;
            if (order.QuantityToProduce > 0)
            {
                rendimento = (dto.PackagedQuantity / order.QuantityToProduce) * 100;

                if (rendimento < 95 || rendimento > 105)
                {
                    _logger.LogWarning("Rendimento: {Rendimento}% - Ordem: {OrderId}", rendimento, id);
                }
            }

            var stepData = new EnvaseStepData
            {
                TipoEmbalagem = dto.PackagingType,
                QuantidadeEnvasada = dto.PackagedQuantity,
                NumeroLote = dto.BatchNumber,
                DataFabricacao = dto.ManufacturingDate,
                DataValidade = dto.ExpiryDate,
                Rendimento = rendimento,
                Observacoes = dto.Observations
            };

            var step = new ManipulationStep
            {
                ManipulationOrderId = id,
                StepType = "ENVASE",
                StepNumber = 3,
                Status = "CONCLUIDA",
                PerformedByEmployeeId = employeeId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                StepData = JsonSerializer.Serialize(stepData),
                Observations = dto.Observations,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationSteps.Add(step);
            order.Status = "ENVASE";
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Envase concluído - Ordem: {OrderId}, Rendimento: {Rendimento}%", id, rendimento);

            var response = new
            {
                Message = "Envase concluído com sucesso",
                Rendimento = rendimento,
                AlertaRendimento = rendimento < 95 || rendimento > 105
            };

            return Ok(ApiResponse<object>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar envase");
            return StatusCode(500, ApiResponse.ErrorResponse("Erro interno"));
        }
    }

    /// <summary>
    /// POST /api/ManipulationWorkflow/{id}/complete-conferencia
    /// </summary>
    [HttpPost("{id}/complete-conferencia")]
    public async Task<ActionResult<ApiResponse>> CompleteConferencia(Guid id, [FromBody] StartConferenciaDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var employeeId = GetEmployeeId();

            var order = await _context.ManipulationOrders
                .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

            if (order == null)
                return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

            // Verificar etapa anterior
            var rotulagemConcluida = await _context.ManipulationSteps
                .AnyAsync(s => s.ManipulationOrderId == id &&
                              s.StepType == "ROTULAGEM" &&
                              s.Status == "CONCLUIDA");

            if (!rotulagemConcluida)
                return BadRequest(ApiResponse.ErrorResponse("ROTULAGEM deve estar concluída"));

            // Validar farmacêutico
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return BadRequest(ApiResponse.ErrorResponse("Funcionário não encontrado"));

            var position = employee.JobPosition.ToString();
            if (position != "PHARMACIST" && position != "PHARMACIST_RT")
                return BadRequest(ApiResponse.ErrorResponse("Somente farmacêuticos podem conferir"));

            var stepData = new ConferenciaStepData
            {
                AspectosVisuais = dto.VisualAspects,
                EmbalagemIntegra = dto.PackagingIntact,
                RotuloCorreto = dto.LabelCorrect,
                QuantidadeCorreta = dto.QuantityCorrect,
                DocumentacaoCompleta = dto.DocumentationComplete,
                AprovadoPorFarmaceutico = dto.ApprovedByPharmacist,
                FarmaceuticoResponsavel = dto.PharmacistName,
                CRF = dto.PharmacistCRF,
                Observacoes = dto.Observations
            };

            var step = new ManipulationStep
            {
                ManipulationOrderId = id,
                StepType = "CONFERENCIA",
                StepNumber = 5,
                Status = "CONCLUIDA",
                PerformedByEmployeeId = employeeId,
                CheckedByEmployeeId = dto.PharmacistEmployeeId ?? employeeId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CheckedAt = DateTime.UtcNow,
                StepData = JsonSerializer.Serialize(stepData),
                Observations = dto.Observations,
                PassedIntermediateCheck = dto.ApprovedByPharmacist,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationSteps.Add(step);

            order.Status = dto.ApprovedByPharmacist ? "APROVADO" : "CONFERENCIA_PENDENTE";
            order.PassedQualityControl = dto.ApprovedByPharmacist;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Conferência concluída - Ordem: {OrderId}, Aprovado: {Aprovado}",
                id, dto.ApprovedByPharmacist);

            return Ok(ApiResponse.SuccessResponse(
                dto.ApprovedByPharmacist
                    ? "Conferência aprovada"
                    : "Conferência registrada - correções necessárias"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao completar conferência");
            return StatusCode(500, ApiResponse.ErrorResponse("Erro interno"));
        }
    }

    /// <summary>
    /// POST /api/ManipulationWorkflow/{id}/approve-final
    /// </summary>
    [HttpPost("{id}/approve-final")]
    public async Task<ActionResult<ApiResponse>> ApproveFinal(Guid id, [FromBody] StartAprovacaoDto dto)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var order = await _context.ManipulationOrders
                .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

            if (order == null)
                return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

            // Verificar conferência
            var conferenciaConcluida = await _context.ManipulationSteps
                .AnyAsync(s => s.ManipulationOrderId == id &&
                              s.StepType == "CONFERENCIA" &&
                              s.Status == "CONCLUIDA" &&
                              s.PassedIntermediateCheck == true);

            if (!conferenciaConcluida)
                return BadRequest(ApiResponse.ErrorResponse("CONFERÊNCIA aprovada é obrigatória"));

            // Validar farmacêutico
            var farmaceutico = await _context.Employees.FindAsync(dto.FarmaceuticoId);
            if (farmaceutico == null)
                return BadRequest(ApiResponse.ErrorResponse("Farmacêutico não encontrado"));

            var position = farmaceutico.JobPosition.ToString();
            if (position != "PHARMACIST" && position != "PHARMACIST_RT")
                return BadRequest(ApiResponse.ErrorResponse("Somente farmacêuticos RT podem aprovar"));

            if (!dto.Aprovado && string.IsNullOrWhiteSpace(dto.MotivoReprovacao))
                return BadRequest(ApiResponse.ErrorResponse("Motivo de reprovação é obrigatório"));

            var stepData = new AprovacaoStepData
            {
                Aprovado = dto.Aprovado,
                FarmaceuticoId = dto.FarmaceuticoId,
                FarmaceuticoNome = dto.FarmaceuticoNome,
                CRF = dto.CRF,
                MotivoReprovacao = dto.MotivoReprovacao,
                AcoesCorretivas = dto.AcoesCorretivas,
                Observacoes = dto.Observacoes,
                DataAprovacao = DateTime.UtcNow,
                AssinaturaDigital = dto.AssinaturaDigital
            };

            var step = new ManipulationStep
            {
                ManipulationOrderId = id,
                StepType = "APROVACAO",
                StepNumber = 6,
                Status = dto.Aprovado ? "CONCLUIDA" : "REJEITADA",
                PerformedByEmployeeId = dto.FarmaceuticoId,
                CheckedByEmployeeId = dto.FarmaceuticoId,
                StartedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow,
                CheckedAt = DateTime.UtcNow,
                StepData = JsonSerializer.Serialize(stepData),
                Observations = dto.Observacoes,
                PassedIntermediateCheck = dto.Aprovado,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.ManipulationSteps.Add(step);

            if (dto.Aprovado)
            {
                order.Status = "FINALIZADO";
                order.PassedQualityControl = true;
                order.ApprovedByPharmacistId = dto.FarmaceuticoId;
                order.CompletionDate = DateTime.UtcNow;
                _logger.LogInformation("Ordem APROVADA: {OrderId}", id);
            }
            else
            {
                order.Status = "REJEITADO";
                order.PassedQualityControl = false;
                _logger.LogWarning("Ordem REJEITADA: {OrderId}, Motivo: {Motivo}", id, dto.MotivoReprovacao);
            }

            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(ApiResponse.SuccessResponse(
                dto.Aprovado
                    ? "✅ Manipulação APROVADA e FINALIZADA!"
                    : "❌ Manipulação REJEITADA"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar final");
            return StatusCode(500, ApiResponse.ErrorResponse("Erro interno"));
        }
    }

    /// <summary>
    /// GET /api/ManipulationWorkflow/{id}/progress
    /// </summary>
    [HttpGet("{id}/progress")]
    public async Task<ActionResult<ApiResponse<object>>> GetProgress(Guid id)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var order = await _context.ManipulationOrders
                .Include(o => o.Formula)
                .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId);

            if (order == null)
                return NotFound(ApiResponse<object>.ErrorResponse("Ordem não encontrada"));

            var steps = await _context.ManipulationSteps
                .Where(s => s.ManipulationOrderId == id)
                .OrderBy(s => s.StepNumber)
                .Select(s => new
                {
                    s.StepType,
                    s.Status,
                    s.StartedAt,
                    s.CompletedAt,
                    s.Observations
                })
                .ToListAsync();

            var allSteps = new[] { "SEPARACAO", "PESAGEM", "MISTURA", "ENVASE", "ROTULAGEM", "CONFERENCIA", "APROVACAO" };
            var completedSteps = steps.Count(s => s.Status == "CONCLUIDA");
            var progress = (decimal)completedSteps / allSteps.Length * 100;

            var result = new
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                FormulaName = order.Formula?.Name,
                Progress = Math.Round(progress, 2),
                CompletedSteps = completedSteps,
                TotalSteps = allSteps.Length,
                Steps = steps,
                CreatedAt = order.CreatedAt,
                CompletionDate = order.CompletionDate
            };

            return Ok(ApiResponse<object>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar progresso");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno"));
        }
    }

    /// <summary>
    /// GET /api/ManipulationWorkflow/dashboard
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult<ApiResponse<object>>> GetDashboard()
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var totalPendentes = await _context.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "PENDENTE");

            var totalEmProducao = await _context.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId &&
                    (o.Status == "SEPARACAO" || o.Status == "PESAGEM" || o.Status == "MISTURA" ||
                     o.Status == "ENVASE" || o.Status == "ROTULAGEM" || o.Status == "CONFERENCIA"));

            var totalFinalizados = await _context.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "FINALIZADO");

            var totalRejeitados = await _context.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && o.Status == "REJEITADO");

            var dashboard = new
            {
                TotalPendentes = totalPendentes,
                TotalEmProducao = totalEmProducao,
                TotalFinalizados = totalFinalizados,
                TotalRejeitados = totalRejeitados,
                TaxaAprovacao = (totalFinalizados + totalRejeitados) > 0
                    ? Math.Round(((decimal)totalFinalizados / (totalFinalizados + totalRejeitados)) * 100, 2)
                    : 0
            };

            return Ok(ApiResponse<object>.SuccessResponse(dashboard));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar dashboard");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Erro interno"));
        }
    }
}