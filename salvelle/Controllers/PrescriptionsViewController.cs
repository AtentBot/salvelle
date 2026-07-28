using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;

namespace Controllers
{
    /// <summary>
    /// MVC Controller para Views de Prescrições e Orçamentos
    /// Rota: /Prescriptions
    /// </summary>
    [Route("PrescriptionsView")]
    public class PrescriptionsViewController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PrescriptionsViewController> _logger;

        public PrescriptionsViewController(
            AppDbContext context,
            ILogger<PrescriptionsViewController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ====================================================================
        // LISTA DE PRESCRIÇÕES / ORÇAMENTOS
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions
        /// Lista todos os orçamentos/prescrições
        /// </summary>
        [HttpGet("")]
        public async Task<IActionResult> Index(string? status, int page = 1, int pageSize = 20)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();
               
                var query = _context.Prescriptions
                    .Where(p => p.EstablishmentId == establishmentId)
                    .AsQueryable();

                // Filtro por status
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(p => p.Status == status);
                }

                // Paginação
                var total = await query.CountAsync();
                var prescriptions = await query
                    .OrderByDescending(p => p.PrescriptionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.CustomerId,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault() ?? "N/A",
                        p.PrescriptionDate,
                        p.ExpirationDate,
                        p.DoctorName,
                        p.Status,
                        p.PrescriptionType
                    })
                    .ToListAsync();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
                ViewBag.Status = status;
                ViewBag.TotalRecords = total;

                return View("~/Views/PrescriptionsView/Index.cshtml", prescriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar prescrições");
                TempData["Error"] = "Erro ao carregar prescrições";
                return View("~/Views/PrescriptionsView/Index.cshtml", new List<object>());
            }
        }

        // ====================================================================
        // PROCESSAR RECEITA - NOVO ORÇAMENTO (OCR)
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Processar
        /// Página para processar receita via OCR e gerar orçamento
        /// </summary>
        [HttpGet("Processar")]
        public IActionResult Processar()
        {
            return View("~/Views/PrescriptionsView/Processar.cshtml");
        }

        // ====================================================================
        // DETALHES DA PRESCRIÇÃO
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Details/{id}
        /// Exibe detalhes de uma prescrição/orçamento
        /// </summary>
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.CustomerId,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault(),
                        CustomerCpf = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.Cpf)
                            .FirstOrDefault(),
                        CustomerPhone = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.Phone ?? c.Phone)
                            .FirstOrDefault(),
                        p.PrescriptionDate,
                        p.ExpirationDate,
                        p.DoctorName,
                        p.DoctorCrm,
                        p.DoctorCrmState,
                        p.Status,
                        p.PrescriptionType,
                        p.ControlledType,
                        p.PrescriptionColor,
                        p.Medications,
                        p.Posology,
                        p.Observations,
                        p.ImageUrl,
                        p.ValidatedAt,
                        ValidatedByName = _context.Employees
                            .Where(e => e.Id == p.ValidatedByEmployeeId)
                            .Select(e => e.FullName)
                            .FirstOrDefault(),
                        p.ValidationNotes,
                        p.ManipulationOrderId,
                        p.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Buscar arquivos OCR relacionados
                var files = await _context.Set<PrescriptionFile>()
                    .Where(f => f.PrescriptionId == id)
                    .OrderByDescending(f => f.UploadedAt)
                    .Select(f => new
                    {
                        f.Id,
                        f.FileName,
                        f.FileType,
                        f.UploadedAt,
                        f.OcrStatus,
                        f.OcrConfidence,
                        f.OcrProcessedAt
                    })
                    .ToListAsync();

                ViewBag.Files = files;

                return View("~/Views/PrescriptionsView/Details.cshtml", prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar detalhes da prescrição {Id}", id);
                TempData["Error"] = "Erro ao carregar detalhes";
                return RedirectToAction(nameof(Index));
            }
        }

        // ====================================================================
        // EDITAR ORÇAMENTO
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Edit/{id}
        /// Editar valores do orçamento
        /// </summary>
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                LoadCustomersForDropdown();
                return View("~/Views/PrescriptionsView/Edit.cshtml", prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar edição {Id}", id);
                TempData["Error"] = "Erro ao carregar edição";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Prescriptions/Edit/{id}
        /// Salvar edição do orçamento
        /// </summary>
        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Prescription model)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Atualizar campos
                prescription.CustomerId = model.CustomerId;
                prescription.DoctorName = model.DoctorName;
                prescription.DoctorCrm = model.DoctorCrm;
                prescription.DoctorCrmState = model.DoctorCrmState;
                prescription.PrescriptionDate = model.PrescriptionDate;
                prescription.ExpirationDate = model.ExpirationDate;
                prescription.PrescriptionType = model.PrescriptionType;
                prescription.ControlledType = model.ControlledType;
                prescription.Medications = model.Medications;
                prescription.Posology = model.Posology;
                prescription.Observations = model.Observations;
                prescription.UpdatedAt = DateTime.UtcNow;
                prescription.UpdatedByEmployeeId = employeeId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescrição atualizada com sucesso!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar prescrição {Id}", id);
                TempData["Error"] = "Erro ao salvar alterações";
                LoadCustomersForDropdown();
                return View("~/Views/PrescriptionsView/Edit.cshtml", model);
            }
        }

        // ====================================================================
        // CONVERTER PARA VENDA
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/ConvertToSale/{id}
        /// Página para converter orçamento aprovado em venda
        /// </summary>
        [HttpGet("ConvertToSale/{id}")]
        public async Task<IActionResult> ConvertToSale(Guid id)
        {
            try
            {
                var quote = await _context.Set<Models.Pharmacy.PrescriptionQuote>()
                    .Where(q => q.Id == id)
                    .Select(q => new
                    {
                        q.Id,
                        q.Code,
                        q.FinalPrice,
                        q.Status,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == q.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync();

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                if (quote.Status != "APROVADO")
                {
                    TempData["Warning"] = "Apenas orçamentos aprovados podem ser convertidos em venda";
                    return RedirectToAction(nameof(Details), new { id });
                }

                ViewBag.Quote = quote;
                return View("~/Views/PrescriptionsView/ConvertToSale.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar conversão {Id}", id);
                TempData["Error"] = "Erro ao carregar página de conversão";
                return RedirectToAction(nameof(Index));
            }
        }

        // ====================================================================
        // PÁGINA DE UPLOAD OCR
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/OcrUpload/{id}
        /// Página dedicada para upload e processamento OCR
        /// </summary>
        [HttpGet("OcrUpload/{id}")]
        public async Task<IActionResult> OcrUpload(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions
                    .Where(p => p.Id == id)
                    .Select(p => new
                    {
                        p.Id,
                        p.Code,
                        p.Status,
                        CustomerName = _context.Customers
                            .Where(c => c.Id == p.CustomerId)
                            .Select(c => c.FullName)
                            .FirstOrDefault(),
                        p.PrescriptionDate
                    })
                    .FirstOrDefaultAsync();

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.Prescription = prescription;
                return View("~/Views/PrescriptionsView/OcrUpload.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar OCR upload {Id}", id);
                TempData["Error"] = "Erro ao carregar página de upload";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /Prescriptions/OcrUpload
        /// Criar nova prescrição e redirecionar para OCR
        /// </summary>
        [HttpGet("OcrUpload")]
        public async Task<IActionResult> OcrUploadNew()
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                // Criar uma nova prescrição temporária
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    EstablishmentId = establishmentId,
                    Code = await GeneratePrescriptionCode(),
                    CustomerId = Guid.Empty,
                    PrescriptionDate = DateTime.UtcNow,
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    DoctorName = "A ser identificado via OCR",
                    Status = "RASCUNHO",
                    PrescriptionType = "SIMPLES",
                    CreatedByEmployeeId = employeeId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(OcrUpload), new { id = prescription.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar prescrição para OCR");
                TempData["Error"] = "Erro ao iniciar processamento";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// GET: /Prescriptions/Upload
        /// Upload direto sem prescrição prévia
        /// </summary>
        [HttpGet("Upload")]
        public IActionResult Upload()
        {
            return View("~/Views/PrescriptionsView/OcrUploadDirect.cshtml");
        }

        // ====================================================================
        // PÁGINA PÚBLICA DE ORÇAMENTO (PARA CLIENTE)
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Quote/{token}
        /// Página pública para cliente visualizar e aprovar/recusar orçamento
        /// </summary>
        [HttpGet("Quote/{token}")]
        [AllowAnonymous]
        public async Task<IActionResult> PublicQuote(string token)
        {
            try
            {
                var quote = await _context.Set<Models.Pharmacy.PrescriptionQuote>()
                    .Where(q => q.PublicToken == token)
                    .Select(q => new
                    {
                        q.Id,
                        q.Code,
                        q.Status,
                        q.FinalPrice,
                        q.MaterialsCost,
                        q.MarkupPercentage,
                        q.MarkupValue,
                        q.LaborCost,
                        q.PackagingCost,
                        q.DiscountPercentage,
                        q.DiscountValue,
                        q.ValidUntil,
                        q.EstimatedDays,
                        q.PharmaceuticalForm,
                        q.TotalQuantity,
                        q.TotalQuantityUnit,
                        q.TotalQuantityNumeric,
                        q.UsageType,
                        q.Instructions,
                        q.DoctorName,
                        q.DoctorCrm,
                        q.DoctorCrmState,
                        q.CustomerName,
                        q.CustomerPhone,
                        q.CustomerEmail,
                        q.ComponentsJson,
                        EstablishmentName = _context.Establishments
                            .Where(e => e.Id == q.EstablishmentId)
                            .Select(e => e.NomeFantasia)
                            .FirstOrDefault(),
                        EstablishmentPhone = _context.Establishments
                            .Where(e => e.Id == q.EstablishmentId)
                            .Select(e => e.Phone)
                            .FirstOrDefault(),
                        q.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (quote == null)
                {
                    return View("~/Views/PrescriptionsView/QuoteNotFound.cshtml");
                }

                // Verificar se expirou
                if (quote.ValidUntil < DateTime.UtcNow && quote.Status == "PENDENTE")
                {
                    ViewBag.Expired = true;
                }

                // Incrementar contador de visualizações
                var quoteEntity = await _context.Set<Models.Pharmacy.PrescriptionQuote>()
                    .FirstOrDefaultAsync(q => q.PublicToken == token);
                
                if (quoteEntity != null)
                {
                    quoteEntity.ViewCount = quoteEntity.ViewCount + 1;
                    quoteEntity.LastViewedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                ViewBag.Token = token;
                return View("~/Views/PrescriptionsView/PublicQuote.cshtml", quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar orçamento público {Token}", token);
                return View("~/Views/PrescriptionsView/QuoteError.cshtml");
            }
        }

        // ====================================================================
        // CRIAR PRESCRIÇÃO MANUAL
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Create
        /// Formulário para criar prescrição manualmente
        /// </summary>
        [HttpGet("Create")]
        public IActionResult Create()
        {
            LoadCustomersForDropdown();
            return View("~/Views/PrescriptionsView/Create.cshtml");
        }

        /// <summary>
        /// POST: /Prescriptions/Create
        /// Salvar nova prescrição
        /// </summary>
        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Prescription model)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var establishmentId = GetEstablishmentId();

                model.Id = Guid.NewGuid();
                model.EstablishmentId = establishmentId;
                model.CreatedByEmployeeId = employeeId;
                model.Status = "PENDENTE";
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;

                // Gerar código automático
                model.Code = await GeneratePrescriptionCode();

                _context.Prescriptions.Add(model);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescrição criada com sucesso!";
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar prescrição");
                TempData["Error"] = "Erro ao criar prescrição";
                LoadCustomersForDropdown();
                return View("~/Views/PrescriptionsView/Create.cshtml", model);
            }
        }

        // ====================================================================
        // VALIDAR PRESCRIÇÃO (FARMACÊUTICO)
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Validate/{id}
        /// Formulário para validação farmacêutica
        /// </summary>
        [HttpGet("Validate/{id}")]
        public async Task<IActionResult> Validate(Guid id)
        {
            try
            {
                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                if (prescription.Status != "PENDENTE")
                {
                    TempData["Warning"] = "Esta prescrição já foi validada";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return View("~/Views/PrescriptionsView/Validate.cshtml", prescription);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar validação {Id}", id);
                TempData["Error"] = "Erro ao carregar validação";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// POST: /Prescriptions/Validate/{id}
        /// Processa validação da prescrição
        /// </summary>
        [HttpPost("Validate/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Validate(Guid id, string validationNotes)
        {
            try
            {
                var employeeId = GetEmployeeId();

                if (!HasPermission("FARMACEUTICO") && !HasPermission("FARMACEUTICO_RT"))
                {
                    TempData["Error"] = "Apenas farmacêuticos podem validar prescrições";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                prescription.Status = "VALIDADA";
                prescription.ValidatedAt = DateTime.UtcNow;
                prescription.ValidatedByEmployeeId = employeeId;
                prescription.ValidationNotes = validationNotes;
                prescription.UpdatedByEmployeeId = employeeId;
                prescription.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescrição validada com sucesso!";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao validar prescrição {Id}", id);
                TempData["Error"] = "Erro ao validar prescrição";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // CANCELAR PRESCRIÇÃO
        // ====================================================================

        /// <summary>
        /// POST: /Prescriptions/Cancel/{id}
        /// Cancelar prescrição/orçamento
        /// </summary>
        [HttpPost("Cancel/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(Guid id, string reason)
        {
            try
            {
                var employeeId = GetEmployeeId();
                var prescription = await _context.Prescriptions.FindAsync(id);

                if (prescription == null)
                {
                    TempData["Error"] = "Prescrição não encontrada";
                    return RedirectToAction(nameof(Index));
                }

                prescription.Status = "CANCELADA";
                prescription.CancelledAt = DateTime.UtcNow;
                prescription.CancelledByEmployeeId = employeeId;
                prescription.CancellationReason = reason;
                prescription.UpdatedAt = DateTime.UtcNow;
                prescription.UpdatedByEmployeeId = employeeId;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Prescrição cancelada";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao cancelar prescrição {Id}", id);
                TempData["Error"] = "Erro ao cancelar";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // ====================================================================
        // IMPRIMIR
        // ====================================================================

        /// <summary>
        /// GET: /Prescriptions/Print/{id}
        /// Versão para impressão do orçamento
        /// </summary>
        [HttpGet("Print/{id}")]
        public async Task<IActionResult> Print(Guid id)
        {
            try
            {
                var quote = await _context.Set<Models.Pharmacy.PrescriptionQuote>()
                    .Where(q => q.Id == id)
                    .FirstOrDefaultAsync();

                if (quote == null)
                {
                    TempData["Error"] = "Orçamento não encontrado";
                    return RedirectToAction(nameof(Index));
                }

                // Carregar dados relacionados
                var customer = await _context.Customers.FindAsync(quote.CustomerId);
                var establishment = await _context.Establishments.FindAsync(quote.EstablishmentId);

                ViewBag.Customer = customer;
                ViewBag.Establishment = establishment;

                return View("~/Views/PrescriptionsView/Print.cshtml", quote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar impressão {Id}", id);
                TempData["Error"] = "Erro ao carregar impressão";
                return RedirectToAction(nameof(Index));
            }
        }

        // ====================================================================
        // MÉTODOS AUXILIARES
        // ====================================================================

        private Guid GetEmployeeId()
        {
            // Primeiro tentar Claims (autenticação padrão)
            var claim = User.Claims.FirstOrDefault(c => c.Type == "EmployeeId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
            {
                return id;
            }

            // Fallback: buscar por cookie de sessão
            var sessionToken = Request.Cookies["SessionId"];
            if (!string.IsNullOrEmpty(sessionToken))
            {
                var session = _context.EmployeeSessions
                    .FirstOrDefault(s => s.Token == sessionToken &&
                                        s.ExpiresAt > DateTime.UtcNow &&
                                        s.IsActive);
                if (session != null)
                {
                    return session.EmployeeId;
                }
            }

            _logger.LogWarning("EmployeeId not found");
            return Guid.Empty;
        }

        private Guid GetEstablishmentId()
        {
            // Primeiro tentar Claims
            var claim = User.Claims.FirstOrDefault(c => c.Type == "EstablishmentId");
            if (claim != null && Guid.TryParse(claim.Value, out var id))
            {
                return id;
            }

            // Fallback: buscar via employee
            var employeeId = GetEmployeeId();
            if (employeeId != Guid.Empty)
            {
                var employee = _context.Employees.FirstOrDefault(e => e.Id == employeeId);
                if (employee != null)
                {
                    return employee.EstablishmentId;
                }
            }

            _logger.LogWarning("EstablishmentId not found");
            return Guid.Empty;
        }

        private bool HasPermission(string jobPositionCode)
        {
            // Primeiro tentar Claims
            var positionClaim = User.Claims.FirstOrDefault(c => c.Type == "JobPositionCode");
            if (positionClaim != null)
            {
                return positionClaim.Value.Equals(jobPositionCode, StringComparison.OrdinalIgnoreCase);
            }

            // Fallback: buscar via employee
            var employeeId = GetEmployeeId();
            if (employeeId != Guid.Empty)
            {
                var employee = _context.Employees
                    .Include(e => e.JobPosition)
                    .FirstOrDefault(e => e.Id == employeeId);
                
                return employee?.JobPosition?.Code?.Equals(jobPositionCode, StringComparison.OrdinalIgnoreCase) == true;
            }

            return false;
        }

        private void LoadCustomersForDropdown()
        {
            var establishmentId = GetEstablishmentId();
            ViewBag.Customers = _context.Customers
                .Where(c => c.EstablishmentId == establishmentId)
                .OrderBy(c => c.FullName)
                .Select(c => new { c.Id, c.FullName, c.Cpf, c.Phone })
                .ToList();
        }

        private async Task<string> GeneratePrescriptionCode()
        {
            var year = Helpers.BrazilTime.Now.Year;
            var lastCode = await _context.Prescriptions
                .Where(p => p.Code.StartsWith($"RX{year}"))
                .OrderByDescending(p => p.Code)
                .Select(p => p.Code)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastCode != null && lastCode.Length >= 10 && int.TryParse(lastCode.Substring(6), out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }

            return $"RX{year}{nextNumber:D6}";
        }
    }
}
