using DTOs.ManipulationSteps;
using Microsoft.AspNetCore.Mvc;
using Service;
using DTOs;
using System.Security.Claims;

namespace Controllers;

[ApiController]
[Route("api/manipulationorders/{manipulationOrderId:guid}/steps")]
public class ManipulationStepsController : ControllerBase
{
    private readonly WeighingStepService _weighingService;
    private readonly WeighingSheetService _weighingSheetService;
    private readonly ILogger<ManipulationStepsController> _logger;

    public ManipulationStepsController(
        WeighingStepService weighingService,
        WeighingSheetService weighingSheetService,
        ILogger<ManipulationStepsController> logger)
    {
        _weighingService = weighingService;
        _weighingSheetService = weighingSheetService;
        _logger = logger;
    }

    /// <summary>
    /// Processa etapa de pesagem dos componentes
    /// </summary>
    /// <param name="manipulationOrderId">ID da ordem de manipulação</param>
    /// <param name="request">Dados da pesagem</param>
    /// <returns>Resultado da pesagem com desvios e alertas</returns>
    [HttpPost("pesagem")]
    public async Task<ActionResult<ApiResponse<WeighingStepResponseDto>>> ProcessWeighing(
        Guid manipulationOrderId,
        [FromBody] WeighingStepRequestDto request)
    {
        try
        {
            var employeeId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(employeeId))
            {
                return Unauthorized(ApiResponse<WeighingStepResponseDto>.ErrorResponse(
                    "Funcionário não autenticado"));
            }

            // Se não foi informado employeeId no request, usar o do token
            if (request.EmployeeId == Guid.Empty)
            {
                if (Guid.TryParse(employeeId, out var parsedId))
                {
                    request.EmployeeId = parsedId;
                }
            }

            var (success, error, result) = await _weighingService.ProcessWeighingStepAsync(
                manipulationOrderId, request);

            if (!success)
            {
                _logger.LogWarning(
                    "Falha ao processar pesagem - Ordem: {OrderId}, Erro: {Error}",
                    manipulationOrderId, error);

                return BadRequest(ApiResponse<WeighingStepResponseDto>.ErrorResponse(error!));
            }

            _logger.LogInformation(
                "Pesagem processada com sucesso - Ordem: {OrderId}, Step: {StepId}",
                manipulationOrderId, result!.StepId);

            return Ok(ApiResponse<WeighingStepResponseDto>.SuccessResponse(
                result, "Pesagem realizada com sucesso"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro não tratado ao processar pesagem - Ordem: {OrderId}",
                manipulationOrderId);

            return StatusCode(500, ApiResponse<WeighingStepResponseDto>.ErrorResponse(
                "Erro interno ao processar pesagem"));
        }
    }

    /// <summary>
    /// Gera ficha de pesagem em HTML para impressão
    /// </summary>
    /// <param name="manipulationOrderId">ID da ordem de manipulação</param>
    /// <returns>HTML da ficha de pesagem</returns>
    [HttpGet("weighing-sheet")]
    [Produces("text/html")]
    public async Task<IActionResult> GenerateWeighingSheet(Guid manipulationOrderId)
    {
        try
        {
            var html = await _weighingSheetService.GenerateWeighingSheetHtml(manipulationOrderId);

            _logger.LogInformation(
                "Ficha de pesagem gerada - Ordem: {OrderId}",
                manipulationOrderId);

            return Content(html, "text/html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Erro ao gerar ficha de pesagem - Ordem: {OrderId}",
                manipulationOrderId);

            return StatusCode(500, new { message = "Erro ao gerar ficha de pesagem", error = ex.Message });
        }
    }
}