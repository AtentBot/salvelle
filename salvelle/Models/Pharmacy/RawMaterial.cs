using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Models.Pharmacy;

[Index(nameof(EstablishmentId), nameof(DcbCode), IsUnique = true)]
[Index(nameof(Name))]
[Index(nameof(ControlType))]
[Index(nameof(EstablishmentId), nameof(IsActive))]
[Index(nameof(Category))]
[Index(nameof(IsVirtual))]

[Table("RawMaterials")]
public class RawMaterial
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid EstablishmentId { get; set; }
    public Establishment? Establishment { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [MaxLength(50)]
    public string? DcbCode { get; set; }

    [MaxLength(50)]
    public string? DciCode { get; set; }

    [Required, MaxLength(50)]
    public string CasNumber { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required, MaxLength(20)]
    public string ControlType { get; set; } = "COMUM";

    [Required, MaxLength(10)]
    public string Unit { get; set; } = "g";

    // ═══════════════════════════════════════════════════════════════════════════
    // FATORES BASE
    // ═══════════════════════════════════════════════════════════════════════════

    [Column(TypeName = "decimal(10,4)")]
    public decimal PurityFactor { get; set; } = 1.0m;

    [Column(TypeName = "decimal(10,4)")]
    public decimal EquivalenceFactor { get; set; } = 1.0m;

    // ═══════════════════════════════════════════════════════════════════════════
    // PROPRIEDADES FÍSICAS (para cálculo de cápsulas e formulações)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Uso permitido: ORAL, TOPICAL, BOTH
    /// </summary>
    [Required, MaxLength(20)]
    public string AllowedUsage { get; set; } = "BOTH";

    /// <summary>
    /// Estado físico: SOLID, LIQUID, SEMI_SOLID
    /// </summary>
    [Required, MaxLength(20)]
    public string PhysicalState { get; set; } = "SOLID";

    /// <summary>
    /// Granulometria do pó
    /// </summary>
    [MaxLength(50)]
    public string? ParticleSize { get; set; }

    /// <summary>
    /// Densidade aparente do pó solto (g/mL)
    /// </summary>
    [Column(TypeName = "decimal(6,4)")]
    public decimal? BulkDensity { get; set; }

    /// <summary>
    /// Densidade após compactação (g/mL)
    /// </summary>
    [Column(TypeName = "decimal(6,4)")]
    public decimal? TappedDensity { get; set; }

    /// <summary>
    /// Fator de correção por variação entre lotes
    /// </summary>
    [Column(TypeName = "decimal(6,4)")]
    public decimal CorrectionFactor { get; set; } = 1.0m;

    /// <summary>
    /// Fator de diluição (ex: 100 para ativo 1:100)
    /// </summary>
    [Column(TypeName = "decimal(8,4)")]
    public decimal DilutionFactor { get; set; } = 1.0m;

    /// <summary>
    /// Perda típica em manipulação (%)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal LossFactor { get; set; } = 0m;

    // ═══════════════════════════════════════════════════════════════════════════
    // ESTOQUE
    // ═══════════════════════════════════════════════════════════════════════════

    [Column(TypeName = "decimal(18,4)")]
    public decimal CurrentStock { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal MinimumStock { get; set; } = 0;

    [Column(TypeName = "decimal(18,4)")]
    public decimal MaximumStock { get; set; } = 0;

    // ═══════════════════════════════════════════════════════════════════════════
    // CONDIÇÕES DE ARMAZENAMENTO
    // ═══════════════════════════════════════════════════════════════════════════

    [MaxLength(200)]
    public string? StorageConditions { get; set; }

    public bool RequiresRefrigeration { get; set; } = false;
    public bool LightSensitive { get; set; } = false;
    public bool HumiditySensitive { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════════════════
    // PRECIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Preço base de referência de mercado (R$/unidade)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? BasePrice { get; set; }

    /// <summary>
    /// Último preço efetivamente pago (R$/unidade)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? LastKnownPrice { get; set; }

    /// <summary>
    /// Data do último preço pago
    /// </summary>
    public DateTime? LastPriceDate { get; set; }

    /// <summary>
    /// Último preço de compra (atualizado automaticamente)
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? LastPurchasePrice { get; set; }

    /// <summary>
    /// Data do último preço de compra
    /// </summary>
    public DateTime? LastPurchasePriceDate { get; set; }

    /// <summary>
    /// TRUE = nunca teve em estoque, apenas referência
    /// </summary>
    public bool IsVirtual { get; set; } = false;

    /// <summary>
    /// Origem do preço atual: ESTOQUE, HISTORICO, BASE
    /// </summary>
    [Required, MaxLength(20)]
    public string PriceSource { get; set; } = "BASE";

    /// <summary>
    /// Markup específico adicional para este ativo (%)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? SpecificMarkup { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    // CATEGORIZAÇÃO E BUSCA
    // ═══════════════════════════════════════════════════════════════════════════

    [MaxLength(100)]
    public string? Category { get; set; }

    public string? Synonyms { get; set; }

    public string? Indications { get; set; }

    public int Popularity { get; set; } = 50;

    // ═══════════════════════════════════════════════════════════════════════════
    // STATUS E AUDITORIA
    // ═══════════════════════════════════════════════════════════════════════════

    public bool IsActive { get; set; } = true;
    public bool RequiresSpecialAuthorization { get; set; } = false;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? CreatedByEmployeeId { get; set; }
    public Guid? UpdatedByEmployeeId { get; set; }

    // ═══════════════════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ═══════════════════════════════════════════════════════════════════════════

    public ICollection<Batch>? Batches { get; set; }
    public ICollection<FormulaComponent>? FormulaComponents { get; set; }
    public ICollection<StockMovement>? StockMovements { get; set; }
}