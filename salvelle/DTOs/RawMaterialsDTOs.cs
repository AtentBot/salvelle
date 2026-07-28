using System;
using System.ComponentModel.DataAnnotations;

namespace DTOs;

// ════════════════════════════════════════════════════════════════════════════
// DTOs - RawMaterialsController
// ════════════════════════════════════════════════════════════════════════════

public class RawMaterialListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? DcbCode { get; set; }
    public string? DciCode { get; set; }
    public string CasNumber { get; set; } = "";
    public string ControlType { get; set; } = "";
    public string? Category { get; set; }
    public string? AllowedUsage { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public string Unit { get; set; } = "";

    // Preços
    public decimal? BasePrice { get; set; }
    public decimal? LastKnownPrice { get; set; }
    public DateTime? LastPriceDate { get; set; }
    public bool IsVirtual { get; set; }
    public string PriceSource { get; set; } = "";
    public decimal CurrentPrice { get; set; }

    // Propriedades físicas
    public decimal? BulkDensity { get; set; }
    public decimal? TappedDensity { get; set; }
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
    public decimal? DilutionFactor { get; set; }

    // Status
    public string StockStatus { get; set; } = "";
    public string PriceStatus { get; set; } = "";
    public bool IsPriceOutdated { get; set; }
    public int? DaysSinceLastPrice { get; set; }

    public bool RequiresSpecialAuthorization { get; set; }
    public bool RequiresRefrigeration { get; set; }
    public int Popularity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateBasePriceDto
{
    [Required]
    [Range(0.0001, 999999.99, ErrorMessage = "Preço deve ser maior que zero")]
    public decimal BasePrice { get; set; }
}

public class BulkPriceUpdateDto
{
    public Guid Id { get; set; }
    public decimal? NewPrice { get; set; }
}

public class CreateRawMaterialDto
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(50)]
    public string? DcbCode { get; set; }

    [MaxLength(50)]
    public string? DciCode { get; set; }

    [MaxLength(50)]
    public string? CasNumber { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? ControlType { get; set; } = "COMUM";

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Synonyms { get; set; }

    [MaxLength(1000)]
    public string? Indications { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; } = "g";

    [MaxLength(20)]
    public string? AllowedUsage { get; set; } = "BOTH";

    [MaxLength(20)]
    public string? PhysicalState { get; set; } = "SOLID";

    public decimal MinimumStock { get; set; }
    public decimal MaximumStock { get; set; }
    public bool IsVirtual { get; set; }
    public decimal? BasePrice { get; set; }

    // Propriedades físicas
    public decimal? BulkDensity { get; set; }
    public decimal? TappedDensity { get; set; }
    public decimal? CorrectionFactor { get; set; } = 1.0m;
    public decimal? PurityFactor { get; set; } = 1.0m;
    public decimal? DilutionFactor { get; set; } = 1.0m;
    public decimal? LossFactor { get; set; } = 0m;

    public int Popularity { get; set; } = 50;

    [MaxLength(500)]
    public string? StorageConditions { get; set; }

    public bool RequiresRefrigeration { get; set; }
    public bool LightSensitive { get; set; }
    public bool HumiditySensitive { get; set; }
}

public class UpdateRawMaterialDto
{
    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [MaxLength(50)]
    public string? ControlType { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    [MaxLength(500)]
    public string? Synonyms { get; set; }

    [MaxLength(1000)]
    public string? Indications { get; set; }

    [MaxLength(20)]
    public string? Unit { get; set; }

    [MaxLength(20)]
    public string? AllowedUsage { get; set; }

    [MaxLength(20)]
    public string? PhysicalState { get; set; }

    public decimal? MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }
    public bool? IsVirtual { get; set; }
    public decimal? BasePrice { get; set; }

    // Propriedades físicas
    public decimal? BulkDensity { get; set; }
    public decimal? TappedDensity { get; set; }
    public decimal? CorrectionFactor { get; set; }
    public decimal? PurityFactor { get; set; }
    public decimal? DilutionFactor { get; set; }
    public decimal? LossFactor { get; set; }

    public int? Popularity { get; set; }

    [MaxLength(500)]
    public string? StorageConditions { get; set; }

    public bool? RequiresRefrigeration { get; set; }
    public bool? LightSensitive { get; set; }
    public bool? HumiditySensitive { get; set; }
}