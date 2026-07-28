using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Pharmacy.Marketplace;
using DTOs.Mobile;
using Models;

namespace Controllers.Pharmacy;

/// <summary>
/// Gestão de pedidos marketplace recebidos pela farmácia
/// Autenticação via EmployeeAuthMiddleware (session-based)
/// </summary>
[ApiController]
[Route("api/pharmacy/marketplace/orders")]
public class PharmacyOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<PharmacyOrdersController> _logger;

    public PharmacyOrdersController(AppDbContext db, ILogger<PharmacyOrdersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Listar pedidos recebidos pela farmácia
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PharmacyOrderListItemDto>>>> GetOrders(
        [FromQuery] OrdersFilterRequest filter)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var pageSize = Math.Min(filter.PageSize, 50);

        var query = _db.OnlineOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.EstablishmentId == establishmentId.Value)
            .AsQueryable();

        // Filtros
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(o => o.Status == filter.Status.ToUpper());

        if (filter.From.HasValue)
            query = query.Where(o => o.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(o => o.CreatedAt <= filter.To.Value);

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(search) ||
                (o.Customer != null && o.Customer.FullName.ToLower().Contains(search)));
        }

        query = query.OrderByDescending(o => o.CreatedAt);

        var total = await query.CountAsync();
        var orders = await query
            .Skip((filter.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = orders.Select(o => new PharmacyOrderListItemDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerName = o.Customer?.FullName ?? "Cliente",
            CustomerPhone = o.Customer?.Phone,
            Status = o.Status,
            StatusDisplay = o.StatusDisplay,
            StatusColor = o.StatusColor,
            Total = o.Total,
            ItemCount = o.Items.Count,
            DeliveryType = o.DeliveryType,
            DeliveryAddress = o.DeliveryAddress,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            CreatedAt = o.CreatedAt,
            EstimatedReadyAt = o.EstimatedReadyAt,
            MinutesSinceCreated = (int)(DateTime.UtcNow - o.CreatedAt).TotalMinutes
        }).ToList();

        return Ok(ApiResponse<PaginatedResponse<PharmacyOrderListItemDto>>.SuccessResponse(
            new PaginatedResponse<PharmacyOrderListItemDto>
            {
                Items = items,
                TotalCount = total,
                Page = filter.Page,
                PageSize = pageSize
            }));
    }

    /// <summary>
    /// Contadores de pedidos por status (para dashboard)
    /// </summary>
    [HttpGet("counts")]
    public async Task<ActionResult<ApiResponse<OrderStatusBreakdownDto>>> GetOrderCounts()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var today = DateTime.UtcNow.Date;
        var orders = await _db.OnlineOrders
            .Where(o => o.EstablishmentId == establishmentId.Value && o.CreatedAt >= today)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var breakdown = new OrderStatusBreakdownDto
        {
            Pending = orders.FirstOrDefault(o => o.Status == "PENDING")?.Count ?? 0,
            Confirmed = orders.FirstOrDefault(o => o.Status == "CONFIRMED")?.Count ?? 0,
            Preparing = orders.FirstOrDefault(o => o.Status == "PREPARING")?.Count ?? 0,
            Ready = orders.FirstOrDefault(o => o.Status == "READY")?.Count ?? 0,
            Delivered = orders.FirstOrDefault(o => o.Status == "DELIVERED")?.Count ?? 0,
            Cancelled = orders.FirstOrDefault(o => o.Status == "CANCELLED")?.Count ?? 0
        };

        return Ok(ApiResponse<OrderStatusBreakdownDto>.SuccessResponse(breakdown));
    }

    /// <summary>
    /// Detalhes de um pedido
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PharmacyOrderDetailDto>>> GetOrder(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var order = await _db.OnlineOrders
            .Include(o => o.Customer)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId.Value);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Pedido não encontrado"));

        var detail = new PharmacyOrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.Customer?.FullName ?? "Cliente",
            CustomerPhone = order.Customer?.Phone,
            CustomerEmail = order.Customer?.Email,
            CustomerNotes = order.CustomerNotes,
            Status = order.Status,
            StatusDisplay = order.StatusDisplay,
            StatusColor = order.StatusColor,
            Total = order.Total,
            Subtotal = order.Subtotal,
            Discount = order.Discount,
            DeliveryFee = order.DeliveryFee,
            ItemCount = order.Items.Count,
            DeliveryType = order.DeliveryType,
            DeliveryAddress = order.DeliveryAddress,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = order.PaymentStatus,
            CommissionRate = order.PlatformCommissionRate,
            CommissionAmount = order.PlatformCommissionAmount,
            NetAmount = order.NetAmountToPharmacy,
            CreatedAt = order.CreatedAt,
            EstimatedReadyAt = order.EstimatedReadyAt,
            ReadyAt = order.ReadyAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            MinutesSinceCreated = (int)(DateTime.UtcNow - order.CreatedAt).TotalMinutes,
            Items = order.Items.Select(i => new PharmacyOrderItemDto
            {
                Id = i.Id,
                ProductName = i.ProductName,
                ProductImageUrl = i.Product?.ImageUrl,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Notes = i.Notes
            }).ToList()
        };

        return Ok(ApiResponse<PharmacyOrderDetailDto>.SuccessResponse(detail));
    }

    /// <summary>
    /// Atualizar status do pedido (aceitar, preparar, pronto, entregar, cancelar)
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<PharmacyOrderDetailDto>>> UpdateOrderStatus(
        Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var order = await _db.OnlineOrders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && o.EstablishmentId == establishmentId.Value);

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Pedido não encontrado"));

        var newStatus = request.NewStatus.ToUpper();

        // Validar transição de status
        var validTransition = IsValidStatusTransition(order.Status, newStatus);
        if (!validTransition)
            return BadRequest(ApiResponse.ErrorResponse(
                $"Transição inválida: {order.Status} → {newStatus}"));

        var previousStatus = order.Status;
        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;

        switch (newStatus)
        {
            case "CONFIRMED":
                if (request.EstimatedMinutes.HasValue)
                {
                    order.EstimatedReadyAt = DateTime.UtcNow.AddMinutes(request.EstimatedMinutes.Value);

                    // Atualizar estimativa de entrega
                    var estimate = await _db.DeliveryEstimates
                        .Where(d => d.OrderId == id)
                        .OrderByDescending(d => d.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (estimate != null)
                    {
                        estimate.EstimatedMinutes = request.EstimatedMinutes.Value;
                        estimate.EstimatedDeliveryAt = order.EstimatedReadyAt ?? DateTime.UtcNow.AddMinutes(request.EstimatedMinutes.Value);
                        estimate.Status = "CONFIRMADO";
                        estimate.UpdatedAt = DateTime.UtcNow;
                    }
                }
                break;

            case "PREPARING":
                // Atualizar estimativa
                var prepEstimate = await _db.DeliveryEstimates
                    .Where(d => d.OrderId == id)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();
                if (prepEstimate != null)
                {
                    prepEstimate.Status = "PREPARANDO";
                    prepEstimate.UpdatedAt = DateTime.UtcNow;
                }
                break;

            case "READY":
                order.ReadyAt = DateTime.UtcNow;
                var readyEstimate = await _db.DeliveryEstimates
                    .Where(d => d.OrderId == id)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();
                if (readyEstimate != null)
                {
                    readyEstimate.Status = "PRONTO";
                    readyEstimate.UpdatedAt = DateTime.UtcNow;
                }
                break;

            case "DELIVERED":
                order.DeliveredAt = DateTime.UtcNow;
                order.PaymentStatus = "PAID";
                var delivEstimate = await _db.DeliveryEstimates
                    .Where(d => d.OrderId == id)
                    .OrderByDescending(d => d.CreatedAt)
                    .FirstOrDefaultAsync();
                if (delivEstimate != null)
                {
                    delivEstimate.Status = "ENTREGUE";
                    delivEstimate.ActualDeliveryAt = DateTime.UtcNow;
                    delivEstimate.UpdatedAt = DateTime.UtcNow;
                }
                break;

            case "CANCELLED":
                order.CancelledAt = DateTime.UtcNow;
                order.PaymentStatus = "REFUNDED";

                // Devolver estoque
                foreach (var item in order.Items)
                {
                    if (item.Product != null)
                    {
                        item.Product.StockQuantity += item.Quantity;
                        item.Product.TotalSold = Math.Max(0, item.Product.TotalSold - item.Quantity);
                    }
                }

                // Marcar transação como estornada
                var transaction = await _db.PlatformTransactions
                    .FirstOrDefaultAsync(t => t.OrderId == id);
                if (transaction != null)
                    transaction.Status = "ESTORNADO";

                break;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Pedido {OrderNumber} atualizado: {From} → {To} por farmácia {PharmacyId}",
            order.OrderNumber, previousStatus, newStatus, establishmentId.Value);

        return await GetOrder(id);
    }

    // ==================== HELPERS ====================

    private static bool IsValidStatusTransition(string current, string next)
    {
        return (current, next) switch
        {
            ("PENDING", "CONFIRMED") => true,
            ("PENDING", "CANCELLED") => true,
            ("CONFIRMED", "PREPARING") => true,
            ("CONFIRMED", "CANCELLED") => true,
            ("PREPARING", "READY") => true,
            ("PREPARING", "CANCELLED") => true,
            ("READY", "DELIVERED") => true,
            _ => false
        };
    }

    private Guid? GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var id) && id is Guid estId)
            return estId;
        return null;
    }
}
