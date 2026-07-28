using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models.Marketplace;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/search")]
public class MobileSearchController : ControllerBase
{
    private readonly AppDbContext _db;

    public MobileSearchController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Busca unificada de produtos e farmácias
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<SearchResultDto>>> Search([FromQuery] SearchRequest request)
    {
        var query = request.Query?.Trim().ToLower() ?? "";
        var customerId = GetCustomerId();

        // Registrar busca para analytics
        if (!string.IsNullOrEmpty(query))
        {
            _db.SearchHistories.Add(new SearchHistory
            {
                CustomerId = customerId,
                SearchTerm = query,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                SearchType = "PRODUCT"
            });
            // Fire and forget - não bloqueia a resposta
            _ = _db.SaveChangesAsync();
        }

        // Buscar produtos
        var productsQuery = _db.CatalogProducts
            .Include(p => p.Category)
            .Include(p => p.Establishment)
            .Where(p => p.IsActive && p.IsMarketplaceVisible
                        && p.Establishment != null && p.Establishment.IsMarketplaceActive);

        if (!string.IsNullOrEmpty(query))
        {
            productsQuery = productsQuery.Where(p =>
                p.Name.ToLower().Contains(query)
                || (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(query))
                || (p.SearchKeywords != null && p.SearchKeywords.ToLower().Contains(query))
                || (p.Composition != null && p.Composition.ToLower().Contains(query)));
        }

        if (request.CategoryId.HasValue)
            productsQuery = productsQuery.Where(p => p.CategoryId == request.CategoryId.Value);

        if (request.MinPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            productsQuery = productsQuery.Where(p => p.Price <= request.MaxPrice.Value);

        // Ordenação
        productsQuery = request.SortBy switch
        {
            "price_asc" => productsQuery.OrderBy(p => p.Price),
            "price_desc" => productsQuery.OrderByDescending(p => p.Price),
            "rating" => productsQuery.OrderByDescending(p => p.AverageRating),
            _ => productsQuery.OrderByDescending(p => p.TotalSold) // relevance = mais vendidos
        };

        var totalProducts = await productsQuery.CountAsync();
        var products = await productsQuery
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductListItemDto
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
                CategoryName = p.Category != null ? p.Category.Name : null,
                InStock = p.StockQuantity > 0
            })
            .ToListAsync();

        // Buscar farmácias (apenas se busca por nome)
        var pharmacies = new List<PharmacyListItemDto>();
        var totalPharmacies = 0;

        if (!string.IsNullOrEmpty(query))
        {
            var pharmacyQuery = _db.Establishments
                .Where(e => e.IsActive && e.IsMarketplaceActive
                            && (e.NomeFantasia.ToLower().Contains(query)
                                || (e.MarketplaceDescription != null && e.MarketplaceDescription.ToLower().Contains(query))
                                || (e.Neighborhood != null && e.Neighborhood.ToLower().Contains(query))
                                || (e.City != null && e.City.ToLower().Contains(query))));

            totalPharmacies = await pharmacyQuery.CountAsync();
            pharmacies = await pharmacyQuery
                .Take(10)
                .Select(e => new PharmacyListItemDto
                {
                    Id = e.Id,
                    NomeFantasia = e.NomeFantasia,
                    LogoUrl = e.LogoUrl,
                    MarketplaceDescription = e.MarketplaceDescription,
                    AverageRating = e.AverageRating,
                    TotalRatings = e.TotalRatings,
                    AverageDeliveryMinutes = e.AverageDeliveryMinutes,
                    MinOrderAmount = e.MinOrderAmount,
                    City = e.City,
                    Neighborhood = e.Neighborhood,
                    AcceptingOrders = e.AcceptingOrders,
                    IsOpen = e.AcceptingOrders
                })
                .ToListAsync();
        }

        // Atualizar result count na search history
        var lastSearch = await _db.SearchHistories
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(s => s.SearchTerm == query && s.CustomerId == customerId);
        if (lastSearch != null)
        {
            lastSearch.ResultCount = totalProducts + totalPharmacies;
            await _db.SaveChangesAsync();
        }

        return Ok(ApiResponse<SearchResultDto>.SuccessResponse(new SearchResultDto
        {
            Products = products,
            Pharmacies = pharmacies,
            TotalProducts = totalProducts,
            TotalPharmacies = totalPharmacies,
            Page = request.Page,
            PageSize = request.PageSize
        }));
    }

    /// <summary>
    /// Categorias disponíveis no marketplace
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var categories = await _db.CatalogCategories
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = _db.CatalogProducts.Count(p =>
                    p.CategoryId == c.Id && p.IsActive && p.IsMarketplaceVisible)
            })
            .Where(c => c.ProductCount > 0)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }
}
