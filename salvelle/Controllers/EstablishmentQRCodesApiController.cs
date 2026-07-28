using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;
using QRCoder;

namespace Controllers.Api;

[ApiController]
[Route("api/establishment-qrcodes")]
public class EstablishmentQRCodesApiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<EstablishmentQRCodesApiController> _logger;
    private readonly IConfiguration _configuration;

    public EstablishmentQRCodesApiController(
        AppDbContext context,
        ILogger<EstablishmentQRCodesApiController> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    // ============================================================
    // MÉTODOS AUXILIARES - Usando nomes corretos do middleware
    // ============================================================

    private Guid GetEmployeeId()
    {
        if (HttpContext.Items["EmployeeId"] is Guid employeeId)
            return employeeId;
        return Guid.Empty;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items["EstablishmentId"] is Guid establishmentId)
            return establishmentId;
        return Guid.Empty;
    }

    // ============================================================
    // ENDPOINTS
    // ============================================================

    // GET: api/establishment-qrcodes
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcodes = await _context.Set<EstablishmentQRCode>()
            .Where(q => q.EstablishmentId == establishmentId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new
            {
                q.Id,
                q.Code,
                q.Name,
                q.Description,
                q.IsActive,
                q.ScanCount,
                q.LastScannedAt,
                q.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, qrcodes });
    }

    // GET: api/establishment-qrcodes/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .Where(q => q.Id == id && q.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code não encontrado" });

        return Ok(new { success = true, qrcode });
    }

    // POST: api/establishment-qrcodes
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateQRCodeDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        // Gerar código único
        var code = await GenerateUniqueCode();

        var qrcode = new EstablishmentQRCode
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = code,
            Name = dto.Name ?? $"QR Code {code}",
            Description = dto.Description,
            IsActive = true,
            ScanCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId != Guid.Empty ? employeeId : null
        };

        _context.Set<EstablishmentQRCode>().Add(qrcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("QR Code {Code} criado para estabelecimento {EstablishmentId}",
            code, establishmentId);

        return Ok(new
        {
            success = true,
            message = "QR Code criado com sucesso",
            qrcode = new
            {
                qrcode.Id,
                qrcode.Code,
                qrcode.Name,
                qrcode.Description
            }
        });
    }

    // PUT: api/establishment-qrcodes/{id}
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQRCodeDto dto)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code não encontrado" });

        if (!string.IsNullOrEmpty(dto.Name))
            qrcode.Name = dto.Name;

        if (dto.Description != null)
            qrcode.Description = dto.Description;

        if (dto.IsActive.HasValue)
            qrcode.IsActive = dto.IsActive.Value;

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "QR Code atualizado" });
    }

    // DELETE: api/establishment-qrcodes/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code não encontrado" });

        _context.Set<EstablishmentQRCode>().Remove(qrcode);
        await _context.SaveChangesAsync();

        _logger.LogInformation("QR Code {Code} removido", qrcode.Code);

        return Ok(new { success = true, message = "QR Code removido" });
    }

    // GET: api/establishment-qrcodes/{id}/image
    [HttpGet("{id:guid}/image")]
    public async Task<IActionResult> GetQRCodeImage(Guid id, [FromQuery] int size = 300)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .Include(q => q.Establishment)
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code não encontrado" });

        // Gerar URL do portal
        var baseUrl = _configuration["AppSettings:PortalUrl"] ?? "https://app.salvelle.com";
        var qrUrl = $"{baseUrl}/c/{qrcode.Code}";

        // Gerar imagem QR Code
        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCodeImage = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCodeImage.GetGraphic(size / 25);

        return File(qrCodeBytes, "image/png", $"qrcode-{qrcode.Code}.png");
    }

    // GET: api/establishment-qrcodes/{id}/image-base64
    [HttpGet("{id:guid}/image-base64")]
    public async Task<IActionResult> GetQRCodeImageBase64(Guid id, [FromQuery] int size = 300)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code não encontrado" });

        var baseUrl = _configuration["AppSettings:PortalUrl"] ?? "https://app.salvelle.com";
        var qrUrl = $"{baseUrl}/c/{qrcode.Code}";

        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCodeImage = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCodeImage.GetGraphic(size / 25);

        var base64 = Convert.ToBase64String(qrCodeBytes);

        return Ok(new
        {
            success = true,
            imageBase64 = $"data:image/png;base64,{base64}",
            code = qrcode.Code,
            url = qrUrl
        });
    }

    // GET: api/establishment-qrcodes/by-code/{code} - Público para o portal
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetByCode(string code)
    {
        var qrcode = await _context.Set<EstablishmentQRCode>()
            .Include(q => q.Establishment)
            .FirstOrDefaultAsync(q => q.Code == code && q.IsActive);

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code inválido ou inativo" });

        // Incrementar contador de scans
        qrcode.ScanCount++;
        qrcode.LastScannedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            establishment = new
            {
                qrcode.Establishment!.Id,
                qrcode.Establishment.NomeFantasia,
                qrcode.Establishment.RazaoSocial,
                qrcode.Establishment.Phone,
                qrcode.Establishment.Email,
                Address = $"{qrcode.Establishment.Street}, {qrcode.Establishment.Number} - {qrcode.Establishment.Neighborhood}, {qrcode.Establishment.City} - {qrcode.Establishment.State}"
            }
        });
    }

    // GET: api/establishment-qrcodes/{id}/print
    [HttpGet("{id:guid}/print")]
    public async Task<IActionResult> GetPrintableQRCode(Guid id)
    {
        var establishmentId = GetEstablishmentId();
        if (establishmentId == Guid.Empty)
            return Unauthorized(new { success = false, message = "Não autenticado" });

        var qrcode = await _context.Set<EstablishmentQRCode>()
            .Include(q => q.Establishment)
            .FirstOrDefaultAsync(q => q.Id == id && q.EstablishmentId == establishmentId);

        if (qrcode == null)
            return NotFound(new { success = false, message = "QR Code não encontrado" });

        var baseUrl = _configuration["AppSettings:PortalUrl"] ?? "https://app.salvelle.com";
        var qrUrl = $"{baseUrl}/c/{qrcode.Code}";

        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(qrUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCodeImage = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCodeImage.GetGraphic(20);

        var base64 = Convert.ToBase64String(qrCodeBytes);

        return Ok(new
        {
            success = true,
            qrcode = new
            {
                qrcode.Code,
                qrcode.Name,
                qrcode.Description
            },
            establishment = new
            {
                qrcode.Establishment!.NomeFantasia,
                qrcode.Establishment.Phone
            },
            imageBase64 = $"data:image/png;base64,{base64}",
            url = qrUrl
        });
    }

    // Helpers
    private async Task<string> GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        string code;

        do
        {
            code = new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        while (await _context.Set<EstablishmentQRCode>().AnyAsync(q => q.Code == code));

        return code;
    }
}

// DTOs
public class CreateQRCodeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UpdateQRCodeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}