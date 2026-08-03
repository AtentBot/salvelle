using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Isopoh.Cryptography.Argon2;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Json;

namespace Controllers;

[ApiController]
[Route("api/establishments")]
public class EstablishmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public EstablishmentsController(AppDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> List(
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var q = _db.Establishments.AsQueryable();
        if (!string.IsNullOrWhiteSpace(city)) q = q.Where(e => e.City == city);
        if (!string.IsNullOrWhiteSpace(state)) q = q.Where(e => e.State == state);

        var data = await q.OrderBy(e => e.NomeFantasia)
                          .Skip(Math.Max(0, skip))
                          .Take(Math.Clamp(take, 1, 200))
                          .Select(e => new
                          {
                              e.Id, e.NomeFantasia, e.RazaoSocial, e.Cnpj,
                              e.City, e.State, e.PostalCode,
                              e.Phone, e.WhatsApp, e.Email,
                              e.IsActive, e.OnboardingCompleted,
                              e.IsMarketplaceActive, e.AverageRating, e.TotalRatings,
                              e.CreatedAt
                          })
                          .ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<object>> Get(Guid id)
    {
        var e = await _db.Establishments.FindAsync(id);
        if (e is null) return NotFound();
        return Ok(new
        {
            e.Id, e.NomeFantasia, e.RazaoSocial, e.Cnpj,
            e.City, e.State, e.PostalCode, e.Street, e.Number, e.Neighborhood,
            e.Phone, e.WhatsApp, e.Email, e.Instagram, e.Facebook,
            e.IsActive, e.OnboardingCompleted,
            e.IsMarketplaceActive, e.AverageRating, e.TotalRatings,
            e.CreatedAt, e.UpdatedAt
        });
    }

    // Categoria padrão (Farmácia de Manipulação) — mesma usada por SignupService.
    private static readonly Guid DefaultCategoryId = Guid.Parse("c0000000-0000-0000-0000-000000000001");

    // 1) CADASTRO: cria establishment, gera 6 dígitos, salva client_onboarding e envia WhatsApp
    [HttpPost]
    public async Task<ActionResult<Establishment>> Create([FromBody] CreateEstablishmentRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { error = "invalid_body" });

        var cnpj = request.Cnpj?.Trim();

        // 1. Verifica se já existe CNPJ igual
        if (!string.IsNullOrWhiteSpace(cnpj))
        {
            var exists = await _db.Establishments.AnyAsync(e => e.Cnpj == cnpj, ct);

            if (exists)
                return Conflict(new
                {
                    error = "duplicate_cnpj",
                    message = "Já temos um cliente cadastrado utilizando este CNPJ. Verifique seus dados ou entre em contato com o suporte."
                });
        }

        // 2. Perfil de acesso e categoria são definidos pela PLATAFORMA, nunca pelo cliente.
        //    Antes o endpoint fazia bind da entidade Establishment inteira ([FromBody] Establishment),
        //    permitindo mass assignment de AccessLevelId (escalada de privilégio via FK de papel),
        //    CategoryId e flags de marketplace/geo. Agora aceitamos apenas uma allow-list.
        var ownerAccessLevel = await _db.Set<AccessLevel>()
            .FirstOrDefaultAsync(a => a.Code.ToLower() == "owner", ct);
        if (ownerAccessLevel == null)
            return StatusCode(500, new { error = "access_level_missing", message = "Configuração de perfil de acesso não encontrada. Contate o suporte." });

        // 3. Monta a entidade a partir dos campos seguros da requisição
        var input = new Establishment
        {
            Id = Guid.NewGuid(),
            NomeFantasia = request.NomeFantasia?.Trim() ?? string.Empty,
            RazaoSocial = request.RazaoSocial?.Trim() ?? string.Empty,
            Cnpj = cnpj,
            InscricaoEstadual = request.InscricaoEstadual?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            WhatsApp = request.WhatsApp?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Street = request.Street?.Trim(),
            Number = request.Number?.Trim(),
            Complement = request.Complement?.Trim(),
            Neighborhood = request.Neighborhood?.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            Instagram = request.Instagram?.Trim(),
            Facebook = request.Facebook?.Trim(),

            // Campos controlados pela plataforma
            AccessLevelId = ownerAccessLevel.Id,
            CategoryId = DefaultCategoryId,
            OnboardingCompleted = false,
            IsActive = true,
            SubscriptionStatus = null,
            TrialEndsAt = null,
            MaxEmployeesLimit = null,
            MaxOrdersLimit = null,
            FeaturesEnabled = null,
            IsMarketplaceActive = false,
            AverageRating = 0,
            TotalRatings = 0,
            StripeConnectAccountId = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            PasswordCreatedAt = DateTime.UtcNow,
            PasswordLastRehash = null
        };

        // 4. Senha em texto puro → gera hash antes de persistir
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            input.PasswordHash = Argon2.Hash(request.Password);
            input.PasswordAlgorithm = "argon2id-v1";
        }

        // 5. Persistência
        _db.Establishments.Add(input);
        await _db.SaveChangesAsync(ct);

        // Gera 6 d�gitos (100000..999999)
        var six = RandomNumberGenerator.GetInt32(100000, 1000000);

        // CORRE��O: Usar o input que acabamos de salvar, n�o buscar novamente
        // Salva em client_onboarding
        var co = new ClientOnboarding
        {
            Id = Guid.NewGuid(),
            EstablishmentId = input.Id,
            WhatsApp = input.WhatsApp ?? string.Empty,
            Numero = six,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            OnboardingCompleted = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false
        };
        _db.ClientOnboardings.Add(co);
        await _db.SaveChangesAsync(ct);

        // Envia WhatsApp via AtentBot
        var msg = $"Ol� equipe do {input.NomeFantasia}, estamos felizes com sua chegada, " +
                  $"seu numero de confirma��o � {six}.";

        await SendWhatsAppAsync(co.WhatsApp, msg, ct);

        return CreatedAtAction(nameof(Get), new { id = input.Id }, input);
    }

    // Allow-list de campos aceitos no cadastro público de establishment.
    // Deliberadamente NÃO inclui AccessLevelId, CategoryId, flags de assinatura/
    // marketplace, ratings, stripe, limites ou geo — todos definidos pela plataforma.
    public class CreateEstablishmentRequest
    {
        public string NomeFantasia { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string? Cnpj { get; set; }
        public string? InscricaoEstadual { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? WhatsApp { get; set; }
        public string? PostalCode { get; set; }
        public string? Street { get; set; }
        public string? Number { get; set; }
        public string? Complement { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Instagram { get; set; }
        public string? Facebook { get; set; }
        public string? Password { get; set; }
    }

    // 2) CONFIRMA��O: recebe c�digo de 6 d�gitos e finaliza onboarding
    public record ConfirmRequest(int Numero);

    [HttpPost("confirm"), Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("auth")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest req, CancellationToken ct)
    {
        if (req.Numero < 100000 || req.Numero > 999999)
            return BadRequest(new { error = "invalid_code_format" });

        // Busca o registro mais recente com esse n�mero que ainda esteja v�lido e n�o usado
        var co = await _db.ClientOnboardings
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(x => x.Numero == req.Numero, ct);

        if (co is null || co.IsUsed)
            return NotFound(new { error = "code_not_found" });

        if (co.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { error = "code_expired", message = "Código expirado. Solicite um novo." });

        // Marca como usado antes de qualquer outra operação para evitar race condition
        co.IsUsed = true;
        co.OnboardingCompleted = true;
        co.UpdatedAt = DateTime.UtcNow;

        // Atualiza establishment correspondente
        var est = await _db.Establishments.FirstOrDefaultAsync(e => e.Id == co.EstablishmentId, ct);
        if (est is null)
            return NotFound(new { error = "establishment_not_found" });

        est.OnboardingCompleted = true;
        est.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return Ok(new { status = "confirmed", establishmentId = est.Id });
    }

    // helper: envia mensagem WhatsApp direto ao provedor
    private async Task<bool> SendWhatsAppAsync(string number, string text, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(number)) return false;

        var apiKey = _config["AtentBot:ApiKey"];
        var baseUrl = _config["AtentBot:BaseUrl"] ?? "https://api.atentbot.com";
        if (string.IsNullOrWhiteSpace(apiKey)) return false;

        var client = _httpClientFactory.CreateClient();
        var url = $"{baseUrl.TrimEnd('/')}/message/sendText/crescer";
        var payload = new { number, text };

        using var http = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        http.Headers.Add("apikey", apiKey);

        var resp = await client.SendAsync(http, ct);
        return resp.IsSuccessStatusCode;
    }
}