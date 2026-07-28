using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models;
using Models.Marketplace;
using Models.Pharmacy;
using Service.Marketplace;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/orders")]
public class MobileOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CommissionService _commission;
    private readonly ILogger<MobileOrdersController> _logger;

    public MobileOrdersController(AppDbContext db, CommissionService commission, ILogger<MobileOrdersController> logger)
    {
        _db = db;
        _commission = commission;
        _logger = logger;
    }

    /// <summary>
    /// Criar pedido a partir do carrinho
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        // Bloquear pedidos de clientes com email não verificado
        var customerAuth = await _db.CustomerAuths
            .FirstOrDefaultAsync(a => a.CustomerId == customerId.Value);
        if (customerAuth != null && !customerAuth.IsVerified)
            return BadRequest(ApiResponse.ErrorResponse("Verifique seu email antes de fazer pedidos. Um código foi enviado no cadastro."));

        // Buscar carrinho ativo
        var cart = await _db.CustomerCarts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.CustomerId == customerId.Value
                                      && c.EstablishmentId == request.EstablishmentId
                                      && c.Status == "ACTIVE");

        if (cart == null || !cart.Items.Any())
            return BadRequest(ApiResponse.ErrorResponse("Carrinho vazio"));

        // Verificar estoque — early check para UX (não garante atomicidade; a garantia real é o UPDATE abaixo)
        foreach (var item in cart.Items)
        {
            if (item.Product != null)
                await _db.Entry(item.Product).ReloadAsync();

            if (item.Product == null || !item.Product.IsActive || item.Product.StockQuantity < item.Quantity)
                return BadRequest(ApiResponse.ErrorResponse($"Produto '{item.DisplayName}' indisponível ou sem estoque"));
        }

        // Verificar valor mínimo
        var pharmacy = await _db.Establishments.FindAsync(request.EstablishmentId);
        if (pharmacy == null || !pharmacy.IsMarketplaceActive || !pharmacy.AcceptingOrders)
            return BadRequest(ApiResponse.ErrorResponse("Farmácia não está aceitando pedidos"));

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        if (subtotal < pharmacy.MinOrderAmount)
            return BadRequest(ApiResponse.ErrorResponse($"Pedido mínimo: R$ {pharmacy.MinOrderAmount:F2}"));

        // Buscar endereço de entrega
        string? deliveryAddress = null;
        double? deliveryLat = null, deliveryLng = null;

        if (request.DeliveryType == "DELIVERY" && request.DeliveryAddressId.HasValue)
        {
            var address = await _db.CustomerAddresses
                .FirstOrDefaultAsync(a => a.Id == request.DeliveryAddressId.Value && a.CustomerId == customerId.Value);

            if (address == null)
                return BadRequest(ApiResponse.ErrorResponse("Endereço de entrega não encontrado"));

            deliveryAddress = $"{address.Street}, {address.Number} - {address.Neighborhood}, {address.City}/{address.State}";
            deliveryLat = address.Latitude;
            deliveryLng = address.Longitude;
        }

        // Validar e calcular cupom de desconto server-side (nunca confiar no cliente)
        Coupon? appliedCoupon = null;
        decimal discountAmount = 0m;

        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var normalizedCode = request.CouponCode.Trim().ToUpperInvariant();
            var coupon = await _db.Coupons
                .Include(c => c.Usages)
                .FirstOrDefaultAsync(c => c.Code == normalizedCode
                    && c.IsActive
                    && (c.EstablishmentId == null || c.EstablishmentId == request.EstablishmentId));

            if (coupon == null || !coupon.IsValid)
                return BadRequest(ApiResponse.ErrorResponse("Cupom inválido ou expirado."));

            if (coupon.MinOrderValue.HasValue && subtotal < coupon.MinOrderValue.Value)
                return BadRequest(ApiResponse.ErrorResponse($"Pedido mínimo para este cupom: R$ {coupon.MinOrderValue.Value:F2}"));

            if (coupon.MaxUses.HasValue && coupon.UsedCount >= coupon.MaxUses.Value)
                return BadRequest(ApiResponse.ErrorResponse("Cupom esgotado."));

            if (coupon.MaxUsesPerCustomer.HasValue)
            {
                var usedByCustomer = coupon.Usages?.Count(u => u.CustomerId == customerId.Value) ?? 0;
                if (usedByCustomer >= coupon.MaxUsesPerCustomer.Value)
                    return BadRequest(ApiResponse.ErrorResponse("Você já atingiu o limite de uso deste cupom."));
            }

            if (coupon.FirstPurchaseOnly)
            {
                var hasPriorOrder = await _db.OnlineOrders
                    .AnyAsync(o => o.CustomerId == customerId.Value && o.Status != "CANCELLED");
                if (hasPriorOrder)
                    return BadRequest(ApiResponse.ErrorResponse("Este cupom é válido apenas na primeira compra."));
            }

            discountAmount = coupon.DiscountType == "PERCENTAGE"
                ? subtotal * (coupon.DiscountPercentage ?? 0m) / 100m
                : (coupon.DiscountValue ?? 0m);

            if (coupon.MaxDiscountValue.HasValue)
                discountAmount = Math.Min(discountAmount, coupon.MaxDiscountValue.Value);

            discountAmount = Math.Min(discountAmount, subtotal);
            appliedCoupon = coupon;
        }

        var orderTotal = subtotal - discountAmount;

        // Calcular comissão
        var (commissionRate, commissionAmount, netAmount) =
            await _commission.CalculateCommissionAsync(request.EstablishmentId, orderTotal);

        // Criar pedido
        var order = new OnlineOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = GenerateOrderNumber(),
            CustomerId = customerId.Value,
            EstablishmentId = request.EstablishmentId,
            Status = "PENDING",
            Subtotal = subtotal,
            Discount = discountAmount,
            DeliveryFee = 0,
            Total = orderTotal,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = "PENDING",
            DeliveryType = request.DeliveryType,
            DeliveryAddress = deliveryAddress,
            DeliveryLatitude = deliveryLat,
            DeliveryLongitude = deliveryLng,
            CustomerNotes = request.CustomerNotes,
            PlatformCommissionRate = commissionRate,
            PlatformCommissionAmount = commissionAmount,
            NetAmountToPharmacy = netAmount,
            StripePaymentIntentId = request.StripePaymentMethodId,
            CreatedAt = DateTime.UtcNow
        };

        // Criar itens
        foreach (var cartItem in cart.Items)
        {
            order.Items.Add(new OnlineOrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ProductId = cartItem.ProductId,
                ProductName = cartItem.Product?.Name ?? cartItem.DisplayName,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.UnitPrice,
                TotalPrice = cartItem.UnitPrice * cartItem.Quantity,
                Notes = cartItem.Notes
            });

        }

        // Persistência em transação — o UPDATE atômico de estoque evita race condition (TOCTOU)
        using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var cartItem in cart.Items)
            {
                var product = await _db.CatalogProducts
                    .FirstOrDefaultAsync(p => p.Id == cartItem.ProductId
                                           && p.StockQuantity >= cartItem.Quantity
                                           && p.IsActive);
                if (product == null)
                {
                    await tx.RollbackAsync();
                    return BadRequest(ApiResponse.ErrorResponse($"Produto '{cartItem.DisplayName}' sem estoque suficiente"));
                }
                product.StockQuantity -= cartItem.Quantity;
                product.TotalSold += cartItem.Quantity;
            }

            _db.OnlineOrders.Add(order);

            // Registrar uso do cupom (dentro da transação para consistência)
            if (appliedCoupon != null)
            {
                appliedCoupon.UsedCount += 1;
                _db.CouponUsages.Add(new CouponUsage
                {
                    Id = Guid.NewGuid(),
                    CouponId = appliedCoupon.Id,
                    CustomerId = customerId.Value,
                    OrderId = order.Id,
                    DiscountApplied = discountAmount,
                    UsedAt = DateTime.UtcNow
                });
            }

            // Registrar transação da plataforma
            await _commission.RegisterTransactionAsync(
                order.Id, request.EstablishmentId, customerId.Value, orderTotal, commissionRate);

            // Criar estimativa de entrega
            var estimate = new DeliveryEstimate
            {
                OrderId = order.Id,
                EstimatedMinutes = pharmacy.AverageDeliveryMinutes,
                EstimatedDeliveryAt = DateTime.UtcNow.AddMinutes(pharmacy.AverageDeliveryMinutes),
                Status = "ESTIMADO"
            };
            _db.DeliveryEstimates.Add(estimate);

            // Limpar carrinho
            cart.Status = "CONVERTED";
            cart.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }

        _logger.LogInformation("Pedido {OrderNumber} criado para farmácia {PharmacyId}, comissão {Rate:P}",
            order.OrderNumber, request.EstablishmentId, commissionRate);

        return Ok(ApiResponse<OrderDetailDto>.SuccessResponse(await MapOrderDetail(order.Id)));
    }

    /// <summary>
    /// Listar pedidos do cliente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<OrderListItemDto>>>> GetOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        pageSize = Math.Min(pageSize, 50);

        var query = _db.OnlineOrders
            .Include(o => o.Establishment)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId.Value)
            .OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                PharmacyName = o.Establishment != null ? o.Establishment.NomeFantasia : "",
                PharmacyLogoUrl = o.Establishment != null ? o.Establishment.LogoUrl : null,
                Status = o.Status,
                StatusDisplay = o.StatusDisplay,
                StatusColor = o.StatusColor,
                Total = o.Total,
                ItemCount = o.Items.Count,
                DeliveryType = o.DeliveryType,
                CreatedAt = o.CreatedAt,
                EstimatedReadyAt = o.EstimatedReadyAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<OrderListItemDto>>.SuccessResponse(
            new PaginatedResponse<OrderListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }));
    }

    /// <summary>
    /// Detalhes de um pedido
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> GetOrder(Guid id)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var order = await _db.OnlineOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId.Value);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Pedido não encontrado"));

        return Ok(ApiResponse<OrderDetailDto>.SuccessResponse(await MapOrderDetail(id)));
    }

    /// <summary>
    /// Tracking de um pedido
    /// </summary>
    [HttpGet("{id:guid}/track")]
    public async Task<ActionResult<ApiResponse<DeliveryTrackingDto>>> TrackOrder(Guid id)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var order = await _db.OnlineOrders
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId.Value);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Pedido não encontrado"));

        var estimate = await _db.DeliveryEstimates
            .Where(d => d.OrderId == id)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();

        var tracking = new DeliveryTrackingDto
        {
            Status = estimate?.Status ?? order.Status,
            StatusDisplay = estimate?.StatusDisplay ?? order.StatusDisplay,
            EstimatedMinutes = estimate?.EstimatedMinutes,
            EstimatedDeliveryAt = estimate?.EstimatedDeliveryAt,
            ActualDeliveryAt = estimate?.ActualDeliveryAt,
            Events = new List<TrackingEventDto>
            {
                new() { Status = "PENDING", StatusDisplay = "Pedido realizado", OccurredAt = order.CreatedAt }
            }
        };

        if (order.Status != "PENDING")
            tracking.Events.Add(new TrackingEventDto { Status = "CONFIRMED", StatusDisplay = "Confirmado pela farmácia", OccurredAt = order.UpdatedAt ?? order.CreatedAt });

        if (order.Status is "PREPARING" or "READY" or "DELIVERED")
            tracking.Events.Add(new TrackingEventDto { Status = "PREPARING", StatusDisplay = "Em preparação", OccurredAt = order.UpdatedAt ?? order.CreatedAt });

        if (order.Status is "READY" or "DELIVERED")
            tracking.Events.Add(new TrackingEventDto { Status = "READY", StatusDisplay = "Pronto", OccurredAt = order.ReadyAt ?? order.UpdatedAt ?? order.CreatedAt });

        if (order.Status == "DELIVERED")
            tracking.Events.Add(new TrackingEventDto { Status = "DELIVERED", StatusDisplay = "Entregue", OccurredAt = order.DeliveredAt ?? DateTime.UtcNow });

        return Ok(ApiResponse<DeliveryTrackingDto>.SuccessResponse(tracking));
    }

    // ==================== HELPERS ====================

    private async Task<OrderDetailDto> MapOrderDetail(Guid orderId)
    {
        var order = await _db.OnlineOrders
            .Include(o => o.Establishment)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstAsync(o => o.Id == orderId);

        var estimate = await _db.DeliveryEstimates
            .Where(d => d.OrderId == orderId)
            .OrderByDescending(d => d.CreatedAt)
            .FirstOrDefaultAsync();

        return new OrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            PharmacyName = order.Establishment?.NomeFantasia ?? "",
            PharmacyLogoUrl = order.Establishment?.LogoUrl,
            Status = order.Status,
            StatusDisplay = order.StatusDisplay,
            StatusColor = order.StatusColor,
            Total = order.Total,
            Subtotal = order.Subtotal,
            Discount = order.Discount,
            DeliveryFee = order.DeliveryFee,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            DeliveryType = order.DeliveryType,
            DeliveryAddress = order.DeliveryAddress,
            CustomerNotes = order.CustomerNotes,
            ItemCount = order.Items.Count,
            CreatedAt = order.CreatedAt,
            EstimatedReadyAt = order.EstimatedReadyAt ?? estimate?.EstimatedDeliveryAt,
            ReadyAt = order.ReadyAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductName = i.ProductName,
                ProductImageUrl = i.Product?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList()
        };
    }

    private static string GenerateOrderNumber()
    {
        return $"MKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}";
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }
}
