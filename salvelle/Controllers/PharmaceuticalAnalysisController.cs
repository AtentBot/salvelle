using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DTOs;
using DTOs.CustomerFormulas;
using Service.CustomerFormulas;

namespace Controllers;

[Route("api/pharmaceutical-analysis")]
[ApiController]
[Authorize] // Requer autenticação
public class PharmaceuticalAnalysisController : ControllerBase
{
    private readonly PharmaceuticalAnalysisService _analysisService;
    private readonly CustomFormulaService _formulaService;
    private readonly ILogger<PharmaceuticalAnalysisController> _logger;

    public PharmaceuticalAnalysisController(
        PharmaceuticalAnalysisService analysisService,
        CustomFormulaService formulaService,
        ILogger<PharmaceuticalAnalysisController> logger)
    {
        _analysisService = analysisService;
        _formulaService = formulaService;
        _logger = logger;
    }

    /// <summary>
    /// GET api/pharmaceutical-analysis/pending
    /// Listar fórmulas aguardando análise
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<List<FormulaAnalysisDto>>>> GetPendingFormulas()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var formulas = await _analysisService.GetPendingFormulasAsync(establishmentId);

            var result = formulas.Select(f => new FormulaAnalysisDto
            {
                Id = f.Id,
                Code = f.Code,
                CustomerName = f.CustomerName ?? "Cliente Não Identificado",
                CustomerPhone = f.CustomerPhone ?? "",
                ProductTypeName = f.ProductType?.Name ?? "",
                ProductSubTypeName = f.ProductSubType?.Name ?? "",
                Quantity = f.Quantity,
                Unit = f.Unit,
                PaidAmount = f.PaidAmount ?? 0,
                Status = f.Status,
                CreatedAt = f.CreatedAt,
                PaidAt = f.PaidAt ?? f.CreatedAt,
                HoursWaiting = f.PaidAt.HasValue
                    ? (DateTime.UtcNow - f.PaidAt.Value).TotalHours
                    : 0,
                Priority = CalculatePriority(f.PaidAt ?? f.CreatedAt),
                RequiresPrescription = f.RequiresPrescription,
                IsControlledSubstance = f.IsControlledSubstance
            }).ToList();

