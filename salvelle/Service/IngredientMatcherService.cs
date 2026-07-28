using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Models.Pharmacy;

namespace Service;

public class IngredientMatcherService
{
    private readonly AppDbContext _context;
    private readonly ILogger<IngredientMatcherService> _logger;

    public IngredientMatcherService(
        AppDbContext context,
        ILogger<IngredientMatcherService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IngredientMatchResponseDto> FindMatchesAsync(List<OcrItemDto> ocrItems)
    {
        _logger.LogInformation($"Finding matches for {ocrItems.Count} OCR items");

        var matches = new List<IngredientMatchDto>();

        foreach (var item in ocrItems)
        {
            var suggestions = await FindSuggestionsAsync(item.Component);

            matches.Add(new IngredientMatchDto
            {
                OcrText = item.Component,
                RawText = item.RawText,
                Quantity = item.Quantity,
                Unit = item.Unit,
                Suggestions = suggestions
            });
        }

        return new IngredientMatchResponseDto { Matches = matches };
    }

    private async Task<List<RawMaterialSuggestionDto>> FindSuggestionsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<RawMaterialSuggestionDto>();

        var normalized = NormalizeText(searchTerm);

        // Buscar todas as matérias-primas ativas
        var rawMaterials = await _context.Set<RawMaterial>()
            .Where(rm => rm.IsActive)
            .Select(rm => new
            {
                rm.Id,
                rm.Name,           // CORRIGIDO: Name ao invés de CommercialName
                rm.DcbCode,        // CORRIGIDO: DcbCode ao invés de DciName
                rm.Unit
            })
            .ToListAsync();

        // Calcular similaridade
        var scored = rawMaterials
            .Select(rm =>
            {
                var nameNormalized = NormalizeText(rm.Name);
                var dcbNormalized = NormalizeText(rm.DcbCode ?? "");

                var distanceName = LevenshteinDistance(normalized, nameNormalized);
                var distanceDcb = string.IsNullOrEmpty(dcbNormalized)
                    ? int.MaxValue
                    : LevenshteinDistance(normalized, dcbNormalized);

                var minDistance = Math.Min(distanceName, distanceDcb);
                var matchedName = distanceName < distanceDcb ? nameNormalized : dcbNormalized;
                var maxLength = Math.Max(normalized.Length, matchedName.Length);

                var confidence = maxLength == 0 ? 0.0 : 1.0 - ((double)minDistance / maxLength);

                // Boost para matches exatos ou que começam igual
                if (nameNormalized == normalized || dcbNormalized == normalized)
                {
                    confidence = 1.0;
                }
                else if (nameNormalized.StartsWith(normalized) || normalized.StartsWith(nameNormalized) ||
                         dcbNormalized.StartsWith(normalized) || normalized.StartsWith(dcbNormalized))
                {
                    confidence = Math.Max(confidence, 0.85);
                }

                return new
                {
                    RawMaterial = rm,
                    Confidence = confidence,
                    Distance = minDistance
                };
            })
            .Where(x => x.Confidence > 0.5)
            .OrderByDescending(x => x.Confidence)
            .ThenBy(x => x.Distance)
            .Take(5)
            .ToList();

        var suggestions = new List<RawMaterialSuggestionDto>();

        foreach (var item in scored)
        {
            var stock = await GetAvailableStockAsync(item.RawMaterial.Id);

            suggestions.Add(new RawMaterialSuggestionDto
            {
                RawMaterialId = item.RawMaterial.Id,
                Name = item.RawMaterial.Name,         // CORRIGIDO
                DciName = item.RawMaterial.DcbCode,   // CORRIGIDO
                Confidence = (decimal)item.Confidence,
                InStock = stock > 0,
                AvailableQuantity = stock,
                Unit = item.RawMaterial.Unit
            });
        }

        _logger.LogInformation($"Found {suggestions.Count} suggestions for '{searchTerm}'");

        return suggestions;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var result = stringBuilder.ToString()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Trim();

        result = new string(result.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray());

        return result;
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1))
            return string.IsNullOrEmpty(s2) ? 0 : s2.Length;

        if (string.IsNullOrEmpty(s2))
            return s1.Length;

        var distance = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            distance[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            distance[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;

                distance[i, j] = Math.Min(
                    Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                    distance[i - 1, j - 1] + cost
                );
            }
        }

        return distance[s1.Length, s2.Length];
    }

    private async Task<decimal> GetAvailableStockAsync(Guid rawMaterialId)
    {
        var totalStock = await _context.Set<Batch>()
            .Where(b => b.RawMaterialId == rawMaterialId
                && b.Status == "APROVADO"
                && b.CurrentQuantity > 0
                && b.ExpiryDate > DateTime.UtcNow)
            .SumAsync(b => (decimal?)b.CurrentQuantity) ?? 0;

        return totalStock;
    }
}