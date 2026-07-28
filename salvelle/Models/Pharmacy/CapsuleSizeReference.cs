using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

/// <summary>
/// Tabela de referência de tamanhos de cápsulas
/// Usada para cálculo automático de seleção de cápsulas
/// </summary>
[Table("capsule_size_reference")]
public class CapsuleSizeReference
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // IDENTIFICAÇÃO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Código do tamanho (0000, 000, 00E, 00, 0E, 0, 1, 2, 3, 4, 5)
    /// </summary>
    [Required]
    [Column("size_code")]
    [MaxLength(10)]
    public string SizeCode { get; set; } = string.Empty;

    /// <summary>
    /// Nome amigável
    /// </summary>
    [Required]
    [Column("name")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════
    // ESPECIFICAÇÕES TÉCNICAS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Volume em mL
    /// </summary>
    [Column("volume_ml", TypeName = "decimal(6,4)")]
    public decimal VolumeMl { get; set; }

    /// <summary>
    /// Capacidade mínima em mg (densidade baixa ~0.6 g/mL)
    /// </summary>
    [Column("capacity_mg_min", TypeName = "decimal(10,2)")]
    public decimal CapacityMgMin { get; set; }

    /// <summary>
    /// Capacidade máxima em mg (densidade alta ~1.2 g/mL)
    /// </summary>
    [Column("capacity_mg_max", TypeName = "decimal(10,2)")]
    public decimal CapacityMgMax { get; set; }

    /// <summary>
    /// Capacidade prática recomendada em mg
    /// Geralmente ~80% da capacidade com densidade média
    /// </summary>
    [Column("practical_capacity_mg", TypeName = "decimal(10,2)")]
    public decimal PracticalCapacityMg { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // CONFIGURAÇÕES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Se está ativo para este estabelecimento
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Se é um tamanho comum (mais usado)
    /// </summary>
    [Column("is_common")]
    public bool IsCommon { get; set; } = true;

    // ═══════════════════════════════════════════════════════════════
    // ORDENAÇÃO
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Ordem de exibição (do menor para o maior)
    /// </summary>
    [Column("sort_order")]
    public int SortOrder { get; set; } = 100;

    // ═══════════════════════════════════════════════════════════════
    // AUDITORIA
    // ═══════════════════════════════════════════════════════════════

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ═══════════════════════════════════════════════════════════════
    // NAVEGAÇÃO
    // ═══════════════════════════════════════════════════════════════

    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifica se um peso cabe nesta cápsula
    /// </summary>
    /// <param name="weightMg">Peso em mg</param>
    /// <returns>True se cabe</returns>
    public bool CanFit(decimal weightMg)
    {
        return weightMg <= PracticalCapacityMg;
    }

    /// <summary>
    /// Calcula quantas cápsulas seriam necessárias para um peso
    /// </summary>
    /// <param name="weightMg">Peso total em mg</param>
    /// <returns>Número de cápsulas (arredondado para cima)</returns>
    public int CalculateCapsuleCount(decimal weightMg)
    {
        if (PracticalCapacityMg <= 0)
            return 0;

        return (int)Math.Ceiling(weightMg / PracticalCapacityMg);
    }

    /// <summary>
    /// Calcula a capacidade para uma matéria-prima específica baseada na densidade
    /// </summary>
    /// <param name="density">Densidade em g/mL</param>
    /// <returns>Capacidade em mg</returns>
    public decimal CalculateCapacityForDensity(decimal density)
    {
        // Volume (mL) × Densidade (g/mL) × 1000 = mg
        // Aplica fator de 80% para margem de segurança
        return VolumeMl * density * 1000m * 0.8m;
    }

    /// <summary>
    /// Descrição formatada para UI
    /// </summary>
    [NotMapped]
    public string DisplayDescription =>
        $"{Name} ({VolumeMl:F2} mL, ~{PracticalCapacityMg:F0} mg)";
}