            return Ok(ApiResponse<List<FormulaAnalysisDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fórmulas pendentes");
            return StatusCode(500, ApiResponse<List<FormulaAnalysisDto>>.ErrorResponse(
                "Erro ao buscar fórmulas pendentes"));
        }
    }

    /// <summary>
    /// POST api/pharmaceutical-analysis/start/{id}
    /// Iniciar análise de uma fórmula
    /// </summary>
    [HttpPost("start/{id}")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> StartAnalysis(Guid id)
    {
        try
        {
            var pharmacistId = GetCurrentUserId();
            var success = await _analysisService.StartAnalysisAsync(id, pharmacistId);

            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Fórmula não encontrada ou já em análise"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Análise iniciada com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao iniciar análise");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Erro ao iniciar análise"));
        }
    }

    /// <summary>
    /// POST api/pharmaceutical-analysis/approve
    /// Aprovar fórmula
    /// </summary>
    [HttpPost("approve")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> ApproveFormula(
        [FromBody] PharmaceuticalAnalysisDto dto)
    {
        try
        {
            var pharmacistId = GetCurrentUserId();

            var success = await _analysisService.ApproveFormulaAsync(
                dto.CustomerFormulaId,
                pharmacistId,
                dto
            );

            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Fórmula não encontrada"));

            return Ok(ApiResponse<bool>.SuccessResponse(
                true,
                "Fórmula aprovada com sucesso! Cliente será notificado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar fórmula {FormulaId}", dto.CustomerFormulaId);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                $"Erro ao aprovar fórmula: {ex.Message}"));
        }
    }

    /// <summary>
    /// POST api/pharmaceutical-analysis/reject
    /// Reprovar fórmula
    /// </summary>
    [HttpPost("reject")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> RejectFormula(
        [FromBody] RejectFormulaDto dto)
    {
        try
        {
            var pharmacistId = GetCurrentUserId();

            var success = await _analysisService.RejectFormulaAsync(
                dto.CustomerFormulaId,
                pharmacistId,
                dto.RejectionReason
            );

            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Fórmula não encontrada"));

            return Ok(ApiResponse<bool>.SuccessResponse(
                true,
                "Fórmula reprovada. Cliente será reembolsado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao reprovar fórmula {FormulaId}", dto.CustomerFormulaId);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                $"Erro ao reprovar fórmula: {ex.Message}"));
        }
    }

    /// <summary>
    /// POST api/pharmaceutical-analysis/request-adjustment
    /// Solicitar ajuste na fórmula
    /// </summary>
    [HttpPost("request-adjustment")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<bool>>> RequestAdjustment(
        [FromBody] RequestAdjustmentDto dto)
    {
        try
        {
            var pharmacistId = GetCurrentUserId();

            var success = await _analysisService.RequestAdjustmentAsync(
                dto.CustomerFormulaId,
                pharmacistId,
                dto.AdjustmentRequest
            );

            if (!success)
                return NotFound(ApiResponse<bool>.ErrorResponse("Fórmula não encontrada"));

            return Ok(ApiResponse<bool>.SuccessResponse(
                true,
                "Ajuste solicitado. Cliente será notificado."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao solicitar ajuste");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Erro ao solicitar ajuste"));
        }
    }

    /// <summary>
    /// GET api/pharmaceutical-analysis/history/{formulaId}
    /// Buscar histórico de análises de uma fórmula
    /// </summary>
    [HttpGet("history/{formulaId}")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<List<AnalysisLogDto>>>> GetAnalysisHistory(
        Guid formulaId)
    {
        try
        {
            var logs = await _analysisService.GetAnalysisHistoryAsync(formulaId);

            var result = logs.Select(log => new AnalysisLogDto
            {
                Id = log.Id,
                ActionType = log.ActionType,
                PharmacistName = log.PharmacistName,
                PharmacistCrf = log.PharmacistCrf,
                Analysis = log.Analysis,
                SafetyCheck = log.SafetyCheck,
                DosageCheck = log.DosageCheck,
                InteractionCheck = log.InteractionCheck,
                StabilityCheck = log.StabilityCheck,
                CreatedAt = log.CreatedAt
            }).ToList();

            return Ok(ApiResponse<List<AnalysisLogDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar histórico");
            return StatusCode(500, ApiResponse<List<AnalysisLogDto>>.ErrorResponse(
                "Erro ao buscar histórico"));
        }
    }

    /// <summary>
    /// GET api/pharmaceutical-analysis/statistics
    /// Estatísticas de análise farmacêutica
    /// </summary>
    [HttpGet("statistics")]
    [Authorize(Roles = "FARMACEUTICO,ADMIN")]
    public async Task<ActionResult<ApiResponse<AnalysisStatisticsDto>>> GetStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var stats = await _analysisService.GetStatisticsAsync(
                establishmentId,
                startDate,
                endDate
            );

            var result = new AnalysisStatisticsDto
            {
                TotalFormulas = stats.TotalFormulas,
                ApprovedCount = stats.ApprovedCount,
                RejectedCount = stats.RejectedCount,
                PendingCount = stats.PendingCount,
                ApprovalRate = stats.ApprovalRate,
                RejectionRate = stats.RejectionRate,
                AverageAnalysisTimeHours = stats.AverageAnalysisTimeHours
            };

            return Ok(ApiResponse<AnalysisStatisticsDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar estatísticas");
            return StatusCode(500, ApiResponse<AnalysisStatisticsDto>.ErrorResponse(
                "Erro ao buscar estatísticas"));
        }
    }

    // ==================== MÉTODOS AUXILIARES ====================

    private Guid GetEstablishmentId()
    {
        // TODO: Implementar lógica para obter EstablishmentId do usuário autenticado
        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private Guid GetCurrentUserId()
    {
        // TODO: Implementar lógica para obter ID do usuário autenticado
        var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("userId");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return Guid.Parse("00000000-0000-0000-0000-000000000001");
    }

    private string CalculatePriority(DateTime paidAt)
    {
        var hoursWaiting = (DateTime.UtcNow - paidAt).TotalHours;

        if (hoursWaiting < 2)
            return "URGENTE";
        else if (hoursWaiting < 6)
            return "ALTA";
        else
            return "NORMAL";
    }
}

// DTOs específicos para este controller
public class FormulaAnalysisDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = default!;
    public string CustomerName { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;
    public string ProductTypeName { get; set; } = default!;
    public string ProductSubTypeName { get; set; } = default!;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = default!;
    public decimal PaidAmount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime PaidAt { get; set; }
    public double HoursWaiting { get; set; }
    public string Priority { get; set; } = default!;
    public bool RequiresPrescription { get; set; }
    public bool IsControlledSubstance { get; set; }
}

public class RejectFormulaDto
{
    public Guid CustomerFormulaId { get; set; }
    public string RejectionReason { get; set; } = default!;
}

public class RequestAdjustmentDto
{
    public Guid CustomerFormulaId { get; set; }
    public string AdjustmentRequest { get; set; } = default!;
}

public class AnalysisLogDto
{
    public Guid Id { get; set; }
    public string ActionType { get; set; } = default!;
    public string PharmacistName { get; set; } = default!;
    public string PharmacistCrf { get; set; } = default!;
    public string? Analysis { get; set; }
    public bool SafetyCheck { get; set; }
    public bool DosageCheck { get; set; }
    public bool InteractionCheck { get; set; }
    public bool StabilityCheck { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AnalysisStatisticsDto
{
    public int TotalFormulas { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PendingCount { get; set; }
    public decimal ApprovalRate { get; set; }
    public decimal RejectionRate { get; set; }
    public double AverageAnalysisTimeHours { get; set; }
}