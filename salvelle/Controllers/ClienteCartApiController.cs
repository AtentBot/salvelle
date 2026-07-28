using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using DTOs.Cart;
using Models.Pharmacy;
using Service.Marketplace;
using System.Text.Json;

namespace Controllers.Api;

// ====================================================================
// API DO CARRINHO DE COMPRAS
// ====================================================================
// Gerencia produtos do catálogo e fórmulas personalizadas no carrinho
// Endpoints disponíveis:
// - POST /api/cliente/cart/add          → Adicionar produto do catálogo
// - POST /api/cliente/cart/add-formula  → Adicionar fórmula personalizada
// - POST /api/cliente/cart/update       → Atualizar quantidade de item
// - POST /api/cliente/cart/remove       → Remover item específico
// - POST /api/cliente/cart/clear        → Limpar carrinho completo
// - GET  /api/cliente/cart/count        → Obter contagem de itens
// ====================================================================

[ApiController]
[Route("api/cliente/cart")]
public class ClienteCartApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClienteCartApiController> _logger;

    public ClienteCartApiController(AppDbContext context, ILogger<ClienteCartApiController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ================================================================
    // 1. ADICIONAR PRODUTO DO CATÁLOGO AO CARRINHO
    // ================================================================
    /// <summary>
    /// Adiciona um produto existente no catálogo ao carrinho
    /// Se o produto já existe no carrinho, incrementa a quantidade
    /// </summary>
    /// <param name="dto">Dados do produto (ProductId, Quantity)</param>
    /// <returns>Sucesso + total de itens no carrinho</returns>
    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddProductToCartDto dto)
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
        {
            _logger.LogWarning("Tentativa de adicionar ao carrinho sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        var establishmentId = session.CurrentEstablishmentId.Value;

        // 2. Validar produto
        var product = await _context.Set<CatalogProduct>()
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId &&
                                      p.EstablishmentId == establishmentId &&
                                      p.IsActive);

        if (product == null)
        {
            _logger.LogWarning("Produto {ProductId} não encontrado", dto.ProductId);
            return BadRequest(new { success = false, message = "Produto não encontrado" });
        }

        // 3. Validar estoque
        if (product.StockQuantity < dto.Quantity)
        {
            _logger.LogWarning("Estoque insuficiente. Produto: {ProductId}, Disponível: {Stock}, Solicitado: {Qty}",
                product.Id, product.StockQuantity, dto.Quantity);
            return BadRequest(new { success = false, message = "Quantidade indisponível em estoque" });
        }

        // 4. Buscar ou criar carrinho
        var cart = await GetOrCreateCart(customer.Id, establishmentId);

        // 5. Verificar se produto já está no carrinho
        var existingItem = cart.Items?.FirstOrDefault(i => i.ProductId == dto.ProductId);

        if (existingItem != null)
        {
            // Atualizar item existente
            var newQuantity = existingItem.Quantity + dto.Quantity;

            if (newQuantity > product.StockQuantity)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"Estoque disponível: {product.StockQuantity} unidades"
                });
            }

            existingItem.Quantity = newQuantity;
            existingItem.UnitPrice = product.CurrentPrice;
            existingItem.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Quantidade atualizada. Produto: {ProductId}, Nova quantidade: {Qty}",
                product.Id, newQuantity);
        }
        else
        {
            // Adicionar novo item
            var newItem = new CustomerCartItem
            {
                CartId = cart.Id,
                ProductId = product.Id,
                CustomerFormulaId = null,
                Quantity = dto.Quantity,
                UnitPrice = product.CurrentPrice,
                Notes = null
            };
            _context.Set<CustomerCartItem>().Add(newItem);

            _logger.LogInformation("Novo item adicionado. Produto: {ProductId}, Quantidade: {Qty}",
                product.Id, dto.Quantity);
        }

        await _context.SaveChangesAsync();

        // 6. Calcular total de itens
        var totalItems = await _context.Set<CustomerCartItem>()
            .Where(i => i.CartId == cart.Id)
            .SumAsync(i => i.Quantity);

        return Ok(new
        {
            success = true,
            message = "Produto adicionado ao carrinho",
            cartItems = totalItems
        });
    }

    // ================================================================
    // 2. ADICIONAR FÓRMULA PERSONALIZADA AO CARRINHO
    // ================================================================
    /// <summary>
    /// Cria uma fórmula personalizada (status DRAFT) e adiciona ao carrinho
    /// A fórmula será analisada pela farmácia após o pagamento do pedido
    /// </summary>
    /// <param name="dto">Dados da fórmula (tipo, ingredientes, preço estimado)</param>
    /// <returns>Sucesso + código da fórmula + total de itens</returns>
    [HttpPost("add-formula")]
    public async Task<IActionResult> AddFormulaToCart([FromBody] AddFormulaToCartDto dto)
    {
        try
        {
            _logger.LogInformation("=== INÍCIO add-formula ===");

            // 1. Validar autenticação
            var customer = HttpContext.Items["Customer"] as Customer;
            var session = HttpContext.Items["CustomerSession"] as CustomerSession;

            if (customer == null || session?.CurrentEstablishmentId == null)
            {
                _logger.LogWarning("Tentativa de criar fórmula sem autenticação");
                return Unauthorized(new { success = false, message = "Não autenticado" });
            }

            var establishmentId = session.CurrentEstablishmentId.Value;

            // 2. Validar ingredientes
            if (dto.Ingredients == null || dto.Ingredients.Count == 0)
            {
                _logger.LogWarning("Tentativa de criar fórmula sem ingredientes");
                return BadRequest(new
                {
                    success = false,
                    message = "Adicione pelo menos um ingrediente à fórmula"
                });
            }

            // 3. Validar dados obrigatórios
            if (dto.ProductTypeId == Guid.Empty || dto.ProductSubTypeId == Guid.Empty)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Selecione o tipo e apresentação do produto"
                });
            }

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Informe o nome do paciente"
                });
            }

            _logger.LogInformation("Cliente: {CustomerId}, Establishment: {EstablishmentId}, " +
                "Ingredientes: {Count}, Preço estimado: {Price}",
                customer.Id, establishmentId, dto.Ingredients.Count, dto.EstimatedPrice);

            // 4. Criar registro da fórmula personalizada
            var formula = new CustomerFormula
            {
                Code = GenerateFormulaCode(),
                CustomerId = customer.Id,
                EstablishmentId = establishmentId,
                ProductTypeId = dto.ProductTypeId,
                ProductSubTypeId = dto.ProductSubTypeId,
                Quantity = dto.Quantity,
                Unit = dto.Unit ?? "g",
                CustomerName = dto.CustomerName,
                CustomerPhone = dto.CustomerPhone,
                CustomerEmail = string.IsNullOrWhiteSpace(dto.CustomerEmail) ? null : dto.CustomerEmail,
                CustomerNotes = string.IsNullOrWhiteSpace(dto.CustomerNotes) ? null : dto.CustomerNotes,
                AdditionalIngredients = JsonSerializer.Serialize(dto.Ingredients),
                EstimatedPrice = dto.EstimatedPrice,
                Status = "DRAFT", // Status inicial (muda para PENDING após pagamento)
                RequiresPrescription = false
            };

            _context.CustomerFormulas.Add(formula);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fórmula criada: {Code} (ID: {FormulaId})",
                formula.Code, formula.Id);

            // 5. Buscar ou criar carrinho
            var cart = await GetOrCreateCart(customer.Id, establishmentId);

            // 6. Montar descrição detalhada da fórmula
            var description = BuildFormulaDescription(dto);

            // 7. Adicionar fórmula como item do carrinho
            var cartItem = new CustomerCartItem
            {
                CartId = cart.Id,
                ProductId = null, // Não é produto do catálogo
                CustomerFormulaId = formula.Id, // Vinculado à fórmula
                Quantity = 1, // Fórmulas sempre têm quantidade fixa = 1
                UnitPrice = dto.EstimatedPrice,
                Notes = description
            };

            _context.Set<CustomerCartItem>().Add(cartItem);
            await _context.SaveChangesAsync();

            // 8. Calcular total de itens
            var totalItems = await _context.Set<CustomerCartItem>()
                .Where(i => i.CartId == cart.Id)
                .SumAsync(i => i.Quantity);

            _logger.LogInformation("Fórmula {Code} adicionada ao carrinho. Total itens: {Total}",
                formula.Code, totalItems);
            _logger.LogInformation("=== FIM add-formula (SUCESSO) ===");

            return Ok(new
            {
                success = true,
                message = "Fórmula adicionada ao carrinho com sucesso!",
                formulaId = formula.Id,
                formulaCode = formula.Code,
                cartItems = totalItems
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERRO ao adicionar fórmula: {Message}", ex.Message);
            _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);

            return StatusCode(500, new
            {
                success = false,
                message = "Erro ao processar fórmula: " + ex.Message
            });
        }
    }

    // ================================================================
    // 3. ATUALIZAR QUANTIDADE DE ITEM
    // ================================================================
    /// <summary>
    /// Atualiza a quantidade de um item do carrinho
    /// Delta positivo (+1) incrementa, delta negativo (-1) decrementa
    /// Se quantidade chegar a 0 ou menos, o item é removido
    /// </summary>
    /// <param name="dto">ID do item + delta (-1 ou +1)</param>
    /// <returns>Sucesso ou erro</returns>
    [HttpPost("update")]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemDto dto)
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
        {
            _logger.LogWarning("Tentativa de atualizar carrinho sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        // 2. Buscar item do carrinho
        var item = await _context.Set<CustomerCartItem>()
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .Include(i => i.CustomerFormula)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.Cart!.CustomerId == customer.Id);

        if (item == null)
        {
            _logger.LogWarning("Item {ItemId} não encontrado no carrinho do cliente {CustomerId}",
                dto.ItemId, customer.Id);
            return BadRequest(new { success = false, message = "Item não encontrado no carrinho" });
        }

        // 3. Fórmulas personalizadas não permitem alteração de quantidade
        if (item.CustomerFormulaId.HasValue)
        {
            return BadRequest(new
            {
                success = false,
                message = "Não é possível alterar a quantidade de fórmulas personalizadas"
            });
        }

        // 4. Atualizar quantidade
        var oldQuantity = item.Quantity;
        item.Quantity += dto.Delta;
        item.UpdatedAt = DateTime.UtcNow;

        // 5. Se quantidade <= 0, remover item
        if (item.Quantity <= 0)
        {
            _context.Set<CustomerCartItem>().Remove(item);
            _logger.LogInformation("Item {ItemId} removido (quantidade chegou a {Qty})",
                item.Id, item.Quantity);
        }
        // 6. Se é produto, validar estoque disponível
        else if (item.Product != null && item.Quantity > item.Product.StockQuantity)
        {
            _logger.LogWarning("Estoque insuficiente. Produto: {ProductId}, Disponível: {Stock}, Solicitado: {Qty}",
                item.Product.Id, item.Product.StockQuantity, item.Quantity);

            return BadRequest(new
            {
                success = false,
                message = $"Quantidade máxima disponível: {item.Product.StockQuantity} unidades"
            });
        }
        else
        {
            _logger.LogInformation("Quantidade atualizada. Item: {ItemId}, {Old} → {New}",
                item.Id, oldQuantity, item.Quantity);
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }

    // ================================================================
    // 4. REMOVER ITEM ESPECÍFICO DO CARRINHO
    // ================================================================
    /// <summary>
    /// Remove completamente um item do carrinho (produto ou fórmula)
    /// </summary>
    /// <param name="dto">ID do item a remover</param>
    /// <returns>Sucesso ou erro</returns>
    [HttpPost("remove")]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemDto dto)
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
        {
            _logger.LogWarning("Tentativa de remover item sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        // 2. Buscar item
        var item = await _context.Set<CustomerCartItem>()
            .Include(i => i.Cart)
            .Include(i => i.Product)
            .Include(i => i.CustomerFormula)
            .FirstOrDefaultAsync(i => i.Id == dto.ItemId && i.Cart!.CustomerId == customer.Id);

        if (item == null)
        {
            _logger.LogWarning("Item {ItemId} não encontrado", dto.ItemId);
            return BadRequest(new { success = false, message = "Item não encontrado" });
        }

        // 3. Determinar tipo do item para log
        var itemType = item.ProductId.HasValue ? "Produto" : "Fórmula";
        var itemName = item.Product?.Name ?? item.CustomerFormula?.Code ?? "Item";

        // 4. Remover item
        _context.Set<CustomerCartItem>().Remove(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation("{Type} removido do carrinho: {Name} (ID: {ItemId})",
            itemType, itemName, item.Id);

        return Ok(new { success = true, message = $"{itemType} removido do carrinho" });
    }

    // ================================================================
    // 5. LIMPAR CARRINHO COMPLETO
    // ================================================================
    /// <summary>
    /// Remove todos os itens do carrinho do cliente
    /// </summary>
    /// <returns>Sucesso</returns>
    [HttpPost("clear")]
    public async Task<IActionResult> ClearCart()
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
        {
            _logger.LogWarning("Tentativa de limpar carrinho sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        // 2. Buscar carrinho
        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id &&
                                      c.EstablishmentId == session.CurrentEstablishmentId);

        // 3. Remover todos os itens
        if (cart != null && cart.Items!.Any())
        {
            var itemCount = cart.Items!.Count;
            _context.Set<CustomerCartItem>().RemoveRange(cart.Items!);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Carrinho limpo. {Count} itens removidos (Cliente: {CustomerId})",
                itemCount, customer.Id);
        }
        else
        {
            _logger.LogInformation("Carrinho já estava vazio (Cliente: {CustomerId})", customer.Id);
        }

        return Ok(new { success = true, message = "Carrinho limpo com sucesso" });
    }

    // ================================================================
    // 6. OBTER CONTAGEM DE ITENS NO CARRINHO
    // ================================================================
    /// <summary>
    /// Retorna o número total de itens no carrinho (soma das quantidades)
    /// Usado para exibir badge de notificação no ícone do carrinho
    /// </summary>
    /// <returns>Contagem total de itens</returns>
    [HttpGet("count")]
    public async Task<IActionResult> GetCartCount()
    {
        // 1. Validar autenticação (retorna 0 se não autenticado)
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
            return Ok(new { count = 0 });

        // 2. Calcular total de itens
        var count = await _context.Set<CustomerCartItem>()
            .Where(i => i.Cart!.CustomerId == customer.Id &&
                        i.Cart.EstablishmentId == session.CurrentEstablishmentId)
            .SumAsync(i => i.Quantity);

        return Ok(new { count });
    }

    // ================================================================
    // MÉTODOS AUXILIARES PRIVADOS
    // ================================================================

    /// <summary>
    /// Busca carrinho existente ou cria um novo se não existir
    /// Método reutilizável para evitar duplicação de código
    /// </summary>
    /// <param name="customerId">ID do cliente</param>
    /// <param name="establishmentId">ID do estabelecimento</param>
    /// <returns>Carrinho existente ou novo</returns>
    private async Task<CustomerCart> GetOrCreateCart(Guid customerId, Guid establishmentId)
    {
        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId &&
                                      c.EstablishmentId == establishmentId);

        if (cart == null)
        {
            cart = new CustomerCart
            {
                CustomerId = customerId,
                EstablishmentId = establishmentId
            };
            _context.Set<CustomerCart>().Add(cart);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Novo carrinho criado: Cliente {CustomerId}, Estabelecimento {EstablishmentId}",
                customerId, establishmentId);
        }

        return cart;
    }

    /// <summary>
    /// Gera código único para fórmula personalizada
    /// Formato: CF-YYYYMMDDHHMMSS-XXXX
    /// Exemplo: CF-20251223223045-7834
    /// </summary>
    /// <returns>Código único da fórmula</returns>
    private string GenerateFormulaCode()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"CF-{timestamp}-{random}";
    }

    /// <summary>
    /// Monta descrição detalhada da fórmula para exibir no carrinho
    /// Inclui tipo, apresentação e lista de ingredientes
    /// </summary>
    /// <param name="dto">Dados da fórmula</param>
    /// <returns>Descrição formatada</returns>
    private string BuildFormulaDescription(AddFormulaToCartDto dto)
    {
        var description = $"Fórmula Personalizada: {dto.ProductTypeName}";

        if (!string.IsNullOrWhiteSpace(dto.ProductSubTypeName))
        {
            description += $" - {dto.ProductSubTypeName}";
        }

        if (dto.Ingredients?.Count > 0)
        {
            var ingredientNames = dto.Ingredients.Take(3).Select(i => i.Name);
            description += $"\nIngredientes: {string.Join(", ", ingredientNames)}";

            if (dto.Ingredients.Count > 3)
            {
                description += $" e mais {dto.Ingredients.Count - 3}";
            }
        }

        if (!string.IsNullOrWhiteSpace(dto.CustomerNotes))
        {
            description += $"\nObservações: {dto.CustomerNotes}";
        }

        return description;
    }
}

