using System.ComponentModel.DataAnnotations;

namespace DTOs.Mobile;

// ==================== CART ====================

public class AddToCartRequest
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;

    public string? Notes { get; set; }
}

public class UpdateCartItemRequest
{
    [Range(1, 999)]
    public int Quantity { get; set; }
}

public class MobileCartDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public string PharmacyName { get; set; } = string.Empty;
    public string? PharmacyLogoUrl { get; set; }
    public List<MobileCartItemDto> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
}

public class MobileCartItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
    public bool InStock { get; set; }
}

// ==================== ORDER ====================

public class CreateOrderRequest
{
    [Required]
    public Guid EstablishmentId { get; set; }

    [Required]
    public string DeliveryType { get; set; } = "DELIVERY"; // DELIVERY, PICKUP

    public Guid? DeliveryAddressId { get; set; }

    [Required]
    public string PaymentMethod { get; set; } = "CREDIT_CARD";

    [MaxLength(200)]
    public string? StripePaymentMethodId { get; set; }

    [MaxLength(1000)]
    public string? CustomerNotes { get; set; }

    [MaxLength(50)]
    public string? CouponCode { get; set; }
}

public class OrderListItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string PharmacyName { get; set; } = string.Empty;
    public string? PharmacyLogoUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedReadyAt { get; set; }
}

public class OrderDetailDto : OrderListItemDto
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal DeliveryFee { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string? CustomerNotes { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DeliveryTrackingDto? Tracking { get; set; }
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class DeliveryTrackingDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public int? EstimatedMinutes { get; set; }
    public DateTime? EstimatedDeliveryAt { get; set; }
    public DateTime? ActualDeliveryAt { get; set; }
    public List<TrackingEventDto> Events { get; set; } = new();
}

public class TrackingEventDto
{
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}

// ==================== ADDRESS ====================

public class AddressDto
{
    public Guid Id { get; set; }
    public string? Label { get; set; }
    public string Street { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string Neighborhood { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

public class CreateAddressRequest
{
    public string? Label { get; set; }

    [Required, MaxLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Complement { get; set; }

    [Required, MaxLength(100)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [Required, MaxLength(8)]
    public string ZipCode { get; set; } = string.Empty;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsDefault { get; set; }
}

// ==================== PAGINATION ====================

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
