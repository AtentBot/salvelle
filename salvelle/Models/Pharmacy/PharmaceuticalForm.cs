using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Pharmacy;

/// <summary>
/// Forma farmacêutica (Cápsula, Creme, Pomada, etc.)
/// Cada estabelecimento pode ativar/desativar e personalizar suas formas
/// </summary>
[Table("pharmaceutical_forms")]
public class PharmaceuticalForm
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

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

    /// <summary>
    /// Se é uma forma padrão do sistema (não pode ser excluída)
    /// </summary>
    [Column("is_system_default")]
    public bool IsSystemDefault { get; set; } = false;

    /// <summary>
    /// Se está ativa para este estabelecimento
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se é uma forma personalizada criada pela farmácia
    /// </summary>
    [Column("is_custom")]
    public bool IsCustom { get; set; } = false;

    // ═══════════════════════════════════════════════════════════════
    // PRECIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Preço mínimo para esta forma farmacêutica
    /// Se o preço calculado for menor, aplica este valor
    /// </summary>
    [Column("minimum_price", TypeName = "decimal(18,4)")]
    public decimal MinimumPrice { get; set; } = 0;

    // ═══════════════════════════════════════════════════════════════
    // LIMITES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Quantidade máxima permitida em orçamentos (evita abusos)
    /// </summary>
    [Column("max_quantity_limit", TypeName = "decimal(18,4)")]
    public decimal? MaxQuantityLimit { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // PADRÕES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Validade padrão em dias
    /// </summary>
    [Column("default_validity_days")]
    public int DefaultValidityDays { get; set; } = 180;

    /// <summary>
    /// Unidade padrão (g, mL, un)
    /// </summary>
    [Column("default_unit")]
    [MaxLength(20)]
    public string DefaultUnit { get; set; } = "g";

    /// <summary>
    /// Tempo estimado de manipulação em horas
    /// </summary>
    [Column("preparation_time_hours", TypeName = "decimal(5,2)")]
    public decimal? PreparationTimeHours { get; set; }

    /// <summary>
    /// Instruções de uso padrão
    /// </summary>
    [Column("usage_instructions")]
    [MaxLength(500)]
    public string? UsageInstructions { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // TIPO DE USO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tipo de uso: ORAL, TOPICAL ou BOTH
    /// Usado para validar compatibilidade com matérias-primas
    /// </summary>
    [Column("usage_type")]
    [MaxLength(20)]
    public string UsageType { get; set; } = "BOTH";

    // ═══════════════════════════════════════════════════════════════
    // ORDENAÇÃO E UI
    // ═══════════════════════════════════════════════════════════════

    [Column("sort_order")]
    public int SortOrder { get; set; } = 100;

    [Column("icon")]
    [MaxLength(50)]
    public string Icon { get; set; } = "bi-capsule";

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

    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public virtual Employee? CreatedByEmployee { get; set; }

    [ForeignKey(nameof(UpdatedByEmployeeId))]
    public virtual Employee? UpdatedByEmployee { get; set; }

    public virtual ICollection<PharmaceuticalFormSubtype>? Subtypes { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica se uma matéria-prima é compatível com esta forma
    /// </summary>
    public bool IsCompatibleWith(string rawMaterialAllowedUsage)
    {
        if (string.IsNullOrEmpty(rawMaterialAllowedUsage) || rawMaterialAllowedUsage == "BOTH")
            return true;

        if (UsageType == "BOTH")
            return true;

        return UsageType == rawMaterialAllowedUsage;
    }
}