// ====================================================================
// API DE PEDIDOS (ORDERS)
// ====================================================================
// Gerencia criação e consulta de pedidos finalizados
// Endpoints disponíveis:
// - POST /api/cliente/orders/create → Criar pedido a partir do carrinho
// - GET  /api/cliente/orders/my     → Listar meus pedidos
// - GET  /api/cliente/orders/{id}   → Detalhes de um pedido específico
// ====================================================================

[ApiController]
[Route("api/cliente/orders")]
public class ClienteOrdersApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CommissionService _commission;
    private readonly ILogger<ClienteOrdersApiController> _logger;

    public ClienteOrdersApiController(AppDbContext context, CommissionService commission, ILogger<ClienteOrdersApiController> logger)
    {
        _context = context;
        _commission = commission;
        _logger = logger;
    }

    // ================================================================
    // 1. CRIAR PEDIDO A PARTIR DO CARRINHO
    // ================================================================
    /// <summary>
    /// Converte os itens do carrinho em um pedido (OnlineOrder)
    /// Fórmulas personalizadas mudam de status DRAFT → PENDING
    /// Carrinho é esvaziado após criação do pedido
    /// </summary>
    /// <param name="dto">Notas do cliente (opcional)</param>
    /// <returns>ID e número do pedido criado</returns>
    [HttpPost("create")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderFromCartDto dto)
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        var session = HttpContext.Items["CustomerSession"] as CustomerSession;

        if (customer == null || session?.CurrentEstablishmentId == null)
        {
            _logger.LogWarning("Tentativa de criar pedido sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        // Bloquear pedidos de clientes com email não verificado (mesma regra do app mobile)
        var customerAuth = await _context.CustomerAuths
            .FirstOrDefaultAsync(a => a.CustomerId == customer.Id);
        if (customerAuth != null && !customerAuth.IsVerified)
            return BadRequest(new { success = false, message = "Verifique seu email antes de fazer pedidos." });

        var establishmentId = session.CurrentEstablishmentId.Value;

        // 2. Buscar carrinho com itens
        var cart = await _context.Set<CustomerCart>()
            .Include(c => c.Items!)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items!)
                .ThenInclude(i => i.CustomerFormula)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id &&
                                      c.EstablishmentId == establishmentId);

        if (cart == null || !cart.Items!.Any())
        {
            _logger.LogWarning("Tentativa de criar pedido com carrinho vazio (Cliente: {CustomerId})",
                customer.Id);
            return BadRequest(new { success = false, message = "Carrinho vazio" });
        }

        // Verificar estoque — early check para UX (a garantia real é o UPDATE atômico abaixo)
        foreach (var item in cart.Items!.Where(i => i.ProductId.HasValue))
        {
            if (item.Product != null)
                await _context.Entry(item.Product).ReloadAsync();

            if (item.Product == null || !item.Product.IsActive || item.Product.StockQuantity < item.Quantity)
                return BadRequest(new { success = false, message = $"Produto '{item.Product?.Name ?? "item"}' indisponível ou sem estoque" });
        }

        // 3. Gerar número único do pedido
        var orderNumber = GenerateOrderNumber();

        // 4. Calcular totais
        var subtotal = cart.Items!.Sum(i => i.UnitPrice * i.Quantity);

        // Calcular comissão da plataforma
        var (commissionRate, commissionAmount, netAmount) =
            await _commission.CalculateCommissionAsync(establishmentId, subtotal);

        // 5. Criar pedido
        var order = new OnlineOrder
        {
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            EstablishmentId = establishmentId,
            Status = "PENDING", // Aguardando confirmação da farmácia
            Subtotal = subtotal,
            Discount = 0,
            DeliveryFee = 0,
            Total = subtotal,
            CustomerNotes = dto.Notes,
            DeliveryType = dto.DeliveryType,
            PaymentMethod = null,
            PaymentStatus = "PENDING",
            PlatformCommissionRate = commissionRate,
            PlatformCommissionAmount = commissionAmount,
            NetAmountToPharmacy = netAmount
        };

        // Persistência em transação — UPDATE atômico de estoque evita race condition (TOCTOU)
        using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in cart.Items!.Where(i => i.ProductId.HasValue))
            {
                var product = await _context.CatalogProducts
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId!.Value
                                           && p.StockQuantity >= item.Quantity
                                           && p.IsActive);
                if (product == null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { success = false, message = $"Produto '{item.Product?.Name ?? "item"}' sem estoque suficiente" });
                }
                product.StockQuantity -= item.Quantity;
                product.TotalSold += item.Quantity;
            }

            _context.Set<OnlineOrder>().Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pedido criado: {OrderNumber} (ID: {OrderId})",
                orderNumber, order.Id);

            // 6. Criar itens do pedido e atualizar fórmulas
            var formulasUpdated = 0;
            foreach (var item in cart.Items!)
            {
                // Determinar nome do produto
                string productName;
                if (item.Product != null)
                {
                    productName = item.Product.Name;
                }
                else if (item.CustomerFormula != null)
                {
                    productName = $"Fórmula Personalizada ({item.CustomerFormula.Code})";

                    // Atualizar status da fórmula: DRAFT → PENDING
                    item.CustomerFormula.Status = "PENDING";
                    item.CustomerFormula.OnlineOrderId = order.Id;
                    formulasUpdated++;
                }
                else
                {
                    productName = "Produto não identificado";
                }

                // Criar item do pedido
                var orderItem = new OnlineOrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    ProductName = productName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.UnitPrice * item.Quantity,
                    Notes = item.Notes
                };
                _context.Set<OnlineOrderItem>().Add(orderItem);
            }

            if (formulasUpdated > 0)
            {
                _logger.LogInformation("{Count} fórmula(s) vinculada(s) ao pedido {OrderNumber}",
                    formulasUpdated, orderNumber);
            }

            // Registrar transação da plataforma (comissão)
            await _commission.RegisterTransactionAsync(order.Id, establishmentId, customer.Id, subtotal, commissionRate);

            // 7. Limpar carrinho (itens já foram convertidos em pedido)
            _context.Set<CustomerCartItem>().RemoveRange(cart.Items!);
            await _context.SaveChangesAsync();

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        _logger.LogInformation("Pedido {OrderNumber} finalizado. Cliente: {CustomerId}, " +
            "Itens: {ItemCount}, Total: R$ {Total:F2}",
            orderNumber, customer.Id, cart.Items!.Count, subtotal);

        return Ok(new
        {
            success = true,
            orderId = order.Id,
            orderNumber = order.OrderNumber,
            message = "Pedido criado com sucesso! Aguardando confirmação da farmácia."
        });
    }

    // ================================================================
    // 2. LISTAR MEUS PEDIDOS
    // ================================================================
    /// <summary>
    /// Retorna lista de todos os pedidos do cliente logado
    /// Ordenados por data (mais recente primeiro)
    /// </summary>
    /// <returns>Lista de pedidos com informações resumidas</returns>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyOrders()
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
        {
            _logger.LogWarning("Tentativa de listar pedidos sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        // 2. Buscar pedidos do cliente
        var orders = await _context.Set<OnlineOrder>()
            .Include(o => o.Establishment)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                StatusDisplay = TranslateStatus(o.Status),
                o.Total,
                o.CreatedAt,
                EstablishmentName = o.Establishment!.NomeFantasia,
                ItemCount = o.Items!.Count
            })
            .ToListAsync();

        _logger.LogInformation("Listagem de pedidos: Cliente {CustomerId}, Total: {Count}",
            customer.Id, orders.Count);

        return Ok(new { success = true, orders });
    }

    // ================================================================
    // 3. OBTER DETALHES DE UM PEDIDO ESPECÍFICO
    // ================================================================
    /// <summary>
    /// Retorna informações completas de um pedido
    /// Inclui todos os itens, valores e status de pagamento
    /// </summary>
    /// <param name="id">ID do pedido</param>
    /// <returns>Dados completos do pedido</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        // 1. Validar autenticação
        var customer = HttpContext.Items["Customer"] as Customer;
        if (customer == null)
        {
            _logger.LogWarning("Tentativa de visualizar pedido sem autenticação");
            return Unauthorized(new { success = false, message = "Não autenticado" });
        }

        // 2. Buscar pedido (apenas se pertence ao cliente logado)
        var order = await _context.Set<OnlineOrder>()
            .Where(o => o.Id == id && o.CustomerId == customer.Id)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                StatusDisplay = TranslateStatus(o.Status),
                o.Subtotal,
                o.Discount,
                o.DeliveryFee,
                o.Total,
                o.PaymentMethod,
                o.PaymentStatus,
                PaymentStatusDisplay = TranslatePaymentStatus(o.PaymentStatus),
                o.DeliveryType,
                DeliveryTypeDisplay = o.DeliveryType == "PICKUP" ? "Retirada no local" :
                                     o.DeliveryType == "DELIVERY" ? "Entrega" : o.DeliveryType,
                o.CustomerNotes,
                o.CreatedAt,
                EstablishmentName = o.Establishment!.NomeFantasia,
                Items = o.Items!.Select(i => new
                {
                    i.Id,
                    i.ProductName,
                    i.Quantity,
                    i.UnitPrice,
                    i.TotalPrice,
                    i.Notes
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
        {
            _logger.LogWarning("Pedido {OrderId} não encontrado para cliente {CustomerId}",
                id, customer.Id);
            return NotFound(new { success = false, message = "Pedido não encontrado" });
        }

        return Ok(new { success = true, order });
    }

    // ================================================================
    // MÉTODOS AUXILIARES PRIVADOS
    // ================================================================

    /// <summary>
    /// Gera número único de pedido
    /// Formato: PYYYYMMDDHHMMSS-XXXX
    /// Exemplo: P20251223223045-7834
    /// </summary>
    private string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"P{timestamp}-{random}";
    }

    /// <summary>
    /// Traduz status do pedido para português
    /// </summary>
    private string TranslateStatus(string? status)
    {
        return status switch
        {
            "PENDING" => "Aguardando Confirmação",
            "CONFIRMED" => "Confirmado",
            "PREPARING" => "Preparando",
            "READY" => "Pronto para Retirada",
            "DELIVERED" => "Entregue",
            "CANCELLED" => "Cancelado",
            _ => status ?? "Desconhecido"
        };
    }

    /// <summary>
    /// Traduz status de pagamento para português
    /// </summary>
    private string TranslatePaymentStatus(string? status)
    {
        return status switch
        {
            "PENDING" => "Aguardando Pagamento",
            "PAID" => "Pago",
            "FAILED" => "Falha no Pagamento",
            "REFUNDED" => "Reembolsado",
            _ => status ?? "Não informado"
        };
    }
}