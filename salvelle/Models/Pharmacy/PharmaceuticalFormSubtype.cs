using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Subtipo de forma farmacêutica (Cápsula 00, Creme Lanette, etc.)
/// Contém composições de matérias-primas para bases/veículos
/// </summary>
[Table("pharmaceutical_form_subtypes")]
public class PharmaceuticalFormSubtype
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("pharmaceutical_form_id")]
    public Guid PharmaceuticalFormId { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // IDENTIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [Required]
    [Column("code")]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // CONFIGURAÇÕES
    // ═══════════════════════════════════════════════════════════════

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se é o subtipo padrão da forma farmacêutica
    /// </summary>
    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════
    // PRECIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Preço mínimo específico deste subtipo (sobrescreve o da forma)
    /// </summary>
    [Column("minimum_price", TypeName = "decimal(18,4)")]
    public decimal? MinimumPrice { get; set; }

    /// <summary>
    /// Custo base calculado a partir da composição
    /// </summary>
    [Column("base_cost", TypeName = "decimal(18,4)")]
    public decimal BaseCost { get; set; } = 0;

    // ═══════════════════════════════════════════════════════════════
    // PRODUÇÃO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Quantidade de rendimento padrão (ex: 100g de creme)
    /// </summary>
    [Column("yield_quantity", TypeName = "decimal(18,4)")]
    public decimal YieldQuantity { get; set; } = 100;

    /// <summary>
    /// Unidade do rendimento
    /// </summary>
    [Column("yield_unit")]
    [MaxLength(20)]
    public string YieldUnit { get; set; } = "g";

    /// <summary>
    /// Validade específica em dias (se diferente da forma)
    /// </summary>
    [Column("validity_days")]
    public int? ValidityDays { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // LIMITES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Limite máximo de quantidade (sobrescreve o da forma)
    /// </summary>
    [Column("max_quantity_limit", TypeName = "decimal(18,4)")]
    public decimal? MaxQuantityLimit { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // ESPECÍFICO PARA CÁPSULAS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tamanho da cápsula (0000, 000, 00, 0, 1, 2, 3, 4, 5)
    /// </summary>
    [Column("capsule_size")]
    [MaxLength(10)]
    public string? CapsuleSize { get; set; }

    /// <summary>
    /// Volume da cápsula em mL
    /// </summary>
    [Column("capsule_volume_ml", TypeName = "decimal(6,4)")]
    public decimal? CapsuleVolumeMl { get; set; }

    /// <summary>
    /// Capacidade mínima em mg
    /// </summary>
    [Column("capsule_capacity_mg_min", TypeName = "decimal(10,2)")]
    public decimal? CapsuleCapacityMgMin { get; set; }

    /// <summary>
    /// Capacidade máxima em mg
    /// </summary>
    [Column("capsule_capacity_mg_max", TypeName = "decimal(10,2)")]
    public decimal? CapsuleCapacityMgMax { get; set; }

    /// <summary>
    /// Cor da cápsula (opcional)
    /// </summary>
    [Column("capsule_color")]
    [MaxLength(50)]
    public string? CapsuleColor { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // INSTRUÇÕES
    // ═══════════════════════════════════════════════════════════════

    [Column("preparation_instructions")]
    public string? PreparationInstructions { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // ORDENAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [Column("sort_order")]
    public int SortOrder { get; set; } = 100;

    // ═══════════════════════════════════════════════════════════════
    // AUDITORIA
    // ═══════════════════════════════════════════════════════════════

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [ForeignKey(nameof(PharmaceuticalFormId))]
    public virtual PharmaceuticalForm? PharmaceuticalForm { get; set; }

    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public virtual Employee? CreatedByEmployee { get; set; }

    [ForeignKey(nameof(UpdatedByEmployeeId))]
    public virtual Employee? UpdatedByEmployee { get; set; }

    public virtual ICollection<PharmaceuticalFormComposition>? Compositions { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Capacidade prática da cápsula (média entre min e max, com margem)
    /// </summary>
    [NotMapped]
    public decimal? PracticalCapsuleCapacity
    {
        get
        {
            if (!CapsuleCapacityMgMin.HasValue || !CapsuleCapacityMgMax.HasValue)
                return null;
            
            // Usa ~80% da capacidade máxima como prático
            return Math.Round(CapsuleCapacityMgMax.Value * 0.8m, 0);
        }
    }

    /// <summary>
    /// Verifica se é um subtipo de cápsula
    /// </summary>
    [NotMapped]
    public bool IsCapsuleSubtype => !string.IsNullOrEmpty(CapsuleSize);

    /// <summary>
    /// Obtém o preço mínimo efetivo (próprio ou da forma pai)
    /// </summary>
    public decimal GetEffectiveMinimumPrice()
    {
        return MinimumPrice ?? PharmaceuticalForm?.MinimumPrice ?? 0;
    }
}
