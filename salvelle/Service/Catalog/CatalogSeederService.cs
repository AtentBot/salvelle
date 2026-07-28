using System.Text.Json;
using System.Text.Json.Serialization;
using Data;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;

namespace Service.Catalog;

public class CatalogSeederService
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CatalogSeederService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public CatalogSeederService(AppDbContext db, IWebHostEnvironment env, ILogger<CatalogSeederService> logger)
    {
        _db = db;
        _env = env;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var existingCount = await _db.RawMaterialsCatalog.CountAsync(ct);
        if (existingCount > 0)
        {
            _logger.LogInformation("Catálogo já tem {Count} itens, pulando seed", existingCount);
            return;
        }

        var path = Path.Combine(_env.ContentRootPath, "Data", "Seeds", "raw_materials_catalog.json");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Arquivo de seed não encontrado em {Path}", path);
            return;
        }

        CatalogSeedFile? parsed;
        await using (var stream = File.OpenRead(path))
        {
            parsed = await JsonSerializer.DeserializeAsync<CatalogSeedFile>(stream, JsonOptions, ct);
        }

        if (parsed?.Items == null || parsed.Items.Count == 0)
        {
            _logger.LogWarning("Seed file vazio ou inválido");
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var inserted = 0;
        var skipped = 0;

        foreach (var item in parsed.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Name) || !seen.Add(item.Name.Trim()))
            {
                skipped++;
                continue;
            }

            _db.RawMaterialsCatalog.Add(new RawMaterialCatalog
            {
                Id = Guid.NewGuid(),
                Name = item.Name.Trim(),
                DcbCode = string.IsNullOrWhiteSpace(item.DcbCode) ? null : item.DcbCode.Trim(),
                CasNumber = string.IsNullOrWhiteSpace(item.CasNumber) ? null : item.CasNumber.Trim(),
                Category = string.IsNullOrWhiteSpace(item.Category) ? null : item.Category.Trim(),
                ControlType = (item.ControlType ?? "COMUM").Trim().ToUpperInvariant(),
                AllowedUsage = (item.AllowedUsage ?? "BOTH").Trim().ToUpperInvariant(),
                PhysicalState = (item.PhysicalState ?? "SOLID").Trim().ToUpperInvariant(),
                Unit = string.IsNullOrWhiteSpace(item.Unit) ? "g" : item.Unit.Trim(),
                DefaultPurityFactor = item.DefaultPurityFactor ?? 1.0m,
                DefaultCorrectionFactor = item.DefaultCorrectionFactor ?? 1.0m,
                Synonyms = string.IsNullOrWhiteSpace(item.Synonyms) ? null : item.Synonyms.Trim(),
                Indications = string.IsNullOrWhiteSpace(item.Indications) ? null : item.Indications.Trim(),
                Popularity = item.Popularity is > 0 and <= 100 ? item.Popularity.Value : 50,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            });
            inserted++;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Catálogo inicial seedado: {Inserted} itens inseridos, {Skipped} ignorados", inserted, skipped);
    }

    private sealed class CatalogSeedFile
    {
        public string? Version { get; set; }
        public string? GeneratedAt { get; set; }
        public int? TotalItems { get; set; }
        public List<CatalogSeedItem> Items { get; set; } = new();
    }

    private sealed class CatalogSeedItem
    {
        public string Name { get; set; } = default!;
        public string? DcbCode { get; set; }
        public string? CasNumber { get; set; }
        public string? Category { get; set; }
        public string? ControlType { get; set; }
        public string? AllowedUsage { get; set; }
        public string? PhysicalState { get; set; }
        public string? Unit { get; set; }
        public decimal? DefaultPurityFactor { get; set; }
        public decimal? DefaultCorrectionFactor { get; set; }
        public string? Synonyms { get; set; }
        public string? Indications { get; set; }
        public int? Popularity { get; set; }
    }
}
