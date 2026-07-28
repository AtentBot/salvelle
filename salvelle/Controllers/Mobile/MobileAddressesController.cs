using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Mobile;
using Models.Marketplace;

namespace Controllers.Mobile;

[ApiController]
[Route("api/mobile/v1/addresses")]
public class MobileAddressesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MobileAddressesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Listar endereços do cliente
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<AddressDto>>>> GetAddresses()
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var addresses = await _db.CustomerAddresses
            .Where(a => a.CustomerId == customerId.Value)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                Label = a.Label,
                Street = a.Street,
                Number = a.Number,
                Complement = a.Complement,
                Neighborhood = a.Neighborhood,
                City = a.City,
                State = a.State,
                ZipCode = a.ZipCode,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                IsDefault = a.IsDefault
            })
            .ToListAsync();

        return Ok(ApiResponse<List<AddressDto>>.SuccessResponse(addresses));
    }

    /// <summary>
    /// Adicionar endereço
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<AddressDto>>> CreateAddress([FromBody] CreateAddressRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        // Se é default, desmarcar os outros
        if (request.IsDefault)
        {
            var others = await _db.CustomerAddresses
                .Where(a => a.CustomerId == customerId.Value && a.IsDefault)
                .ToListAsync();
            foreach (var a in others) a.IsDefault = false;
        }

        var address = new CustomerAddress
        {
            CustomerId = customerId.Value,
            Label = request.Label,
            Street = request.Street,
            Number = request.Number,
            Complement = request.Complement,
            Neighborhood = request.Neighborhood,
            City = request.City,
            State = request.State,
            ZipCode = request.ZipCode,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsDefault = request.IsDefault
        };

        _db.CustomerAddresses.Add(address);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AddressDto>.SuccessResponse(new AddressDto
        {
            Id = address.Id,
            Label = address.Label,
            Street = address.Street,
            Number = address.Number,
            Complement = address.Complement,
            Neighborhood = address.Neighborhood,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            IsDefault = address.IsDefault
        }));
    }

    /// <summary>
    /// Atualizar endereço
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AddressDto>>> UpdateAddress(Guid id, [FromBody] CreateAddressRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var address = await _db.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId.Value);

        if (address == null)
            return NotFound(ApiResponse.ErrorResponse("Endereço não encontrado"));

        if (request.IsDefault && !address.IsDefault)
        {
            var others = await _db.CustomerAddresses
                .Where(a => a.CustomerId == customerId.Value && a.IsDefault && a.Id != id)
                .ToListAsync();
            foreach (var a in others) a.IsDefault = false;
        }

        address.Label = request.Label;
        address.Street = request.Street;
        address.Number = request.Number;
        address.Complement = request.Complement;
        address.Neighborhood = request.Neighborhood;
        address.City = request.City;
        address.State = request.State;
        address.ZipCode = request.ZipCode;
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;
        address.IsDefault = request.IsDefault;
        address.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(ApiResponse<AddressDto>.SuccessResponse(new AddressDto
        {
            Id = address.Id,
            Label = address.Label,
            Street = address.Street,
            Number = address.Number,
            Complement = address.Complement,
            Neighborhood = address.Neighborhood,
            City = address.City,
            State = address.State,
            ZipCode = address.ZipCode,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            IsDefault = address.IsDefault
        }));
    }

    /// <summary>
    /// Remover endereço
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteAddress(Guid id)
    {
        var customerId = GetCustomerId();
        if (customerId == null) return Unauthorized(ApiResponse.ErrorResponse("Não autenticado"));

        var address = await _db.CustomerAddresses
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == customerId.Value);

        if (address == null)
            return NotFound(ApiResponse.ErrorResponse("Endereço não encontrado"));

        _db.CustomerAddresses.Remove(address);
        await _db.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Endereço removido"));
    }

    private Guid? GetCustomerId()
    {
        if (HttpContext.Items.TryGetValue("MobileCustomerId", out var id) && id is Guid customerId)
            return customerId;
        return null;
    }
}
