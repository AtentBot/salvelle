using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Pharmacy;

namespace Controllers
{
    /// <summary>
    /// MVC Controller para Views de Orçamentos
    /// Rota: /Orcamentos
    /// </summary>
    [Route("Orcamentos")]
    public class OrcamentosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<OrcamentosController> _logger;

        public OrcamentosController(
            AppDbContext context,
            ILogger<OrcamentosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ====================================================================
        // LISTA DE ORÇAMENTOS
        // ====================================================================

        /// <summary>
        /// GET: /Orcamentos
        /// Lista todos os orçamentos
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 20)
        {
            try
            {
                var establishmentId = GetEstablishmentId();

                var query = _context.PrescriptionQuotes
                    .Where(q => q.EstablishmentId == establishmentId)
                    .AsQueryable();

                // Filtro por status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(q => q.Status == status.ToUpper());
                }

                // Paginação
                var total = await query.CountAsync();
                var quotes = await query
                    .OrderByDescending(q => q.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(q => new
                    {
                        q.Id,
                        q.Code,
                        q.CustomerName,
                        q.CustomerPhone,
                        q.DoctorName,
                        q.PharmaceuticalForm,
                        q.TotalQuantity,
                        q.FinalPrice,
                        q.ValidUntil,
                        q.Status,
                        q.CreatedAt,
                        q.WhatsAppSent,
                        q.WhatsAppSentAt,
                        q.ApprovedAt,
                        q.RejectedAt,
                        IsExpired = q.ValidUntil < DateTime.UtcNow && q.Status == "PENDENTE",
                        DaysUntilExpiration = (q.ValidUntil - DateTime.UtcNow).Days,
                        q.PublicToken
                    })
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
                ViewBag.Status = status;
                ViewBag.TotalRecords = total;

                // Estatísticas
                ViewBag.TotalPendentes = await _context.PrescriptionQuotes
                    .CountAsync(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE");
                ViewBag.TotalAprovados = await _context.PrescriptionQuotes
                    .CountAsync(q => q.EstablishmentId == establishmentId && q.Status == "APROVADO");
                ViewBag.TotalRejeitados = await _context.PrescriptionQuotes
                    .CountAsync(q => q.EstablishmentId == establishmentId && q.Status == "REJEITADO");
                ViewBag.ValorPendente = await _context.PrescriptionQuotes
                    .Where(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE")
                    .SumAsync(q => (decimal?)q.FinalPrice) ?? 0;

                return View(quotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar orçamentos");
                TempData["Error"] = "Erro ao carregar orçamentos";
                return View(new List<object>());
            }
        }

        // ====================================================================
        // DETALHES DO ORÇAMENTO
        // ====================================================================

        /// <summary>
        /// GET: /Orcamentos/Details/{id}
        /// Exibe detalhes de um orçamento
        /// </summary>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var establishmentId = GetEstablishmentId();

                var quote = await _context.PrescriptionQuotes
                    .Where(q => q.Id == id && q.EstablishmentId == establishmentId)
                    .FirstOrDefaultAsync();

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Buscar funcionário que criou
                var createdBy = await _context.Employees
                    .Where(e => e.Id == quote.CreatedByEmployeeId)
                    .Select(e => e.FullName)
                    .FirstOrDefaultAsync();

                ViewBag.CreatedByName = createdBy ?? "N/A";

                return View(quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar detalhes do orçamento {Id}", id);
                TempData["Error"] = "Erro ao carregar detalhes";
                return RedirectToAction(nameof(Index));
            }
        }

        // ====================================================================
        // REENVIAR WHATSAPP
        // ====================================================================

        /// <summary>
        /// POST: /Orcamentos/ResendWhatsApp/{id}
        /// Reenvia orçamento via WhatsApp
        /// </summary>
        [HttpPost("ResendWhatsApp/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendWhatsApp(Guid id)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                var quote = await _context.PrescriptionQuotes
                    .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (string.IsNullOrEmpty(quote.CustomerPhone))
                {
                    TempData["Error"] = "Cliente não possui telefone cadastrado";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // Chamar serviço de WhatsApp
                var whatsAppService = HttpContext.RequestServices
                    .GetService<Service.Prescriptions.QuoteWhatsAppService>();

                if (whatsAppService != null)
                {
                    var result = await whatsAppService.SendQuoteAsync(quote.Id, establishmentId);
                    if (result.Success)
                    {
                        TempData["Success"] = "Orçamento reenviado com sucesso!";
                    }
                    else
                    {
                        TempData["Error"] = result.Message ?? "Erro ao reenviar";
                    }
                }
                else
                {
                    TempData["Warning"] = "Serviço de WhatsApp não configurado";
                }

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao reenviar WhatsApp para orçamento {Id}", id);
                TempData["Error"] = "Erro ao reenviar";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // APROVAR MANUALMENTE
        // ====================================================================

        /// <summary>
        /// POST: /Orcamentos/Approve/{id}
        /// Aprova orçamento manualmente (funcionário)
        /// </summary>
        [HttpPost("Approve/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id, string? observations)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                var quote = await _context.PrescriptionQuotes
                    .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (quote.Status != "PENDENTE")
                {
                    TempData["Warning"] = "Este orçamento não está pendente";
                    return RedirectToAction(nameof(Details), new { id });
                }

                quote.Status = "APROVADO";
                quote.ApprovedAt = DateTime.UtcNow;
                quote.CustomerObservations = observations;
                quote.UpdatedAt = DateTime.UtcNow;
                quote.UpdatedByEmployeeId = employeeId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Orçamento aprovado com sucesso!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao aprovar orçamento {Id}", id);
                TempData["Error"] = "Erro ao aprovar";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // REJEITAR
        // ====================================================================

        /// <summary>
        /// POST: /Orcamentos/Reject/{id}
        /// Rejeita orçamento
        /// </summary>
        [HttpPost("Reject/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string reason)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                var quote = await _context.PrescriptionQuotes
                    .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                quote.Status = "REJEITADO";
                quote.RejectedAt = DateTime.UtcNow;
                quote.RejectionReason = reason;
                quote.UpdatedAt = DateTime.UtcNow;
                quote.UpdatedByEmployeeId = employeeId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Orçamento rejeitado";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao rejeitar orçamento {Id}", id);
                TempData["Error"] = "Erro ao rejeitar";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // CONVERTER EM VENDA
        // ====================================================================

        /// <summary>
        /// POST: /Orcamentos/ConvertToSale/{id}
        /// Converte orçamento aprovado em venda/manipulação
        /// </summary>
        [HttpPost("ConvertToSale/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConvertToSale(Guid id)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                var quote = await _context.PrescriptionQuotes
                    .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (quote.Status != "APROVADO")
                {
                    TempData["Warning"] = "Apenas orçamentos aprovados podem ser convertidos";
                    return RedirectToAction(nameof(Details), new { id });
                }

                // TODO: Implementar conversão para venda/manipulação
                // Por enquanto, apenas marca como convertido
                quote.Status = "CONVERTIDO";
                quote.UpdatedAt = DateTime.UtcNow;
                quote.UpdatedByEmployeeId = employeeId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Orçamento convertido! Implemente a criação da manipulação.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao converter orçamento {Id}", id);
                TempData["Error"] = "Erro ao converter";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // COPIAR LINK PÚBLICO
        // ====================================================================

        /// <summary>
        /// GET: /Orcamentos/GetPublicLink/{id}
        /// Retorna o link público do orçamento
        /// </summary>
        [HttpGet("GetPublicLink/{id}")]
        public async Task<IActionResult> GetPublicLink(Guid id)
        {
            try
            {
                var establishmentId = GetEstablishmentId();

                var quote = await _context.PrescriptionQuotes
                    .Where(q => q.Id == id && q.EstablishmentId == establishmentId)
                    .Select(q => new { q.PublicToken })
                    .FirstOrDefaultAsync();

                if (quote == null)
                    return NotFound(new { message = "Orçamento não encontrado" });

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var publicUrl = $"{baseUrl}/Orcamento/{quote.PublicToken}";

                return Ok(new { url = publicUrl, token = quote.PublicToken });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter link público {Id}", id);
                return StatusCode(500, new { message = "Erro ao obter link" });
            }
        }

        // ====================================================================
        // MÉTODOS AUXILIARES
        // ====================================================================

        private Guid GetEmployeeId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
            if (claim == null || !Guid.TryParse(claim.Value, out var id))
            {
                return Guid.Empty;
            }
            return id;
        }

        private Guid GetEstablishmentId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == "EstablishmentId");
            if (claim == null || !Guid.TryParse(claim.Value, out var id))
            {
                return Guid.Empty;
            }
            return id;
        }
    }
}
