using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Pharmacy.Marketplace;
using DTOs.Mobile;
using Models;

namespace Controllers.Pharmacy;

/// <summary>
/// CRUD de catálogo de produtos para o marketplace
/// </summary>
[ApiController]
[Route("api/pharmacy/marketplace/catalog")]
public class PharmacyCatalogController : ControllerBase
{
    private readonly AppDbContext _db;

    public PharmacyCatalogController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Listar produtos do catálogo da farmácia
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PharmacyProductDto>>>> GetProducts(
        [FromQuery] ProductsFilterRequest filter)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var pageSize = Math.Min(filter.PageSize, 50);

        var query = _db.CatalogProducts
            .Include(p => p.Category)
            .Where(p => p.EstablishmentId == establishmentId.Value)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                (p.Code != null && p.Code.ToLower().Contains(search)) ||
                (p.SearchKeywords != null && p.SearchKeywords.ToLower().Contains(search)));
        }

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

        if (filter.IsActive.HasValue)
            query = query.Where(p => p.IsActive == filter.IsActive.Value);

        if (filter.IsMarketplaceVisible.HasValue)
            query = query.Where(p => p.IsMarketplaceVisible == filter.IsMarketplaceVisible.Value);

        // Ordenação
        query = filter.SortBy?.ToLower() switch
        {
            "price" => filter.SortDesc ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "stock" => filter.SortDesc ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
            "sold" => filter.SortDesc ? query.OrderByDescending(p => p.TotalSold) : query.OrderBy(p => p.TotalSold),
            "rating" => filter.SortDesc ? query.OrderByDescending(p => p.AverageRating) : query.OrderBy(p => p.AverageRating),
            _ => filter.SortDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name)
        };

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.Page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PharmacyProductDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                ShortDescription = p.ShortDescription,
                Description = p.Description,
                Composition = p.Composition,
                Dosage = p.Dosage,
                Price = p.Price,
                PromotionalPrice = p.PromotionalPrice,
                PromotionEndsAt = p.PromotionEndsAt,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit,
                IsActive = p.IsActive,
                IsHighlight = p.IsHighlight,
                IsMarketplaceVisible = p.IsMarketplaceVisible,
                ImageUrl = p.ImageUrl,
                SearchKeywords = p.SearchKeywords,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : null,
                AverageRating = p.AverageRating,
                TotalRatings = p.TotalRatings,
                TotalSold = p.TotalSold,
                CurrentPrice = p.PromotionalPrice.HasValue && p.PromotionalPrice.Value < p.Price
                    && (!p.PromotionEndsAt.HasValue || p.PromotionEndsAt.Value > DateTime.UtcNow)
                    ? p.PromotionalPrice.Value : p.Price,
                IsOnPromotion = p.PromotionalPrice.HasValue && p.PromotionalPrice.Value < p.Price
                    && (!p.PromotionEndsAt.HasValue || p.PromotionEndsAt.Value > DateTime.UtcNow),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<PaginatedResponse<PharmacyProductDto>>.SuccessResponse(
            new PaginatedResponse<PharmacyProductDto>
            {
                Items = items,
                TotalCount = total,
                Page = filter.Page,
                PageSize = pageSize
            }));
    }

    /// <summary>
    /// Detalhes de um produto
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PharmacyProductDto>>> GetProduct(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var product = await _db.CatalogProducts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId.Value);

        if (product == null)
            return NotFound(ApiResponse.ErrorResponse("Produto não encontrado"));

        return Ok(ApiResponse<PharmacyProductDto>.SuccessResponse(MapProduct(product)));
    }

    /// <summary>
    /// Criar novo produto
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<PharmacyProductDto>>> CreateProduct([FromBody] CreateProductRequest request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var product = new CatalogProduct
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId.Value,
            Code = request.Code,
            Name = request.Name,
            ShortDescription = request.ShortDescription,
            Description = request.Description,
            Composition = request.Composition,
            Dosage = request.Dosage,
            Price = request.Price,
            PromotionalPrice = request.PromotionalPrice,
            PromotionEndsAt = request.PromotionEndsAt,
            StockQuantity = request.StockQuantity,
            Unit = request.Unit,
            IsActive = request.IsActive,
            IsHighlight = request.IsHighlight,
            IsMarketplaceVisible = request.IsMarketplaceVisible,
            ImageUrl = request.ImageUrl,
            SearchKeywords = request.SearchKeywords,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        _db.CatalogProducts.Add(product);
        await _db.SaveChangesAsync();

        // Reload with category
        await _db.Entry(product).Reference(p => p.Category).LoadAsync();

        return Ok(ApiResponse<PharmacyProductDto>.SuccessResponse(MapProduct(product)));
    }

    /// <summary>
    /// Atualizar produto
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PharmacyProductDto>>> UpdateProduct(
        Guid id, [FromBody] UpdateProductRequest request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var product = await _db.CatalogProducts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId.Value);

        if (product == null)
            return NotFound(ApiResponse.ErrorResponse("Produto não encontrado"));

        product.Code = request.Code;
        product.Name = request.Name;
        product.ShortDescription = request.ShortDescription;
        product.Description = request.Description;
        product.Composition = request.Composition;
        product.Dosage = request.Dosage;
        product.Price = request.Price;
        product.PromotionalPrice = request.PromotionalPrice;
        product.PromotionEndsAt = request.PromotionEndsAt;
        product.StockQuantity = request.StockQuantity;
        product.Unit = request.Unit;
        product.IsActive = request.IsActive;
        product.IsHighlight = request.IsHighlight;
        product.IsMarketplaceVisible = request.IsMarketplaceVisible;
        product.ImageUrl = request.ImageUrl;
        product.SearchKeywords = request.SearchKeywords;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<PharmacyProductDto>.SuccessResponse(MapProduct(product)));
    }

    /// <summary>
    /// Excluir produto (soft delete — desativa)
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteProduct(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var product = await _db.CatalogProducts
            .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId.Value);

        if (product == null)
            return NotFound(ApiResponse.ErrorResponse("Produto não encontrado"));

        product.IsActive = false;
        product.IsMarketplaceVisible = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Produto removido do catálogo"));
    }

    /// <summary>
    /// Atualização em lote (ativar/desativar/visibilidade)
    /// </summary>
    [HttpPut("bulk")]
    public async Task<ActionResult<ApiResponse>> BulkUpdate([FromBody] BulkUpdateProductsRequest request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var products = await _db.CatalogProducts
            .Where(p => request.ProductIds.Contains(p.Id) && p.EstablishmentId == establishmentId.Value)
            .ToListAsync();

        foreach (var product in products)
        {
            if (request.IsActive.HasValue)
                product.IsActive = request.IsActive.Value;
            if (request.IsMarketplaceVisible.HasValue)
                product.IsMarketplaceVisible = request.IsMarketplaceVisible.Value;
            product.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse($"{products.Count} produtos atualizados"));
    }

    /// <summary>
    /// Listar categorias da farmácia
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var categories = await _db.CatalogCategories
            .Where(c => c.EstablishmentId == establishmentId.Value && c.IsActive)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ProductCount = _db.CatalogProducts.Count(p => p.CategoryId == c.Id && p.IsActive)
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
    }

    // ==================== HELPERS ====================

    private static PharmacyProductDto MapProduct(CatalogProduct p)
    {
        return new PharmacyProductDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            ShortDescription = p.ShortDescription,
            Description = p.Description,
            Composition = p.Composition,
            Dosage = p.Dosage,
            Price = p.Price,
            PromotionalPrice = p.PromotionalPrice,
            PromotionEndsAt = p.PromotionEndsAt,
            StockQuantity = p.StockQuantity,
            Unit = p.Unit,
            IsActive = p.IsActive,
            IsHighlight = p.IsHighlight,
            IsMarketplaceVisible = p.IsMarketplaceVisible,
            ImageUrl = p.ImageUrl,
            SearchKeywords = p.SearchKeywords,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name,
            AverageRating = p.AverageRating,
            TotalRatings = p.TotalRatings,
            TotalSold = p.TotalSold,
            CurrentPrice = p.CurrentPrice,
            IsOnPromotion = p.IsOnPromotion,
            CreatedAt = p.CreatedAt
        };
    }

    private Guid? GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var id) && id is Guid estId)
            return estId;
        return null;
    }
}
