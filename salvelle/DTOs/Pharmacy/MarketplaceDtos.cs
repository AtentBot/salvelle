using System.ComponentModel.DataAnnotations;

namespace DTOs.Pharmacy.Marketplace;

// ==================== ORDERS MANAGEMENT ====================

public class PharmacyOrderListItemDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public string DeliveryType { get; set; } = string.Empty;
    public string? DeliveryAddress { get; set; }
    public string? PaymentMethod { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedReadyAt { get; set; }
    public int MinutesSinceCreated { get; set; }
}

public class PharmacyOrderDetailDto : PharmacyOrderListItemDto
{
    public string? CustomerEmail { get; set; }
    public string? CustomerNotes { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal? CommissionRate { get; set; }
    public decimal? CommissionAmount { get; set; }
    public decimal? NetAmount { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public List<PharmacyOrderItemDto> Items { get; set; } = new();
}

public class PharmacyOrderItemDto
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required]
    public string NewStatus { get; set; } = string.Empty; // CONFIRMED, PREPARING, READY, DELIVERED, CANCELLED

    public int? EstimatedMinutes { get; set; }

    [MaxLength(500)]
    public string? CancellationReason { get; set; }
}

public class OrdersFilterRequest
{
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Search { get; set; } // order number or customer name
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ==================== CATALOG MANAGEMENT ====================

public class PharmacyProductDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public string? Composition { get; set; }
    public string? Dosage { get; set; }
    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public DateTime? PromotionEndsAt { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = "UN";
    public bool IsActive { get; set; }
    public bool IsHighlight { get; set; }
    public bool IsMarketplaceVisible { get; set; }
    public string? ImageUrl { get; set; }
    public string? SearchKeywords { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalSold { get; set; }
    public decimal CurrentPrice { get; set; }
    public bool IsOnPromotion { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(300)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    public string? Composition { get; set; }

    [MaxLength(200)]
    public string? Dosage { get; set; }

    [Required, Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(0.01, 999999.99)]
    public decimal? PromotionalPrice { get; set; }

    public DateTime? PromotionEndsAt { get; set; }

    [Range(0, 999999)]
    public int StockQuantity { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "UN";

    public bool IsActive { get; set; } = true;

    public bool IsHighlight { get; set; }

    public bool IsMarketplaceVisible { get; set; } = true;

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [MaxLength(500)]
    public string? SearchKeywords { get; set; }

    public Guid? CategoryId { get; set; }
}

public class UpdateProductRequest : CreateProductRequest { }

public class ProductsFilterRequest
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsMarketplaceVisible { get; set; }
    public string? SortBy { get; set; } // name, price, stock, sold, rating
    public bool SortDesc { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class BulkUpdateProductsRequest
{
    [Required]
    public List<Guid> ProductIds { get; set; } = new();

    public bool? IsActive { get; set; }
    public bool? IsMarketplaceVisible { get; set; }
}

// ==================== FINANCIAL DASHBOARD ====================

public class PharmacyFinancialDashboardDto
{
    // Resumo do período
    public decimal GrossRevenue { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal NetRevenue { get; set; }
    public int TotalOrders { get; set; }
    public decimal CurrentCommissionRate { get; set; }
    public int WeeklySalesCount { get; set; }
    public string CommissionTier { get; set; } = string.Empty; // "7%", "5%", "3%"

    // Comparação com período anterior
    public decimal RevenueChange { get; set; } // percentual
    public int OrdersChange { get; set; }

    // Detalhamento
    public decimal AverageOrderValue { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }

    // Gráficos
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public OrderStatusBreakdownDto StatusBreakdown { get; set; } = new();
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Gross { get; set; }
    public decimal Net { get; set; }
    public int OrderCount { get; set; }
}

public class TopProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public class OrderStatusBreakdownDto
{
    public int Pending { get; set; }
    public int Confirmed { get; set; }
    public int Preparing { get; set; }
    public int Ready { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
}

public class FinancialFilterRequest
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

// ==================== MARKETPLACE SETTINGS ====================

public class MarketplaceSettingsDto
{
    public bool IsMarketplaceActive { get; set; }
    public string? MarketplaceDescription { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryRadiusKm { get; set; }
    public int AverageDeliveryMinutes { get; set; }
    public bool AcceptingOrders { get; set; }
    public string? MarketplaceOpeningHours { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class UpdateMarketplaceSettingsRequest
{
    public bool? IsMarketplaceActive { get; set; }

    [MaxLength(2000)]
    public string? MarketplaceDescription { get; set; }

    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [MaxLength(500)]
    public string? BannerUrl { get; set; }

    [Range(0, 99999.99)]
    public decimal? MinOrderAmount { get; set; }

    [Range(0.1, 100)]
    public decimal? DeliveryRadiusKm { get; set; }

    [Range(5, 600)]
    public int? AverageDeliveryMinutes { get; set; }

    public bool? AcceptingOrders { get; set; }

    [MaxLength(1000)]
    public string? MarketplaceOpeningHours { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

// ==================== RATINGS MANAGEMENT ====================

public class PharmacyRatingDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? PharmacyResponse { get; set; }
    public string? OrderNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RespondToRatingRequest
{
    [Required, MaxLength(1000)]
    public string Response { get; set; } = string.Empty;
}
