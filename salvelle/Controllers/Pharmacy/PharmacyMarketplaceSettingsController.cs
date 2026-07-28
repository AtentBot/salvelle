using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Pharmacy.Marketplace;
using Models.Marketplace;

namespace Controllers.Pharmacy;

/// <summary>
/// Configurações do marketplace da farmácia + gestão de avaliações
/// </summary>
[ApiController]
[Route("api/pharmacy/marketplace/settings")]
public class PharmacyMarketplaceSettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public PharmacyMarketplaceSettingsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Obter configurações atuais do marketplace
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<MarketplaceSettingsDto>>> GetSettings()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var pharmacy = await _db.Establishments.FindAsync(establishmentId.Value);
        if (pharmacy == null)
            return NotFound(ApiResponse.ErrorResponse("Farmácia não encontrada"));

        return Ok(ApiResponse<MarketplaceSettingsDto>.SuccessResponse(new MarketplaceSettingsDto
        {
            IsMarketplaceActive = pharmacy.IsMarketplaceActive,
            MarketplaceDescription = pharmacy.MarketplaceDescription,
            LogoUrl = pharmacy.LogoUrl,
            BannerUrl = pharmacy.BannerUrl,
            MinOrderAmount = pharmacy.MinOrderAmount,
            DeliveryRadiusKm = pharmacy.DeliveryRadiusKm,
            AverageDeliveryMinutes = pharmacy.AverageDeliveryMinutes,
            AcceptingOrders = pharmacy.AcceptingOrders,
            MarketplaceOpeningHours = pharmacy.MarketplaceOpeningHours,
            Latitude = pharmacy.Latitude,
            Longitude = pharmacy.Longitude
        }));
    }

    /// <summary>
    /// Atualizar configurações do marketplace
    /// </summary>
    [HttpPut]
    public async Task<ActionResult<ApiResponse<MarketplaceSettingsDto>>> UpdateSettings(
        [FromBody] UpdateMarketplaceSettingsRequest request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var pharmacy = await _db.Establishments.FindAsync(establishmentId.Value);
        if (pharmacy == null)
            return NotFound(ApiResponse.ErrorResponse("Farmácia não encontrada"));

        // Atualizar apenas os campos fornecidos
        if (request.IsMarketplaceActive.HasValue)
            pharmacy.IsMarketplaceActive = request.IsMarketplaceActive.Value;
        if (request.MarketplaceDescription != null)
            pharmacy.MarketplaceDescription = request.MarketplaceDescription;
        if (request.LogoUrl != null)
            pharmacy.LogoUrl = request.LogoUrl;
        if (request.BannerUrl != null)
            pharmacy.BannerUrl = request.BannerUrl;
        if (request.MinOrderAmount.HasValue)
            pharmacy.MinOrderAmount = request.MinOrderAmount.Value;
        if (request.DeliveryRadiusKm.HasValue)
            pharmacy.DeliveryRadiusKm = request.DeliveryRadiusKm.Value;
        if (request.AverageDeliveryMinutes.HasValue)
            pharmacy.AverageDeliveryMinutes = request.AverageDeliveryMinutes.Value;
        if (request.AcceptingOrders.HasValue)
            pharmacy.AcceptingOrders = request.AcceptingOrders.Value;
        if (request.MarketplaceOpeningHours != null)
            pharmacy.MarketplaceOpeningHours = request.MarketplaceOpeningHours;
        if (request.Latitude.HasValue)
            pharmacy.Latitude = request.Latitude.Value;
        if (request.Longitude.HasValue)
            pharmacy.Longitude = request.Longitude.Value;

        await _db.SaveChangesAsync();

        return await GetSettings();
    }

    /// <summary>
    /// Toggle rápido: aceitar/pausar pedidos
    /// </summary>
    [HttpPost("toggle-orders")]
    public async Task<ActionResult<ApiResponse>> ToggleAcceptingOrders()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var pharmacy = await _db.Establishments.FindAsync(establishmentId.Value);
        if (pharmacy == null)
            return NotFound(ApiResponse.ErrorResponse("Farmácia não encontrada"));

        pharmacy.AcceptingOrders = !pharmacy.AcceptingOrders;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            pharmacy.AcceptingOrders ? "Pedidos ativados" : "Pedidos pausados"));
    }

    // ==================== RATINGS MANAGEMENT ====================

    /// <summary>
    /// Listar avaliações recebidas pela farmácia
    /// </summary>
    [HttpGet("~/api/pharmacy/marketplace/ratings")]
    public async Task<ActionResult<ApiResponse<List<PharmacyRatingDto>>>> GetRatings(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        pageSize = Math.Min(pageSize, 50);

        var ratings = await _db.PharmacyRatings
            .Include(r => r.Customer)
            .Include(r => r.Order)
            .Where(r => r.EstablishmentId == establishmentId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new PharmacyRatingDto
            {
                Id = r.Id,
                CustomerName = r.Customer != null ? r.Customer.FullName : "Cliente",
                Rating = r.Rating,
                Comment = r.Comment,
                PharmacyResponse = r.PharmacyResponse,
                OrderNumber = r.Order != null ? r.Order.OrderNumber : null,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<PharmacyRatingDto>>.SuccessResponse(ratings));
    }

    /// <summary>
    /// Responder a uma avaliação
    /// </summary>
    [HttpPost("~/api/pharmacy/marketplace/ratings/{id:guid}/respond")]
    public async Task<ActionResult<ApiResponse>> RespondToRating(
        Guid id, [FromBody] RespondToRatingRequest request)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var rating = await _db.PharmacyRatings
            .FirstOrDefaultAsync(r => r.Id == id && r.EstablishmentId == establishmentId.Value);

        if (rating == null)
            return NotFound(ApiResponse.ErrorResponse("Avaliação não encontrada"));

        rating.PharmacyResponse = request.Response;
        rating.PharmacyRespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Resposta registrada"));
    }

    // ==================== HELPERS ====================

    private Guid? GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var id) && id is Guid estId)
            return estId;
        return null;
    }
}
