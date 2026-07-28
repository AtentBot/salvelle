using Microsoft.AspNetCore.Mvc;
using Service.Documents;

namespace Controllers.Api;

/// <summary>
/// API para geração de documentos PDF de manipulação
/// - Ficha de Manipulação (uso interno)
/// - Certificado de Manipulação (para cliente)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ManipulationDocumentsController : ControllerBase
{
    private readonly ManipulationDocumentService _documentService;
    private readonly ILogger<ManipulationDocumentsController> _logger;

    public ManipulationDocumentsController(
        ManipulationDocumentService documentService,
        ILogger<ManipulationDocumentsController> logger)
    {
        _documentService = documentService;
        _logger = logger;
    }

    // ════════════════════════════════════════════════════════════════════════
    // FICHA DE MANIPULAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera a ficha de manipulação em PDF para uso no laboratório
    /// GET /api/ManipulationDocuments/{orderId}/sheet
    /// </summary>
    [HttpGet("{orderId}/sheet")]
    public async Task<IActionResult> GetManipulationSheet(Guid orderId)
    {
        try
        {
            var pdfBytes = await _documentService.GenerateManipulationSheetAsync(orderId);
            
            _logger.LogInformation("Ficha de manipulação gerada para ordem {OrderId}", orderId);
            
            return File(pdfBytes, "application/pdf", $"ficha_manipulacao_{orderId:N}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ordem {OrderId} não encontrada para gerar ficha", orderId);
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar ficha de manipulação para ordem {OrderId}", orderId);
            return StatusCode(500, new { success = false, error = "Erro ao gerar documento" });
        }
    }

    /// <summary>
    /// Visualiza a ficha de manipulação inline no navegador
    /// GET /api/ManipulationDocuments/{orderId}/sheet/view
    /// </summary>
    [HttpGet("{orderId}/sheet/view")]
    public async Task<IActionResult> ViewManipulationSheet(Guid orderId)
    {
        try
        {
            var pdfBytes = await _documentService.GenerateManipulationSheetAsync(orderId);
            
            return File(pdfBytes, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao visualizar ficha de manipulação para ordem {OrderId}", orderId);
            return StatusCode(500, new { success = false, error = "Erro ao gerar documento" });
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // CERTIFICADO DE MANIPULAÇÃO
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera o certificado de manipulação em PDF para entregar ao cliente
    /// GET /api/ManipulationDocuments/{orderId}/certificate
    /// </summary>
    [HttpGet("{orderId}/certificate")]
    public async Task<IActionResult> GetCertificate(Guid orderId)
    {
        try
        {
            var pdfBytes = await _documentService.GenerateCertificateAsync(orderId);
            
            _logger.LogInformation("Certificado de manipulação gerado para ordem {OrderId}", orderId);
            
            return File(pdfBytes, "application/pdf", $"certificado_manipulacao_{orderId:N}.pdf");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ordem {OrderId} não encontrada para gerar certificado", orderId);
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar certificado para ordem {OrderId}", orderId);
            return StatusCode(500, new { success = false, error = "Erro ao gerar documento" });
        }
    }

    /// <summary>
    /// Visualiza o certificado inline no navegador
    /// GET /api/ManipulationDocuments/{orderId}/certificate/view
    /// </summary>
    [HttpGet("{orderId}/certificate/view")]
    public async Task<IActionResult> ViewCertificate(Guid orderId)
    {
        try
        {
            var pdfBytes = await _documentService.GenerateCertificateAsync(orderId);
            
            return File(pdfBytes, "application/pdf");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao visualizar certificado para ordem {OrderId}", orderId);
            return StatusCode(500, new { success = false, error = "Erro ao gerar documento" });
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // PACOTE COMPLETO (Ficha + Certificado)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera ambos os documentos em um único ZIP
    /// GET /api/ManipulationDocuments/{orderId}/package
    /// </summary>
    [HttpGet("{orderId}/package")]
    public async Task<IActionResult> GetDocumentPackage(Guid orderId)
    {
        try
        {
            var sheetBytes = await _documentService.GenerateManipulationSheetAsync(orderId);
            var certificateBytes = await _documentService.GenerateCertificateAsync(orderId);

            using var memoryStream = new MemoryStream();
            using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
            {
                var sheetEntry = archive.CreateEntry($"ficha_manipulacao_{orderId:N}.pdf");
                using (var sheetStream = sheetEntry.Open())
                {
                    await sheetStream.WriteAsync(sheetBytes);
                }

                var certEntry = archive.CreateEntry($"certificado_manipulacao_{orderId:N}.pdf");
                using (var certStream = certEntry.Open())
                {
                    await certStream.WriteAsync(certificateBytes);
                }
            }

            memoryStream.Position = 0;
            var zipBytes = memoryStream.ToArray();

            _logger.LogInformation("Pacote de documentos gerado para ordem {OrderId}", orderId);

            return File(zipBytes, "application/zip", $"documentos_manipulacao_{orderId:N}.zip");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar pacote de documentos para ordem {OrderId}", orderId);
            return StatusCode(500, new { success = false, error = "Erro ao gerar documentos" });
        }
    }
}
