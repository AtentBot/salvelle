using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Pharmacy;
using Service.Prescriptions;
using Service;
using DTOs;

namespace Controllers.Api;

/// <summary>
/// API para envio e processamento de receitas pelo cliente
/// Conecta o portal do cliente ao sistema de OCR existente
/// </summary>
[ApiController]
[Route("api/cliente/prescriptions")]
public class ClientePrescriptionApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly OpenAIPrescriptionParserService _ocrService;
    private readonly IngredientMatcherService _matcherService;
    private readonly ILogger<ClientePrescriptionApiController> _logger;

    public ClientePrescriptionApiController(
        AppDbContext context,
        OpenAIPrescriptionParserService ocrService,
        IngredientMatcherService matcherService,
        ILogger<ClientePrescriptionApiController> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _matcherService = matcherService;
        _logger = logger;
    }

    /// <summary>
    /// Upload e processamento OCR de receita
    /// </summary>
    [HttpPost("upload")]
    [EnableRateLimiting("ocr-upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadPrescription([FromBody] CustomerPrescriptionUploadDto dto)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        if (session?.CurrentEstablishmentId == null)
            return BadRequest(new { success = false, message = "Selecione uma farmácia primeiro" });

        // Validar magic bytes do arquivo (evita upload de executáveis disfarçados)
        if (!IsAllowedPrescriptionFile(dto.FileBase64))
            return BadRequest(new { success = false, message = "Tipo de arquivo inválido. Envie JPEG, PNG ou PDF." });

        try
        {
            _logger.LogInformation("Cliente {CustomerId} enviando receita para OCR", customer.Id);

            // 1. Criar registro de prescrição
            var prescription = new Prescription
            {
                Id = Guid.NewGuid(),
                EstablishmentId = session.CurrentEstablishmentId.Value,
                CustomerId = customer.Id,
                Code = $"RX-{DateTime.UtcNow:yyyyMMddHHmmss}-{System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 9999)}",
                Status = "PENDENTE",
                PrescriptionDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(30),
                DoctorName = "",
                DoctorCrm = "",
                DoctorCrmState = "",
                PrescriptionType = "COMUM",
                Medications = "",
                Posology = "",
                Observations = dto.Observations ?? "Receita enviada via Portal do Cliente",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = Guid.Empty
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            // 2. Salvar arquivo
            var prescriptionFile = new PrescriptionFile
            {
                Id = Guid.NewGuid(),
                PrescriptionId = prescription.Id,
                FileName = dto.FileName ?? $"receita_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg",
                FileType = dto.FileType ?? "image/jpeg",
                FileBase64 = dto.FileBase64,
                FileSizeBytes = dto.FileBase64.Length,
                UploadedAt = DateTime.UtcNow,
                OcrStatus = "PROCESSING"
            };

            _context.PrescriptionFiles.Add(prescriptionFile);
            await _context.SaveChangesAsync();

            // 3. Processar OCR com OpenAI Vision
            var ocrResult = await _ocrService.ParsePrescriptionAsync(dto.FileBase64, dto.FileType ?? "image/jpeg");

            // 4. Atualizar arquivo com resultado
            prescriptionFile.OcrStatus = "COMPLETED";
            prescriptionFile.OcrProcessedAt = DateTime.UtcNow;
            prescriptionFile.OcrConfidence = (decimal)ocrResult.OverallConfidence; // já é decimal no DTO
            prescriptionFile.OcrResult = System.Text.Json.JsonSerializer.Serialize(ocrResult);
            prescriptionFile.UpdatedAt = DateTime.UtcNow;

            // 5. Atualizar prescrição com dados do OCR
            if (ocrResult.Doctor != null && !string.IsNullOrEmpty(ocrResult.Doctor.Name))
            {
                prescription.DoctorName = ocrResult.Doctor.Name;
                prescription.DoctorCrm = ocrResult.Doctor.Crm ?? "";
                prescription.DoctorCrmState = ""; // DoctorInfoDto não tem CrmState
            }
            
            if (!string.IsNullOrEmpty(ocrResult.PrescriptionDate))
            {
                if (DateTime.TryParse(ocrResult.PrescriptionDate, out var parsedDate))
                {
                    prescription.PrescriptionDate = parsedDate;
                }
            }
            
            if (!string.IsNullOrEmpty(ocrResult.Instructions))
            {
                prescription.Posology = ocrResult.Instructions;
            }
            
            if (ocrResult.Items.Any())
            {
                prescription.Medications = string.Join("\n", 
                    ocrResult.Items.Select(i => $"{i.Name} - {i.Quantity} {i.Unit}"));
            }
            prescription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 6. Fazer matching dos ingredientes com estoque
            // Converter items do OCR para o DTO esperado pelo IngredientMatcherService
            var ocrItemsDto = ocrResult.Items.Select(i => new OcrItemDto
            {
                Name = i.Name,
                Quantity = i.Quantity,
                Unit = i.Unit,
                RawText = i.RawText,
                Confidence = (decimal)i.Confidence,
                LineNumber = 0
            }).ToList();

            var matchResponse = await _matcherService.FindMatchesAsync(ocrItemsDto);

            // Mapear resultados - usar Matches, não Results
            var matchResults = matchResponse.Matches?.Select(r => new IngredientMatchResultLocal
            {
                OcrText = r.OcrText,
                Name = r.RawText,
                Quantity = r.Quantity ?? "",
                Unit = r.Unit ?? "",
                IsQsp = false,
                Suggestions = r.Suggestions?.Select(s => new IngredientSuggestionLocal
                {
                    RawMaterialId = s.RawMaterialId,
                    Name = s.Name,
                    Confidence = (double)s.Confidence,
                    InStock = s.InStock,
                    AvailableQuantity = s.AvailableQuantity,
                    Unit = s.Unit
                }).ToList() ?? new()
            }).ToList() ?? new();

            _logger.LogInformation("OCR concluído para prescrição {Code}. {ItemCount} itens encontrados", 
                prescription.Code, ocrResult.Items.Count);

            return Ok(new
            {
                success = true,
                prescriptionId = prescription.Id,
                prescriptionCode = prescription.Code,
                ocrResult = new
                {
                    doctor = ocrResult.Doctor?.Name,
                    doctorCrm = ocrResult.Doctor?.Crm,
                    patient = ocrResult.Patient?.Name,
                    prescriptionDate = ocrResult.PrescriptionDate,
                    instructions = ocrResult.Instructions,
                    totalQuantity = ocrResult.TotalQuantity,
                    confidence = ocrResult.OverallConfidence
                },
                ingredients = matchResults,
                message = "Receita processada com sucesso!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar OCR de receita");
            return StatusCode(500, new { success = false, message = "Erro ao processar receita. Tente novamente." });
        }
    }

    /// <summary>
    /// Confirmar ingredientes e gerar orçamento
    /// </summary>
    [HttpPost("{id}/confirm")]
    public async Task<IActionResult> ConfirmIngredients(Guid id, [FromBody] ConfirmIngredientsLocalDto dto)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var prescription = await _context.Prescriptions
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customer.Id);

        if (prescription == null)
            return NotFound(new { success = false, message = "Prescrição não encontrada" });

        try
        {
            // Calcular orçamento
            decimal totalMaterials = 0;
            var quoteItems = new List<object>();

            foreach (var item in dto.ConfirmedIngredients)
            {
                var rawMaterial = await _context.RawMaterials
                    .FirstOrDefaultAsync(r => r.Id == item.RawMaterialId);

                if (rawMaterial == null) continue;

                // RawMaterial não tem AverageCost
                // Usar LastPurchasePrice ?? BasePrice ?? LastKnownPrice ?? 0
                decimal unitCost = rawMaterial.LastPurchasePrice 
                    ?? rawMaterial.BasePrice 
                    ?? rawMaterial.LastKnownPrice 
                    ?? 0m;
                
                decimal itemCost = unitCost * item.Quantity;
                totalMaterials += itemCost;

                quoteItems.Add(new
                {
                    rawMaterialId = rawMaterial.Id,
                    name = rawMaterial.Name,
                    quantity = item.Quantity,
                    unit = item.Unit,
                    unitCost = unitCost,
                    totalCost = itemCost
                });
            }

            // Markup e taxas
            var markupPercentage = 150m;
            var laborCost = 15.00m;
            var packagingCost = 5.00m;
            
            var markupValue = totalMaterials * (markupPercentage / 100);
            var subtotal = totalMaterials + markupValue + laborCost + packagingCost;
            var finalPrice = subtotal;

            // Gerar token público
            var publicToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_").Replace("+", "-").Substring(0, 22);

            // Criar orçamento
            var quote = new PrescriptionQuote
            {
                Id = Guid.NewGuid(),
                EstablishmentId = session!.CurrentEstablishmentId!.Value,
                CustomerId = customer.Id,
                PrescriptionId = prescription.Id,
                Code = $"ORC-{DateTime.UtcNow:yyyyMMdd}-{System.Security.Cryptography.RandomNumberGenerator.GetInt32(1000, 9999)}",
                PublicToken = publicToken,
                
                CustomerName = customer.FullName ?? "",
                CustomerPhone = customer.Phone,
                CustomerEmail = customer.Email,
                
                Status = "PENDENTE",
                
                MaterialsCost = totalMaterials,
                MarkupPercentage = markupPercentage,
                MarkupValue = markupValue,
                LaborCost = laborCost,
                PackagingCost = packagingCost,
                Subtotal = subtotal,
                DiscountPercentage = 0,
                DiscountValue = 0,
                FinalPrice = finalPrice,
                
                ValidUntil = DateTime.UtcNow.AddDays(7),
                EstimatedDays = 3,
                
                PharmaceuticalForm = dto.PharmaceuticalForm,
                TotalQuantity = dto.TotalQuantity,
                Instructions = dto.Instructions,
                ComponentsJson = System.Text.Json.JsonSerializer.Serialize(quoteItems),
                
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = Guid.Empty
            };

            _context.PrescriptionQuotes.Add(quote);

            // Atualizar status da prescrição
            prescription.Status = "VALIDADA";
            prescription.ValidatedAt = DateTime.UtcNow;
            prescription.ValidationNotes = "Orçamento gerado via Portal do Cliente";
            prescription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                quoteId = quote.Id,
                quoteCode = quote.Code,
                quote = new
                {
                    items = quoteItems,
                    materialsCost = totalMaterials,
                    markupPercentage = markupPercentage,
                    markupValue = markupValue,
                    laborCost = laborCost,
                    packagingCost = packagingCost,
                    subtotal = subtotal,
                    total = finalPrice,
                    validUntil = quote.ValidUntil,
                    estimatedDays = quote.EstimatedDays,
                    pharmaceuticalForm = dto.PharmaceuticalForm,
                    totalQuantity = dto.TotalQuantity
                },
                message = "Orçamento gerado com sucesso!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao gerar orçamento");
            return StatusCode(500, new { success = false, message = "Erro ao gerar orçamento" });
        }
    }

    /// <summary>
    /// Adicionar orçamento ao carrinho
    /// </summary>
    [HttpPost("quotes/{quoteId}/add-to-cart")]
    public async Task<IActionResult> AddQuoteToCart(Guid quoteId)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var quote = await _context.PrescriptionQuotes
            .FirstOrDefaultAsync(q => q.Id == quoteId && q.CustomerId == customer.Id);

        if (quote == null)
            return NotFound(new { success = false, message = "Orçamento não encontrado" });

        try
        {
            // Buscar ou criar carrinho
            var cart = await _context.CustomerCarts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && 
                                          c.EstablishmentId == session.CurrentEstablishmentId);

            if (cart == null)
            {
                cart = new CustomerCart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    EstablishmentId = session.CurrentEstablishmentId.Value,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CustomerCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Buscar ProductType e ProductSubType
            var productType = await GetOrCreateProductTypeAsync(
                session.CurrentEstablishmentId.Value,
                quote.PharmaceuticalForm ?? "Creme"
            );
            var productSubType = await GetOrCreateProductSubTypeAsync(
                productType.Id,
                quote.PharmaceuticalForm ?? "Creme"
            );

            // Criar CustomerFormula (Models.Pharmacy)
            var formula = new CustomerFormula
            {
                Id = Guid.NewGuid(),
                Code = $"FORM-{DateTime.UtcNow:yyyyMMddHHmmss}-{System.Security.Cryptography.RandomNumberGenerator.GetInt32(100, 999)}",
                EstablishmentId = session.CurrentEstablishmentId.Value,
                CustomerId = customer.Id,
                
                CustomerName = customer.FullName,
                CustomerPhone = customer.Phone,
                CustomerEmail = customer.Email,
                
                ProductTypeId = productType.Id,
                ProductSubTypeId = productSubType.Id,
                
                Quantity = ParseQuantity(quote.TotalQuantity),
                Unit = ParseUnit(quote.TotalQuantity),
                
                CustomerNotes = quote.Instructions,
                
                Status = "AGUARDANDO_COMPRA",
                
                EstimatedPrice = quote.FinalPrice,
                FinalPrice = quote.FinalPrice,
                
                PrescriptionQuoteId = quote.Id,
                
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CustomerFormulas.Add(formula);

            // Adicionar ao carrinho
            var cartItem = new CustomerCartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                CustomerFormulaId = formula.Id,
                ProductId = null,
                Quantity = 1,
                UnitPrice = quote.FinalPrice,
                Notes = $"Receita {quote.Code}",
                CreatedAt = DateTime.UtcNow
            };

            _context.CustomerCartItems.Add(cartItem);

            // Atualizar status do orçamento
            quote.Status = "CONVERTIDO";
            quote.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                cartItemId = cartItem.Id,
                formulaId = formula.Id,
                formulaCode = formula.Code,
                message = "Fórmula adicionada ao carrinho!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar orçamento ao carrinho");
            return StatusCode(500, new { success = false, message = "Erro ao adicionar ao carrinho" });
        }
    }

    /// <summary>
    /// Listar orçamentos do cliente
    /// </summary>
    [HttpGet("quotes")]
    public async Task<IActionResult> GetMyQuotes([FromQuery] string? status = null)
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var query = _context.PrescriptionQuotes
            .Include(q => q.Establishment)
            .Where(q => q.CustomerId == customer.Id);

        if (!string.IsNullOrEmpty(status))
            query = query.Where(q => q.Status == status);

        var quotes = await query
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new
            {
                q.Id,
                q.Code,
                q.Status,
                TotalValue = q.FinalPrice,
                q.PharmaceuticalForm,
                q.TotalQuantity,
                q.ValidUntil,
                q.EstimatedDays,
                q.CreatedAt,
                EstablishmentName = q.Establishment!.NomeFantasia
            })
            .ToListAsync();

        return Ok(new { success = true, quotes });
    }

    /// <summary>
    /// Sugestões de recompra baseadas no histórico
    /// </summary>
    [HttpGet("reorder-suggestions")]
    public async Task<IActionResult> GetReorderSuggestions()
    {
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var recentOrders = await _context.OnlineOrders
            .Include(o => o.Items)
            .Include(o => o.Establishment)
            .Where(o => o.CustomerId == customer.Id && o.Status == "DELIVERED")
            .OrderByDescending(o => o.DeliveredAt)
            .Take(10)
            .ToListAsync();

        var suggestions = recentOrders
            .SelectMany(o => o.Items!.Select(i => new
            {
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                LastPurchase = o.DeliveredAt ?? o.CreatedAt,
                DaysSincePurchase = (DateTime.UtcNow - (o.DeliveredAt ?? o.CreatedAt)).Days,
                UnitPrice = i.UnitPrice,
                EstablishmentId = o.EstablishmentId,
                EstablishmentName = o.Establishment?.NomeFantasia
            }))
            .GroupBy(i => i.ProductName)
            .Select(g => g.First())
            .OrderByDescending(i => i.DaysSincePurchase)
            .Take(5)
            .ToList();

        return Ok(new { success = true, suggestions });
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════

    private async Task<ProductType> GetOrCreateProductTypeAsync(Guid establishmentId, string form)
    {
        var normalizedForm = NormalizePharmaceuticalForm(form);
        
        var productType = await _context.ProductTypes
            .FirstOrDefaultAsync(pt => pt.Name.ToLower() == normalizedForm.ToLower());

        if (productType == null)
        {
            productType = await _context.ProductTypes.FirstOrDefaultAsync();

            if (productType == null)
            {
                // ProductType não tem Code
                productType = new ProductType
                {
                    Id = Guid.NewGuid(),
                    Name = normalizedForm,
                    PharmaceuticalForm = normalizedForm,
                    Category = "MANIPULADOS",
                    Description = $"Forma farmacêutica: {normalizedForm}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.ProductTypes.Add(productType);
                await _context.SaveChangesAsync();
            }
        }

        return productType;
    }

    private async Task<ProductSubType> GetOrCreateProductSubTypeAsync(Guid productTypeId, string form)
    {
        var normalizedForm = NormalizePharmaceuticalForm(form);
        
        var productSubType = await _context.ProductSubTypes
            .FirstOrDefaultAsync(pst => pst.ProductTypeId == productTypeId);

        if (productSubType == null)
        {
            // ProductSubType não tem Code
            productSubType = new ProductSubType
            {
                Id = Guid.NewGuid(),
                ProductTypeId = productTypeId,
                Name = $"{normalizedForm} Padrão",
                Description = $"Subtipo padrão para {normalizedForm}",
                StandardQuantity = 1,
                StandardUnit = "un",
                PriceModifier = 1.0m,
                ManipulationCostBase = 15.00m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProductSubTypes.Add(productSubType);
            await _context.SaveChangesAsync();
        }

        return productSubType;
    }

    private static string NormalizePharmaceuticalForm(string form)
    {
        if (string.IsNullOrEmpty(form)) return "Creme";
        
        var lower = form.ToLower().Trim();
        return lower switch
        {
            "capsula" or "cápsula" or "capsulas" or "cápsulas" => "Cápsula",
            "creme" or "cremes" => "Creme",
            "gel" or "geis" or "géis" => "Gel",
            "pomada" or "pomadas" => "Pomada",
            "solucao" or "solução" or "soluções" => "Solução",
            "locao" or "loção" or "loções" => "Loção",
            "xarope" or "xaropes" => "Xarope",
            _ => form
        };
    }

    private static decimal ParseQuantity(string? quantity)
    {
        if (string.IsNullOrEmpty(quantity)) return 1;
        
        var match = System.Text.RegularExpressions.Regex.Match(quantity, @"[\d.,]+");
        if (match.Success && decimal.TryParse(match.Value.Replace(",", "."), 
            System.Globalization.NumberStyles.Any, 
            System.Globalization.CultureInfo.InvariantCulture, 
            out var result))
            return result;
        
        return 1;
    }

    private static string ParseUnit(string? quantity)
    {
        if (string.IsNullOrEmpty(quantity)) return "un";
        
        var lower = quantity.ToLower();
        if (lower.Contains("g") && !lower.Contains("mg") && !lower.Contains("kg")) return "g";
        if (lower.Contains("mg")) return "mg";
        if (lower.Contains("kg")) return "kg";
        if (lower.Contains("ml")) return "ml";
        if (lower.Contains("l") && !lower.Contains("ml")) return "L";
        if (lower.Contains("caps") || lower.Contains("cáps")) return "un";

        return "un";
    }

    private static bool IsAllowedPrescriptionFile(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64)) return false;
        try
        {
            // Remove data URL prefix if present (data:image/jpeg;base64,...)
            var raw = base64.Contains(',') ? base64[(base64.IndexOf(',') + 1)..] : base64;
            var bytes = Convert.FromBase64String(raw);
            if (bytes.Length < 8) return false;

            // JPEG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return true;
            // PNG: 89 50 4E 47 0D 0A 1A 0A
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return true;
            // PDF: 25 50 44 46 (%PDF)
            if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// DTOs LOCAIS (para evitar conflitos com DTOs existentes)
// ═══════════════════════════════════════════════════════════════════════════════

public class CustomerPrescriptionUploadDto
{
    public string FileBase64 { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? FileType { get; set; }
    public string? Observations { get; set; }
}

public class ConfirmIngredientsLocalDto
{
    public List<ConfirmedIngredientLocal> ConfirmedIngredients { get; set; } = new();
    public string? PharmaceuticalForm { get; set; }
    public string? TotalQuantity { get; set; }
    public string? Instructions { get; set; }
}

public class ConfirmedIngredientLocal
{
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "g";
}

public class IngredientMatchResultLocal
{
    public string OcrText { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsQsp { get; set; }
    public List<IngredientSuggestionLocal> Suggestions { get; set; } = new();
}

public class IngredientSuggestionLocal
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool InStock { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
