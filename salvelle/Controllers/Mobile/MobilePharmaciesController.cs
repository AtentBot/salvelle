using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/pharmacies")]
public class MobilePharmaciesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MobilePharmaciesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Buscar farmácias próximas por geolocalização
    /// </summary>
    [HttpGet("nearby")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PharmacyListItemDto>>>> GetNearby(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radius = 10,
        [FromQuery] string? sortBy = "distance",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 50);

        // Buscar do banco sem cálculo de distância (CalculateDistance não é traduzível para SQL)
        var establishments = await _db.Establishments
            .Where(e => e.IsActive && e.IsMarketplaceActive && e.Latitude != null && e.Longitude != null)
            .Select(e => new
            {
                Establishment = e,
                ProductCount = _db.CatalogProducts.Count(p =>
                    p.EstablishmentId == e.Id && p.IsActive && p.IsMarketplaceVisible)
            })
            .ToListAsync();

        // Filtrar e calcular distância em memória
        var pharmacies = establishments
            .Select(e => new
            {
                e.Establishment,
                Distance = CalculateDistance(lat, lng, e.Establishment.Latitude!.Value, e.Establishment.Longitude!.Value),
                e.ProductCount
            })
            .Where(x => x.Distance <= radius)
            .ToList();

        var sorted = sortBy switch
        {
            "rating" => pharmacies.OrderByDescending(x => x.Establishment.AverageRating),
            "delivery_time" => pharmacies.OrderBy(x => x.Establishment.AverageDeliveryMinutes),
            _ => pharmacies.OrderBy(x => x.Distance)
        };

        var total = sorted.Count();
        var items = sorted
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PharmacyListItemDto
            {
                Id = x.Establishment.Id,
                NomeFantasia = x.Establishment.NomeFantasia,
                LogoUrl = x.Establishment.LogoUrl,
                MarketplaceDescription = x.Establishment.MarketplaceDescription,
                AverageRating = x.Establishment.AverageRating,
                TotalRatings = x.Establishment.TotalRatings,
                AverageDeliveryMinutes = x.Establishment.AverageDeliveryMinutes,
                MinOrderAmount = x.Establishment.MinOrderAmount,
                DeliveryRadiusKm = x.Establishment.DeliveryRadiusKm,
                Latitude = x.Establishment.Latitude,
                Longitude = x.Establishment.Longitude,
                DistanceKm = Math.Round(x.Distance, 1),
                City = x.Establishment.City,
                Neighborhood = x.Establishment.Neighborhood,
                AcceptingOrders = x.Establishment.AcceptingOrders,
                IsOpen = x.Establishment.AcceptingOrders, // TODO: verificar horário
                ProductCount = x.ProductCount
            })
            .ToList();

        var result = new PaginatedResponse<PharmacyListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResponse<PharmacyListItemDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Detalhes de uma farmácia com produtos em destaque
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PharmacyDetailDto>>> GetDetail(Guid id)
    {
        var pharmacy = await _db.Establishments
            .FirstOrDefaultAsync(e => e.Id == id && e.IsActive && e.IsMarketplaceActive);

        if (pharmacy == null)
            return NotFound(ApiResponse.ErrorResponse("Farmácia não encontrada"));

        var categories = await _db.CatalogCategories
            .Where(c => c.EstablishmentId == id)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = _db.CatalogProducts.Count(p =>
                    p.CategoryId == c.Id && p.IsActive && p.IsMarketplaceVisible)
            })
            .Where(c => c.ProductCount > 0)
            .ToListAsync();

        var featuredProducts = await _db.CatalogProducts
            .Include(p => p.Category)
            .Where(p => p.EstablishmentId == id && p.IsActive && p.IsMarketplaceVisible)
            .OrderByDescending(p => p.IsHighlight)
            .ThenByDescending(p => p.TotalSold)
            .Take(10)
            .Select(p => MapProduct(p))
            .ToListAsync();

        var detail = new PharmacyDetailDto
        {
            Id = pharmacy.Id,
            NomeFantasia = pharmacy.NomeFantasia,
            RazaoSocial = pharmacy.RazaoSocial,
            LogoUrl = pharmacy.LogoUrl,
            BannerUrl = pharmacy.BannerUrl,
            MarketplaceDescription = pharmacy.MarketplaceDescription,
            AverageRating = pharmacy.AverageRating,
            TotalRatings = pharmacy.TotalRatings,
            AverageDeliveryMinutes = pharmacy.AverageDeliveryMinutes,
            MinOrderAmount = pharmacy.MinOrderAmount,
            DeliveryRadiusKm = pharmacy.DeliveryRadiusKm,
            Latitude = pharmacy.Latitude,
            Longitude = pharmacy.Longitude,
            Street = pharmacy.Street,
            Number = pharmacy.Number,
            Neighborhood = pharmacy.Neighborhood,
            City = pharmacy.City,
            State = pharmacy.State,
            AcceptingOrders = pharmacy.AcceptingOrders,
            MarketplaceOpeningHours = pharmacy.MarketplaceOpeningHours,
            Categories = categories,
            FeaturedProducts = featuredProducts
        };

        return Ok(ApiResponse<PharmacyDetailDto>.SuccessResponse(detail));
    }

    /// <summary>
    /// Listar produtos de uma farmácia com paginação
    /// </summary>
    [HttpGet("{id:guid}/products")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<ProductListItemDto>>>> GetProducts(
        Guid id,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? sortBy = "name",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 50);

        var query = _db.CatalogProducts
            .Include(p => p.Category)
            .Where(p => p.EstablishmentId == id && p.IsActive && p.IsMarketplaceVisible);

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        query = sortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "rating" => query.OrderByDescending(p => p.AverageRating),
            "best_seller" => query.OrderByDescending(p => p.TotalSold),
            _ => query.OrderBy(p => p.Name)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapProduct(p))
            .ToListAsync();

        var result = new PaginatedResponse<ProductListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(ApiResponse<PaginatedResponse<ProductListItemDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Avaliações de uma farmácia
    /// </summary>
    [HttpGet("{id:guid}/ratings")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<RatingDto>>>> GetRatings(
        Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        pageSize = Math.Min(pageSize, 50);

        var query = _db.PharmacyRatings
            .Include(r => r.Customer)
            .Where(r => r.EstablishmentId == id)
            .OrderByDescending(r => r.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RatingDto
            {
                Id = r.Id,
                CustomerName = r.Customer != null ? r.Customer.FullName : "Cliente",
                Rating = r.Rating,
                Comment = r.Comment,
                PharmacyResponse = r.PharmacyResponse,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<RatingDto>>.SuccessResponse(
            new PaginatedResponse<RatingDto>
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            }));
    }

    // ==================== HELPERS ====================

    private static ProductListItemDto MapProduct(Models.CatalogProduct p) => new()
    {
        Id = p.Id,
        EstablishmentId = p.EstablishmentId,
        Name = p.Name,
        ShortDescription = p.ShortDescription,
        Price = p.Price,
        PromotionalPrice = p.PromotionalPrice,
        IsOnPromotion = p.IsOnPromotion,
        DiscountPercent = p.DiscountPercent,
        ImageUrl = p.ImageUrl,
        AverageRating = p.AverageRating,
        TotalRatings = p.TotalRatings,
        TotalSold = p.TotalSold,
        CategoryName = p.Category?.Name,
        InStock = p.StockQuantity > 0
    };

    /// <summary>
    /// Fórmula de Haversine para calcular distância entre dois pontos geográficos em km
    /// </summary>
    private static double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371; // Raio da Terra em km
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double deg) => deg * (Math.PI / 180);
}
