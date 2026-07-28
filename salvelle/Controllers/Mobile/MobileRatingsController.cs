using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models.Marketplace;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/ratings")]
public class MobileRatingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public MobileRatingsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Criar avaliação de farmácia ou produto
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse>> CreateRating([FromBody] CreateRatingRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(ApiResponse.ErrorResponse("Avaliação deve ser entre 1 e 5"));

        // OrderId obrigatório para evitar avaliações falsas
        if (!request.OrderId.HasValue)
            return BadRequest(ApiResponse.ErrorResponse("É necessário informar o pedido para avaliar"));

        // Verificar se o pedido existe e pertence ao cliente
        {
            var order = await _db.OnlineOrders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId.Value && o.CustomerId == customerId.Value);

            if (order == null)
                return BadRequest(ApiResponse.ErrorResponse("Pedido não encontrado"));

            if (order.Status != "DELIVERED")
                return BadRequest(ApiResponse.ErrorResponse("Só é possível avaliar pedidos entregues"));
        }

        // Avaliação de farmácia
        if (request.EstablishmentId.HasValue)
        {
            var alreadyRated = await _db.PharmacyRatings
                .AnyAsync(r => r.EstablishmentId == request.EstablishmentId.Value
                               && r.CustomerId == customerId.Value
                               && r.OrderId == request.OrderId);

            if (alreadyRated)
                return BadRequest(ApiResponse.ErrorResponse("Você já avaliou esta farmácia para este pedido"));

            _db.PharmacyRatings.Add(new PharmacyRating
            {
                EstablishmentId = request.EstablishmentId.Value,
                CustomerId = customerId.Value,
                OrderId = request.OrderId,
                Rating = request.Rating,
                Comment = request.Comment
            });

            // Atualizar média da farmácia
            var pharmacy = await _db.Establishments.FindAsync(request.EstablishmentId.Value);
            if (pharmacy != null)
            {
                var totalRatings = pharmacy.TotalRatings + 1;
                pharmacy.AverageRating = ((pharmacy.AverageRating * pharmacy.TotalRatings) + request.Rating) / totalRatings;
                pharmacy.TotalRatings = totalRatings;
            }
        }

        // Avaliação de produto
        if (request.ProductId.HasValue)
        {
            var alreadyRated = await _db.ProductRatings
                .AnyAsync(r => r.CatalogProductId == request.ProductId.Value
                               && r.CustomerId == customerId.Value
                               && r.OrderId == request.OrderId);

            if (alreadyRated)
                return BadRequest(ApiResponse.ErrorResponse("Você já avaliou este produto para este pedido"));

            _db.ProductRatings.Add(new ProductRating
            {
                CatalogProductId = request.ProductId.Value,
                CustomerId = customerId.Value,
                OrderId = request.OrderId,
                Rating = request.Rating,
                Comment = request.Comment
            });

            // Atualizar média do produto
            var product = await _db.CatalogProducts.FindAsync(request.ProductId.Value);
            if (product != null)
            {
                var totalRatings = product.TotalRatings + 1;
                product.AverageRating = ((product.AverageRating * product.TotalRatings) + request.Rating) / totalRatings;
                product.TotalRatings = totalRatings;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Avaliação registrada com sucesso"));
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }
}
