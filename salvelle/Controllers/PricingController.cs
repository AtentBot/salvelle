using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service.CustomerFormulas;
using Models.Employees;
using Models.Pharmacy;

// Aliases para resolver conflitos de namespace
using PricingSettingsModel = Models.EstablishmentPricingSettings;
using PricingSettingsDto = DTOs.Pricing.EstablishmentPricingSettings;

namespace Controllers.Api;

/// <summary>
/// API de Precificação Inteligente de Ingredientes
/// Hierarquia: ESTOQUE (100%) > HISTÓRICO (50-95%) > BASE (30%)
/// 
/// ROTAS PÚBLICAS (Portal do Cliente):
/// - GET  /api/pricing/ingredient/search?name=
/// - GET  /api/pricing/ingredient/{id}
/// - POST /api/pricing/formula/calculate
/// - POST /api/pricing/ingredients/batch
/// - GET  /api/pricing/autocomplete?query=
/// 
/// ROTAS PROTEGIDAS (Funcionários):
/// - GET/PUT /api/pricing/settings
/// - POST /api/pricing/ingredient/{id}/update-price
/// - GET /api/pricing/statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PricingController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PricingService _pricingService;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        AppDbContext db,
        PricingService pricingService, 
        ILogger<PricingController> logger)
    {
        _db = db;
        _pricingService = pricingService;
        _logger = logger;
    }

    #region Helpers

    /// <summary>
    /// Obtém o funcionário logado do contexto (pode ser null em rotas públicas)
    /// </summary>
    private Employee? GetCurrentEmployee()
    {
        return HttpContext.Items["Employee"] as Employee;
    }

    /// <summary>
    /// Obtém o EstablishmentId do contexto (Employee ou Customer ou Default)
    /// </summary>
    private Guid GetEstablishmentId()
    {
        // 1. Funcionário logado
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var empEstId) && empEstId is Guid employeeEstId)
            return employeeEstId;

        // 2. Cliente logado (Portal do Cliente)
        if (HttpContext.Items.TryGetValue("CurrentEstablishmentId", out var custEstId) && custEstId is Guid customerEstId)
            return customerEstId;

        // 3. Sem fallback - exigir autenticação
        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    /// <summary>
    /// Obtém EstablishmentId apenas se houver funcionário autenticado
    /// </summary>
    private Guid? GetAuthenticatedEstablishmentId()
    {
        var employee = GetCurrentEmployee();
        return employee?.EstablishmentId;
    }

    /// <summary>
    /// Obtém o código do cargo do funcionário (JobPosition.Code)
    /// </summary>
    private string? GetEmployeeJobCode()
    {
        var employee = GetCurrentEmployee();
        return employee?.JobPosition?.Code;
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão de gerente/proprietário
    /// </summary>
    private bool IsManagerOrOwner()
    {
        var code = GetEmployeeJobCode()?.ToUpper();
        if (string.IsNullOrEmpty(code)) return false;

        return code == "MANAGER" ||
               code == "GERENTE" ||
               code == "PROPRIETARIO" ||
               code == "ADMIN" ||
               code == "OWNER";
    }

    /// <summary>
    /// Verifica se o funcionário é farmacêutico RT
    /// </summary>
    private bool IsPharmacistRT()
    {
        var code = GetEmployeeJobCode()?.ToUpper();
        if (string.IsNullOrEmpty(code)) return false;

        return code == "PHARMACIST_RT" ||
               code == "FARMACEUTICO_RT" ||
               code == "FARMACEUTICO" ||
               code == "RT";
    }

    /// <summary>
    /// Verifica se tem permissão para alterar configurações (Manager ou RT)
    /// </summary>
    private bool CanManageSettings()
    {
        return IsManagerOrOwner() || IsPharmacistRT();
    }

    #endregion

    #region Configurações (Protegidas)

    /// <summary>
    /// Obtém configurações de precificação do estabelecimento
    /// </summary>
    [HttpGet("settings")]
    public async Task<ActionResult<PricingSettingsDto>> GetSettings()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var settings = await _pricingService.GetPricingSettingsAsync(establishmentId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter configurações de precificação");
            return StatusCode(500, new { error = "Erro interno ao obter configurações" });
        }
    }

    /// <summary>
    /// Atualiza configurações de precificação (apenas Manager/RT)
    /// </summary>
    /// <summary>
    /// Atualiza configurações de precificação (apenas Manager/RT)
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] PricingSettingsDto settings)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação de funcionário necessária" });

            if (!CanManageSettings())
            {
                _logger.LogWarning("Tentativa de alterar configurações por {JobCode}", GetEmployeeJobCode());
                return Forbid();
            }

            // Converter DTO para Model
            var settingsModel = new PricingSettingsModel
            {
                EstablishmentId = employee.EstablishmentId,
                InflationRateMonthly = settings.InflationRateMonthly,
                SafetyMarginPercent = settings.SafetyMarginPercent,
                DefaultProfitMargin = settings.DefaultProfitMargin,
                PriceValidityDays = settings.PriceValidityDays,
                ManipulationFee = settings.ManipulationFee,
                DefaultPackagingCost = settings.DefaultPackagingCost,
                AlertOnEstimated = settings.AlertOnEstimated,
                BlockWithoutStock = settings.BlockWithoutStock
            };

            var success = await _pricingService.UpdatePricingSettingsAsync(employee.EstablishmentId, settingsModel);

            if (success)
                return Ok(new { message = "Configurações atualizadas com sucesso" });
            else
                return StatusCode(500, new { error = "Erro ao atualizar configurações" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar configurações de precificação");
            return StatusCode(500, new { error = "Erro interno ao atualizar configurações" });
        }
    }

    #endregion

    #region Autocomplete (Pública)

    /// <summary>
    /// Autocomplete de ingredientes para busca de preços
    /// GET /api/pricing/autocomplete?query=vitamina&limit=10
    /// </summary>
    [HttpGet("autocomplete")]
    public async Task<ActionResult<IEnumerable<DTOs.Pricing.IngredientAutocompleteDto>>> Autocomplete(
        [FromQuery] string query, 
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new List<DTOs.Pricing.IngredientAutocompleteDto>());
        }

        try
        {
            var establishmentId = GetEstablishmentId();
            var normalizedQuery = query.ToLower().Trim();

            // Buscar matérias-primas que correspondem à query
            var materials = await _db.RawMaterials
                .Where(r => r.EstablishmentId == establishmentId && 
                           r.IsActive &&
                           (r.Name.ToLower().Contains(normalizedQuery) ||
                            (r.Synonyms != null && r.Synonyms.ToLower().Contains(normalizedQuery)) ||
                            (r.DcbCode != null && r.DcbCode.ToLower().Contains(normalizedQuery))))
                .OrderByDescending(r => r.Name.ToLower().StartsWith(normalizedQuery))
                .ThenByDescending(r => r.Popularity)
                .ThenBy(r => r.Name)
                .Take(limit)
                .Select(r => new DTOs.Pricing.IngredientAutocompleteDto
                {
                    Id = r.Id,
                    Name = r.Name,
                    Category = r.Category,
                    Unit = r.Unit,
                    DcbCode = r.DcbCode,
                    CurrentStock = r.CurrentStock,
                    BasePrice = r.BasePrice,
                    LastKnownPrice = r.LastKnownPrice,
                    PriceSource = r.CurrentStock > 0 ? "ESTOQUE" : 
                                 (r.LastKnownPrice.HasValue && r.LastKnownPrice > 0 ? "HISTORICO" : "BASE"),
                    UnitCost = r.CurrentStock > 0 ? 
                              (r.LastKnownPrice ?? r.BasePrice ?? 0) : 
                              (r.LastKnownPrice ?? r.BasePrice ?? 0)
                })
                .ToListAsync();

            // Buscar preço real do estoque para materiais com estoque
            foreach (var material in materials.Where(m => m.CurrentStock > 0))
            {
                var latestBatch = await _db.Batches
                    .Where(b => b.RawMaterialId == material.Id && 
                               b.Status == "APROVADO" && 
                               b.CurrentQuantity > 0)
                    .OrderByDescending(b => b.ReceivedDate)
                    .FirstOrDefaultAsync();

                if (latestBatch != null)
                {
                    material.UnitCost = latestBatch.UnitCost;
                }
            }

            return Ok(materials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no autocomplete de ingredientes para query: {Query}", query);
            return Ok(new List<DTOs.Pricing.IngredientAutocompleteDto>());
        }
    }

    #endregion

    #region Preço Individual de Ingrediente (Públicas)

    /// <summary>
    /// Obtém preço de um ingrediente específico por ID
    /// </summary>
    [HttpGet("ingredient/{rawMaterialId:guid}")]
    public async Task<ActionResult<Service.CustomerFormulas.IngredientPriceResult>> GetIngredientPrice(Guid rawMaterialId)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var result = await _pricingService.GetIngredientPriceAsync(rawMaterialId, establishmentId);

            if (result == null)
                return NotFound(new { error = "Ingrediente não encontrado" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter preço do ingrediente {RawMaterialId}", rawMaterialId);
            return StatusCode(500, new { error = "Erro interno ao obter preço" });
        }
    }

    /// <summary>
    /// Busca ingrediente por nome e retorna preço
    /// </summary>
    [HttpGet("ingredient/search")]
    public async Task<ActionResult<Service.CustomerFormulas.IngredientPriceResult>> SearchIngredientPrice([FromQuery] string name)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { error = "Nome do ingrediente é obrigatório" });

            var establishmentId = GetEstablishmentId();
            var result = await _pricingService.GetIngredientPriceByNameAsync(name, establishmentId);

            if (result == null)
                return NotFound(new { error = "Ingrediente não encontrado" });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar preço do ingrediente {Name}", name);
            return StatusCode(500, new { error = "Erro interno ao buscar preço" });
        }
    }

    /// <summary>
    /// Obtém preços de múltiplos ingredientes em lote
    /// </summary>
    [HttpPost("ingredients/batch")]
    public async Task<ActionResult<List<Service.CustomerFormulas.IngredientPriceResult>>> GetIngredientPricesBatch([FromBody] List<Guid> rawMaterialIds)
    {
        try
        {
            if (rawMaterialIds == null || !rawMaterialIds.Any())
                return BadRequest(new { error = "Lista de IDs não pode ser vazia" });

            var establishmentId = GetEstablishmentId();
            var results = new List<Service.CustomerFormulas.IngredientPriceResult>();

            foreach (var id in rawMaterialIds.Distinct())
            {
                var result = await _pricingService.GetIngredientPriceAsync(id, establishmentId);
                if (result != null)
                    results.Add(result);
            }

            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter preços em lote");
            return StatusCode(500, new { error = "Erro interno ao obter preços" });
        }
    }

    #endregion

    #region Cálculo de Preço de Fórmula (Pública)

    /// <summary>
    /// Calcula preço completo de uma fórmula com lista de ingredientes
    /// ROTA PÚBLICA - Funciona para Portal do Cliente
    /// </summary>
    [HttpPost("formula/calculate")]
    public async Task<IActionResult> CalculateFormulaPrice([FromBody] CalculateFormulaPriceRequest request)
    {
        try
        {
            if (request.Ingredients == null || !request.Ingredients.Any())
                return BadRequest(new { success = false, error = "Lista de ingredientes é obrigatória" });

            var establishmentId = GetEstablishmentId();
            
            // Obter configurações de precificação
            var settings = await _pricingService.GetPricingSettingsAsync(establishmentId);
            
            var ingredientResults = new List<object>();
            decimal totalIngredientsCost = 0;
            int inStockCount = 0;
            int estimatedCount = 0;
            int baseCount = 0;
            var warnings = new List<string>();
            var outOfStockItems = new List<string>();

            foreach (var ingredient in request.Ingredients)
            {
                Service.CustomerFormulas.IngredientPriceResult? priceResult = null;

                // Tentar buscar por ID primeiro
                if (ingredient.RawMaterialId.HasValue)
                {
                    priceResult = await _pricingService.GetIngredientPriceAsync(
                        ingredient.RawMaterialId.Value, establishmentId);
                }
                // Se não encontrou por ID, buscar por nome
                else if (!string.IsNullOrEmpty(ingredient.Name))
                {
                    priceResult = await _pricingService.GetIngredientPriceByNameAsync(
                        ingredient.Name, establishmentId);
                }

                if (priceResult == null)
                {
                    ingredientResults.Add(new
                    {
                        name = ingredient.Name ?? "Desconhecido",
                        quantity = ingredient.Quantity,
                        unit = ingredient.Unit ?? "mg",
                        unitCost = 0m,
                        totalCost = 0m,
                        priceSource = "NAO_ENCONTRADO",
                        confidence = 0,
                        statusIcon = "⚪",
                        warning = "Ingrediente não encontrado no cadastro"
                    });
                    warnings.Add($"Ingrediente '{ingredient.Name}' não encontrado");
                    continue;
                }

                // Calcular quantidade normalizada
                var quantityInGrams = NormalizeQuantity(ingredient.Quantity, ingredient.Unit ?? "mg");
                var totalCost = quantityInGrams * priceResult.Price * request.ProductQuantity;
                totalIngredientsCost += totalCost;

                // Contadores por fonte
                switch (priceResult.Source)
                {
                    case Service.CustomerFormulas.PriceSource.ESTOQUE:
                        inStockCount++;
                        break;
                    case Service.CustomerFormulas.PriceSource.HISTORICO:
                        estimatedCount++;
                        break;
                    case Service.CustomerFormulas.PriceSource.BASE:
                        baseCount++;
                        break;
                }

                // Verificar estoque
                if (priceResult.AvailableStock <= 0)
                {
                    outOfStockItems.Add(priceResult.Name);
                }

                ingredientResults.Add(new
                {
                    rawMaterialId = priceResult.RawMaterialId,
                    name = priceResult.Name,
                    quantity = ingredient.Quantity,
                    unit = ingredient.Unit ?? "mg",
                    unitCost = priceResult.Price,
                    totalCost = Math.Round(totalCost, 2),
                    priceSource = priceResult.Source.ToString(),
                    confidence = priceResult.Confidence,
                    statusIcon = priceResult.StatusIcon,
                    availableStock = priceResult.AvailableStock,
                    warning = priceResult.Warning
                });

                if (!string.IsNullOrEmpty(priceResult.Warning))
                    warnings.Add(priceResult.Warning);
            }

            // Custos adicionais
            var manipulationCost = settings?.ManipulationFee ?? 25m;
            var packagingCost = settings?.DefaultPackagingCost ?? 5m;
            var totalCostBeforeMargin = totalIngredientsCost + manipulationCost + packagingCost;

            // Margem de lucro
            var profitMarginPercent = settings?.DefaultProfitMargin ?? 100m;
            var profitMarginMultiplier = 1 + (profitMarginPercent / 100m);
            var suggestedPrice = totalCostBeforeMargin * profitMarginMultiplier;

            // Confiança média
            var totalItems = inStockCount + estimatedCount + baseCount;
            var averageConfidence = totalItems > 0
                ? (inStockCount * 100 + estimatedCount * 70 + baseCount * 30) / totalItems
                : 0;

            // Prazo estimado
            int estimatedDays = outOfStockItems.Any() ? 7 : 3;
            var deliveryMessage = outOfStockItems.Any()
                ? $"Prazo estendido: {outOfStockItems.Count} item(ns) precisam ser adquiridos"
                : "Todos os ingredientes em estoque";

            return Ok(new
            {
                success = true,
                ingredients = ingredientResults,
                summary = new
                {
                    totalIngredientsCost = Math.Round(totalIngredientsCost, 2),
                    manipulationCost,
                    packagingCost,
                    totalCostBeforeMargin = Math.Round(totalCostBeforeMargin, 2),
                    profitMarginPercent,
                    suggestedPrice = Math.Round(suggestedPrice, 2)
                },
                confidence = new
                {
                    average = averageConfidence,
                    inStockCount,
                    estimatedCount,
                    baseCount
                },
                hasOutOfStock = outOfStockItems.Any(),
                outOfStockItems,
                estimatedDays,
                deliveryMessage,
                warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao calcular preço da fórmula");
            return StatusCode(500, new { success = false, error = "Erro interno ao calcular preço" });
        }
    }

    /// <summary>
    /// Recalcula preço de uma fórmula existente (CustomerFormula)
    /// </summary>
    [HttpPost("formula/{formulaId:guid}/recalculate")]
    public async Task<IActionResult> RecalculateFormulaPrice(Guid formulaId)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação de funcionário necessária" });

            var success = await _pricingService.RecalculateFormulaPriceAsync(formulaId);

            if (success)
                return Ok(new { message = "Preço da fórmula recalculado com sucesso" });
            else
                return NotFound(new { error = "Fórmula não encontrada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao recalcular preço da fórmula {FormulaId}", formulaId);
            return StatusCode(500, new { error = "Erro interno ao recalcular preço" });
        }
    }

    #endregion

    #region Estatísticas (Protegida)

    /// <summary>
    /// Obtém estatísticas de precificação do estabelecimento
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<PricingStatistics>> GetStatistics()
    {
        try
        {
            var establishmentId = GetAuthenticatedEstablishmentId();
            if (!establishmentId.HasValue)
                return Unauthorized(new { error = "Autenticação de funcionário necessária" });

            var stats = await _pricingService.GetPricingStatisticsAsync(establishmentId.Value);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter estatísticas de precificação");
            return StatusCode(500, new { error = "Erro interno ao obter estatísticas" });
        }
    }

    #endregion

    #region Utilitários

    /// <summary>
    /// Aplica desconto a um preço
    /// </summary>
    [HttpPost("apply-discount")]
    public ActionResult ApplyDiscount([FromBody] ApplyDiscountRequest request)
    {
        try
        {
            if (request.Price <= 0)
                return BadRequest(new { error = "Preço deve ser maior que zero" });

            if (request.DiscountPercentage < 0 || request.DiscountPercentage > 1)
                return BadRequest(new { error = "Desconto deve estar entre 0 e 1" });

            var finalPrice = _pricingService.ApplyDiscount(request.Price, request.DiscountPercentage);

            return Ok(new
            {
                originalPrice = request.Price,
                discountPercentage = request.DiscountPercentage,
                discountPercent = $"{request.DiscountPercentage * 100:N0}%",
                discountAmount = Math.Round(request.Price - finalPrice, 2),
                finalPrice
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aplicar desconto");
            return StatusCode(500, new { error = "Erro interno" });
        }
    }

    /// <summary>
    /// Atualiza o último preço conhecido de um ingrediente (apenas Manager/RT)
    /// </summary>
    [HttpPost("ingredient/{rawMaterialId:guid}/update-price")]
    public async Task<IActionResult> UpdateIngredientPrice(Guid rawMaterialId, [FromBody] UpdatePriceRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação de funcionário necessária" });

            if (!CanManageSettings())
                return Forbid();

            if (request.UnitCost <= 0)
                return BadRequest(new { error = "Custo unitário deve ser maior que zero" });

            await _pricingService.UpdateLastKnownPriceAsync(rawMaterialId, request.UnitCost);

            return Ok(new
            {
                message = "Preço atualizado com sucesso",
                rawMaterialId,
                newPrice = request.UnitCost
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar preço do ingrediente {RawMaterialId}", rawMaterialId);
            return StatusCode(500, new { error = "Erro interno" });
        }
    }

    #endregion

    #region Métodos Auxiliares Privados

    private PriceInfo GetPriceInfo(RawMaterial rm)
    {
        decimal price;
        string source;
        decimal confidence;

        if (rm.CurrentStock > 0 && rm.LastKnownPrice.HasValue && rm.LastKnownPrice.Value > 0)
        {
            price = rm.LastKnownPrice.Value;
            source = "ESTOQUE";
            confidence = 100m;
        }
        else if (rm.LastKnownPrice.HasValue && rm.LastKnownPrice.Value > 0)
        {
            price = rm.LastKnownPrice.Value;
            source = "HISTORICO";
            
            if (rm.LastPriceDate.HasValue)
            {
                var daysSince = (DateTime.UtcNow - rm.LastPriceDate.Value).TotalDays;
                confidence = daysSince < 90 ? 80m : daysSince < 180 ? 60m : 50m;
            }
            else
            {
                confidence = 50m;
            }
        }
        else if (rm.BasePrice.HasValue && rm.BasePrice.Value > 0)
        {
            price = rm.BasePrice.Value;
            source = "BASE";
            confidence = 30m;
        }
        else
        {
            price = 0.50m;
            source = "BASE";
            confidence = 20m;
        }

        return new PriceInfo { Price = price, Source = source, Confidence = confidence };
    }

    private decimal NormalizeQuantity(decimal quantity, string unit)
    {
        return unit.ToLower() switch
        {
            "kg" => quantity * 1000,
            "g" => quantity,
            "mg" => quantity / 1000,
            "mcg" or "µg" => quantity / 1000000,
            "l" => quantity * 1000,
            "ml" => quantity,
            "ui" or "iu" => quantity / 1000,
            "%" => quantity,
            _ => quantity
        };
    }

    private class PriceInfo
    {
        public decimal Price { get; set; }
        public string Source { get; set; } = "";
        public decimal Confidence { get; set; }
    }

    #endregion
}

// ════════════════════════════════════════════════════════════════════════════
// DTOs LOCAIS DO CONTROLLER (evita conflitos de namespace)
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Request para cálculo de preço de fórmula com ingredientes
/// </summary>
public class CalculateFormulaPriceRequest
{
    public string? ProductType { get; set; } = "Cápsula";
    public int ProductQuantity { get; set; } = 60;
    public List<FormulaIngredientInput> Ingredients { get; set; } = new();
}

/// <summary>
/// Ingrediente para cálculo de fórmula
/// </summary>
public class FormulaIngredientInput
{
    public Guid? RawMaterialId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
}

/// <summary>
/// Request para aplicar desconto
/// </summary>
public class ApplyDiscountRequest
{
    public decimal Price { get; set; }
    public decimal DiscountPercentage { get; set; }
}

/// <summary>
/// Request para atualização manual de preço
/// </summary>
public class UpdatePriceRequest
{
    public decimal UnitCost { get; set; }
}

/// <summary>
/// Estatísticas de precificação
/// </summary>
public class PricingStatistics
{
    public int TotalIngredients { get; set; }
    public int InStockCount { get; set; }
    public int HistoricalCount { get; set; }
    public int BaseCount { get; set; }
    public decimal TotalStockValue { get; set; }
}
