using System.ComponentModel.DataAnnotations;

namespace DTOs.Mobile;

// ==================== PHARMACY ====================

public class PharmacyListItemDto
{
    public Guid Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? MarketplaceDescription { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int AverageDeliveryMinutes { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryRadiusKm { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? DistanceKm { get; set; }
    public string? City { get; set; }
    public string? Neighborhood { get; set; }
    public bool AcceptingOrders { get; set; }
    public bool IsOpen { get; set; }
    public int ProductCount { get; set; }
}

public class PharmacyDetailDto
{
    public Guid Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string? RazaoSocial { get; set; }
    public string? LogoUrl { get; set; }
    public string? BannerUrl { get; set; }
    public string? MarketplaceDescription { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int AverageDeliveryMinutes { get; set; }
    public decimal MinOrderAmount { get; set; }
    public decimal DeliveryRadiusKm { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool AcceptingOrders { get; set; }
    public string? MarketplaceOpeningHours { get; set; }
    public List<ProductListItemDto> FeaturedProducts { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}

public class NearbySearchRequest
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double RadiusKm { get; set; } = 10;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } // distance, rating, delivery_time
}

// ==================== PRODUCT ====================

public class ProductListItemDto
{
    public Guid Id { get; set; }
    public Guid EstablishmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public decimal Price { get; set; }
    public decimal? PromotionalPrice { get; set; }
    public bool IsOnPromotion { get; set; }
    public int DiscountPercent { get; set; }
    public string? ImageUrl { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalSold { get; set; }
    public string? CategoryName { get; set; }
    public bool InStock { get; set; }
}

public class ProductDetailDto : ProductListItemDto
{
    public string? Description { get; set; }
    public string? Composition { get; set; }
    public string? Dosage { get; set; }
    public string Unit { get; set; } = "UN";
    public string PharmacyName { get; set; } = string.Empty;
    public string? PharmacyLogoUrl { get; set; }
    public decimal PharmacyRating { get; set; }
    public List<RatingDto> RecentRatings { get; set; } = new();
}

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}

// ==================== SEARCH ====================

public class SearchRequest
{
    [MaxLength(200)]
    public string? Query { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public double? RadiusKm { get; set; }
    public Guid? CategoryId { get; set; }
    public string? SortBy { get; set; } // relevance, price_asc, price_desc, rating, distance
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class SearchResultDto
{
    public List<ProductListItemDto> Products { get; set; } = new();
    public List<PharmacyListItemDto> Pharmacies { get; set; } = new();
    public int TotalProducts { get; set; }
    public int TotalPharmacies { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// ==================== RATING ====================

public class RatingDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public string? PharmacyResponse { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRatingRequest
{
    public Guid? EstablishmentId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? OrderId { get; set; }
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }
}
