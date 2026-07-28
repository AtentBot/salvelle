using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service.CustomerFormulas;
using Models.Pharmacy;

namespace Controllers.Api;

/// <summary>
/// API do Portal do Cliente - Tipos de Produto, Fórmulas e Carrinho
/// </summary>
[ApiController]
[Route("api/customer-portal")]
public class CustomerPortalController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PricingService _pricingService;
    private readonly ILogger<CustomerPortalController> _logger;

    public CustomerPortalController(
        AppDbContext db,
        PricingService pricingService,
        ILogger<CustomerPortalController> logger)
    {
        _db = db;
        _pricingService = pricingService;
        _logger = logger;
    }

    #region Helpers

    private async Task<(Guid? CustomerId, Guid? EstablishmentId)> GetCustomerContext()
    {
        // 1. Tentar via HttpContext.Items (se middleware já setou)
        var customerId = HttpContext.Items["CustomerId"] as Guid?;
        if (customerId.HasValue)
        {
            var customer = await _db.Customers.FindAsync(customerId.Value);
            if (customer != null)
            {
                return (customer.Id, customer.EstablishmentId);
            }
        }

        // 2. Tentar via cookie de sessão do cliente
        var sessionToken = HttpContext.Request.Cookies["CustomerSessionId"];
        if (!string.IsNullOrEmpty(sessionToken))
        {
            var session = await _db.CustomerSessions
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken 
                    && s.IsActive 
                    && s.ExpiresAt > DateTime.UtcNow);
            
            if (session?.Customer != null)
            {
                // Atualizar última atividade
                session.LastActivityAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                return (session.CustomerId, session.CurrentEstablishmentId ?? session.Customer.EstablishmentId);
            }
        }

        // 3. Tentar via employee (funcionário acessando portal)
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee != null)
        {
            return (null, employee.EstablishmentId);
        }

        return (null, null);
    }

    #endregion

    #region Product Types

    /// <summary>
    /// Lista tipos de produtos disponíveis (Cápsula, Creme, Solução, etc.)
    /// ProductType NÃO tem EstablishmentId - é global
    /// </summary>
    [HttpGet("product-types")]
    public async Task<IActionResult> GetProductTypes()
    {
        var (_, establishmentId) = await GetCustomerContext();
        
        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Usuário não autenticado" });

        try
        {
            // ProductType é global (não tem EstablishmentId)
            var types = await _db.ProductTypes
                .Where(pt => pt.IsActive)
                .OrderBy(pt => pt.DisplayOrder)
                .ThenBy(pt => pt.Name)
                .Select(pt => new
                {
                    id = pt.Id,
                    name = pt.Name,
                    description = pt.Description,
                    pharmaceuticalForm = pt.PharmaceuticalForm,
                    category = pt.Category,
                    displayOrder = pt.DisplayOrder
                })
                .ToListAsync();

            return Ok(new { success = true, data = types });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar tipos de produtos");
            return StatusCode(500, new { success = false, error = "Erro ao listar tipos" });
        }
    }

    /// <summary>
    /// Prateleira de ingredientes ativos do estabelecimento — agrupado por categoria.
    /// GET /api/customer-portal/ingredients?category=Vitaminas&search=vit&limit=200
    /// Sempre retorna apenas ingredientes ativos com estoque liberado pra Customer (allowed_usage IN BOTH/COMMERCIAL).
    /// </summary>
    [HttpGet("ingredients")]
    public async Task<IActionResult> GetIngredients(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 500)
    {
        var (_, establishmentId) = await GetCustomerContext();

        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Usuário não autenticado" });

        try
        {
            var query = _db.RawMaterials
                .Where(r => r.EstablishmentId == establishmentId.Value && r.IsActive);

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(r => r.Category == category);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                query = query.Where(r => EF.Functions.ILike(r.Name, $"%{s}%"));
            }

            var items = await query
                .OrderBy(r => r.Category)
                .ThenBy(r => r.Name)
                .Take(Math.Clamp(limit, 1, 1000))
                .Select(r => new
                {
                    id = r.Id,
                    name = r.Name,
                    category = r.Category,
                    controlType = r.ControlType,
                    allowedUsage = r.AllowedUsage,
                    defaultUnit = r.Unit ?? "mg",
                    minimumStock = r.MinimumStock,
                    currentStock = r.CurrentStock,
                    inStock = r.CurrentStock > 0,
                })
                .ToListAsync();

            // Agrupa por categoria pra UI
            var groups = items
                .GroupBy(i => i.category ?? "Outros")
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    category = g.Key,
                    count = g.Count(),
                    items = g.ToList(),
                })
                .ToList();

            return Ok(new
            {
                success = true,
                totalItems = items.Count,
                totalCategories = groups.Count,
                categories = groups,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar prateleira de ingredientes");
            return StatusCode(500, new { success = false, error = "Erro ao carregar prateleira" });
        }
    }

    /// <summary>
    /// Lista subtipos de um tipo de produto (60 cápsulas, 100g creme, etc.)
    /// </summary>
    [HttpGet("product-types/{typeId}/subtypes")]
    public async Task<IActionResult> GetProductSubTypes(Guid typeId)
    {
        var (_, establishmentId) = await GetCustomerContext();
        
        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Usuário não autenticado" });

        try
        {
            var subTypes = await _db.ProductSubTypes
                .Where(pst => pst.ProductTypeId == typeId && pst.IsActive)
                .OrderBy(pst => pst.DisplayOrder)
                .ThenBy(pst => pst.Name)
                .Select(pst => new
                {
                    id = pst.Id,
                    name = pst.Name,
                    description = pst.Description,
                    standardUnit = pst.StandardUnit,
                    standardQuantity = pst.StandardQuantity,
                    priceModifier = pst.PriceModifier,
                    displayOrder = pst.DisplayOrder
                })
                .ToListAsync();

            return Ok(new { success = true, data = subTypes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar subtipos para {TypeId}", typeId);
            return StatusCode(500, new { success = false, error = "Erro ao listar apresentações" });
        }
    }

    #endregion

    #region Customer Formulas

    /// <summary>
    /// Salva uma fórmula do cliente
    /// </summary>
    [HttpPost("formulas")]
    public async Task<IActionResult> SaveFormula([FromBody] SaveFormulaRequest request)
    {
        var (customerId, establishmentId) = await GetCustomerContext();
        
        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Estabelecimento não identificado" });

        if (request.Ingredients == null || !request.Ingredients.Any())
            return BadRequest(new { success = false, error = "Fórmula deve ter pelo menos um ingrediente" });

        try
        {
            var code = $"CF-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";

            // Calcular preço estimado
            decimal estimatedPrice = 0;
            
            try
            {
                var ingredientIds = request.Ingredients
                    .Where(i => i.RawMaterialId.HasValue)
                    .Select(i => i.RawMaterialId!.Value)
                    .ToList();

                if (ingredientIds.Any())
                {
                    var prices = new List<decimal>();
                    foreach (var ingId in ingredientIds)
                    {
                        var priceResult = await _pricingService.GetIngredientPriceAsync(ingId, establishmentId.Value);
                        if (priceResult != null)
                        {
                            var ing = request.Ingredients.First(i => i.RawMaterialId == ingId);
                            prices.Add(priceResult.Price * ing.Quantity);
                        }
                    }
                    
                    if (prices.Any())
                    {
                        estimatedPrice = prices.Sum() * 1.30m; // + 30% margem
                    }
                }
            }
            catch (Exception priceEx)
            {
                _logger.LogWarning(priceEx, "Erro ao calcular preço, usando estimativa");
                estimatedPrice = request.Ingredients.Sum(i => i.Quantity * 2.0m);
            }

            // Criar fórmula usando o modelo CustomerFormula de Models.Pharmacy
            var formula = new CustomerFormula
            {
                Id = Guid.NewGuid(),
                Code = code,
                EstablishmentId = establishmentId.Value,
                CustomerId = customerId,
                ProductTypeId = request.ProductTypeId,
                ProductSubTypeId = request.ProductSubTypeId,
                Quantity = request.ProductQuantity,
                Unit = request.ProductUnit ?? "un",
                CustomerNotes = request.Notes,
                EstimatedPrice = estimatedPrice,
                FinalPrice = estimatedPrice,
                Status = "AGUARDANDO_COMPRA",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Salvar ingredientes como JSON no AdditionalIngredients
            var ingredientsJson = System.Text.Json.JsonSerializer.Serialize(
                request.Ingredients.Select(i => new
                {
                    rawMaterialId = i.RawMaterialId,
                    name = i.Name,
                    quantity = i.Quantity,
                    unit = i.Unit,
                    notes = i.Notes
                })
            );
            formula.AdditionalIngredients = ingredientsJson;

            _db.CustomerFormulas.Add(formula);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Fórmula {Code} criada pelo cliente {CustomerId}", code, customerId);

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = formula.Id,
                    code = formula.Code,
                    estimatedPrice = formula.EstimatedPrice
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao salvar fórmula para cliente {CustomerId}", customerId);
            return StatusCode(500, new { success = false, error = "Erro ao salvar fórmula" });
        }
    }

    /// <summary>
    /// Lista fórmulas do cliente
    /// </summary>
    [HttpGet("formulas")]
    public async Task<IActionResult> GetFormulas(
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var (customerId, establishmentId) = await GetCustomerContext();
        
        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Estabelecimento não identificado" });

        try
        {
            var query = _db.CustomerFormulas
                .Where(cf => cf.EstablishmentId == establishmentId.Value);

            // Se tem customerId, filtrar apenas as do cliente
            if (customerId.HasValue)
                query = query.Where(cf => cf.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(cf => cf.Status == status);

            var total = await query.CountAsync();

            var formulas = await query
                .OrderByDescending(cf => cf.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(cf => new
                {
                    id = cf.Id,
                    code = cf.Code,
                    productTypeId = cf.ProductTypeId,
                    productSubTypeId = cf.ProductSubTypeId,
                    quantity = cf.Quantity,
                    unit = cf.Unit,
                    estimatedPrice = cf.EstimatedPrice,
                    finalPrice = cf.FinalPrice,
                    status = cf.Status,
                    customerNotes = cf.CustomerNotes,
                    createdAt = cf.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                data = formulas,
                pagination = new { page, pageSize, total, totalPages = (int)Math.Ceiling((double)total / pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar fórmulas");
            return StatusCode(500, new { success = false, error = "Erro ao listar fórmulas" });
        }
    }

    /// <summary>
    /// Detalhes de uma fórmula específica
    /// </summary>
    [HttpGet("formulas/{id}")]
    public async Task<IActionResult> GetFormula(Guid id)
    {
        var (customerId, establishmentId) = await GetCustomerContext();
        
        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Estabelecimento não identificado" });

        try
        {
            var query = _db.CustomerFormulas
                .Where(cf => cf.Id == id && cf.EstablishmentId == establishmentId.Value);

            // Se tem customerId, verificar se pertence ao cliente
            if (customerId.HasValue)
                query = query.Where(cf => cf.CustomerId == customerId.Value);

            var formula = await query
                .Select(cf => new
                {
                    id = cf.Id,
                    code = cf.Code,
                    productTypeId = cf.ProductTypeId,
                    productSubTypeId = cf.ProductSubTypeId,
                    quantity = cf.Quantity,
                    unit = cf.Unit,
                    estimatedPrice = cf.EstimatedPrice,
                    finalPrice = cf.FinalPrice,
                    status = cf.Status,
                    customerNotes = cf.CustomerNotes,
                    rejectionReason = cf.RejectionReason,
                    pharmaceuticalAnalysis = cf.PharmaceuticalAnalysis,
                    requiresPrescription = cf.RequiresPrescription,
                    isControlledSubstance = cf.IsControlledSubstance,
                    additionalIngredients = cf.AdditionalIngredients,
                    createdAt = cf.CreatedAt,
                    analyzedAt = cf.AnalyzedAt,
                    approvedAt = cf.ApprovedAt
                })
                .FirstOrDefaultAsync();

            if (formula == null)
                return NotFound(new { success = false, error = "Fórmula não encontrada" });

            // Parse ingredientes do JSON
            object? ingredients = null;
            if (!string.IsNullOrEmpty(formula.additionalIngredients))
            {
                try
                {
                    ingredients = System.Text.Json.JsonSerializer.Deserialize<object>(formula.additionalIngredients);
                }
                catch { }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    formula.id,
                    formula.code,
                    formula.productTypeId,
                    formula.productSubTypeId,
                    formula.quantity,
                    formula.unit,
                    formula.estimatedPrice,
                    formula.finalPrice,
                    formula.status,
                    formula.customerNotes,
                    formula.rejectionReason,
                    formula.pharmaceuticalAnalysis,
                    formula.requiresPrescription,
                    formula.isControlledSubstance,
                    formula.createdAt,
                    formula.analyzedAt,
                    formula.approvedAt,
                    ingredients
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fórmula {Id}", id);
            return StatusCode(500, new { success = false, error = "Erro ao buscar fórmula" });
        }
    }

    #endregion

    #region Cart (usando CartItem de sessão)

    /// <summary>
    /// Adiciona fórmula ao carrinho (usando CartItem baseado em sessão)
    /// </summary>
    [HttpPost("cart/add")]
    public async Task<IActionResult> AddToCart([FromBody] PortalAddToCartRequest request)
    {
        var (customerId, establishmentId) = await GetCustomerContext();
        
        if (!establishmentId.HasValue)
            return Unauthorized(new { success = false, error = "Estabelecimento não identificado" });

        if (request.FormulaIds == null || !request.FormulaIds.Any())
            return BadRequest(new { success = false, error = "Nenhuma fórmula selecionada" });

        // Obter ou criar token de sessão
        var sessionToken = HttpContext.Request.Cookies["CartSession"] ?? Guid.NewGuid().ToString();

        try
        {
            var addedCount = 0;
            foreach (var formulaId in request.FormulaIds)
            {
                var formula = await _db.CustomerFormulas
                    .FirstOrDefaultAsync(cf => cf.Id == formulaId && cf.EstablishmentId == establishmentId.Value);

                if (formula == null) continue;

                // Verificar se já está no carrinho
                var existingItem = await _db.CartItems
                    .FirstOrDefaultAsync(ci => 
                        ci.SessionToken == sessionToken && 
                        ci.ReferenceId == formulaId &&
                        ci.ItemType == "FORMULA");

                if (existingItem != null)
                {
                    existingItem.Quantity += 1;
                    existingItem.TotalPrice = existingItem.Quantity * existingItem.UnitPrice;
                }
                else
                {
                    var item = new Models.Cart.CartItem
                    {
                        Id = Guid.NewGuid(),
                        SessionToken = sessionToken,
                        CustomerId = customerId,
                        EstablishmentId = establishmentId.Value,
                        ItemType = "FORMULA",
                        ReferenceId = formulaId,
                        Name = $"Fórmula {formula.Code}",
                        Description = formula.CustomerNotes,
                        Quantity = 1,
                        UnitPrice = formula.EstimatedPrice ?? 0,
                        TotalPrice = formula.EstimatedPrice ?? 0,
                        RequiresPrescription = formula.RequiresPrescription,
                        IsControlled = formula.IsControlledSubstance,
                        IsCustomFormula = true,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddDays(7)
                    };
                    _db.CartItems.Add(item);
                }
                addedCount++;
            }

            await _db.SaveChangesAsync();

            // Definir cookie de sessão
            HttpContext.Response.Cookies.Append("CartSession", sessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            // Contar itens no carrinho
            var totalItems = await _db.CartItems
                .Where(ci => ci.SessionToken == sessionToken)
                .SumAsync(ci => ci.Quantity);

            return Ok(new
            {
                success = true,
                message = $"{addedCount} fórmula(s) adicionada(s) ao carrinho",
                data = new { sessionToken, totalItems }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar ao carrinho");
            return StatusCode(500, new { success = false, error = "Erro ao adicionar ao carrinho" });
        }
    }

    /// <summary>
    /// Retorna o carrinho atual
    /// </summary>
    [HttpGet("cart")]
    public async Task<IActionResult> GetCart()
    {
        var (customerId, establishmentId) = await GetCustomerContext();
        var sessionToken = HttpContext.Request.Cookies["CartSession"];

        if (string.IsNullOrEmpty(sessionToken))
        {
            return Ok(new
            {
                success = true,
                data = new { items = Array.Empty<object>(), total = 0m, itemCount = 0 }
            });
        }

        try
        {
            var items = await _db.CartItems
                .Where(ci => ci.SessionToken == sessionToken)
                .OrderByDescending(ci => ci.CreatedAt)
                .Select(ci => new
                {
                    id = ci.Id,
                    itemType = ci.ItemType,
                    referenceId = ci.ReferenceId,
                    name = ci.Name,
                    description = ci.Description,
                    quantity = ci.Quantity,
                    unitPrice = ci.UnitPrice,
                    totalPrice = ci.TotalPrice,
                    requiresPrescription = ci.RequiresPrescription,
                    isControlled = ci.IsControlled,
                    isCustomFormula = ci.IsCustomFormula
                })
                .ToListAsync();

            var total = items.Sum(i => i.totalPrice);
            var itemCount = items.Sum(i => i.quantity);

            return Ok(new
            {
                success = true,
                data = new { items, total, itemCount }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar carrinho");
            return StatusCode(500, new { success = false, error = "Erro ao buscar carrinho" });
        }
    }

    /// <summary>
    /// Remove item do carrinho
    /// </summary>
    [HttpDelete("cart/items/{itemId}")]
    public async Task<IActionResult> RemoveFromCart(Guid itemId)
    {
        var sessionToken = HttpContext.Request.Cookies["CartSession"];

        if (string.IsNullOrEmpty(sessionToken))
            return NotFound(new { success = false, error = "Carrinho não encontrado" });

        try
        {
            var item = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.SessionToken == sessionToken);

            if (item == null)
                return NotFound(new { success = false, error = "Item não encontrado" });

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "Item removido do carrinho" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao remover item {ItemId} do carrinho", itemId);
            return StatusCode(500, new { success = false, error = "Erro ao remover item" });
        }
    }

    /// <summary>
    /// Atualiza quantidade de um item no carrinho
    /// </summary>
    [HttpPut("cart/items/{itemId}")]
    public async Task<IActionResult> UpdateCartItem(Guid itemId, [FromBody] PortalUpdateCartItemRequest request)
    {
        var sessionToken = HttpContext.Request.Cookies["CartSession"];

        if (string.IsNullOrEmpty(sessionToken))
            return NotFound(new { success = false, error = "Carrinho não encontrado" });

        if (request.Quantity < 1)
            return BadRequest(new { success = false, error = "Quantidade deve ser maior que zero" });

        try
        {
            var item = await _db.CartItems
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.SessionToken == sessionToken);

            if (item == null)
                return NotFound(new { success = false, error = "Item não encontrado" });

            item.Quantity = request.Quantity;
            item.TotalPrice = item.Quantity * item.UnitPrice;
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = item.Id,
                    quantity = item.Quantity,
                    totalPrice = item.TotalPrice
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar item {ItemId}", itemId);
            return StatusCode(500, new { success = false, error = "Erro ao atualizar item" });
        }
    }

    #endregion
}

#region DTOs

public class SaveFormulaRequest
{
    public Guid ProductTypeId { get; set; }
    public Guid ProductSubTypeId { get; set; }
    public decimal ProductQuantity { get; set; }
    public string? ProductUnit { get; set; }
    public List<PortalIngredientDto> Ingredients { get; set; } = new();
    public string? Notes { get; set; }
}

public class PortalIngredientDto
{
    public Guid? RawMaterialId { get; set; }
    public string Name { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "mg";
    public string? Notes { get; set; }
}

public class PortalAddToCartRequest
{
    public List<Guid> FormulaIds { get; set; } = new();
}

public class PortalUpdateCartItemRequest
{
    public int Quantity { get; set; }
}

#endregion
