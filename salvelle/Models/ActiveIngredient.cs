using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Catálogo de ingredientes ativos para autocomplete em fórmulas personalizadas
/// </summary>
[Table("active_ingredients")]
public class ActiveIngredient
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Nome do ingrediente (ex: "Vitamina C", "Ácido Hialurônico")
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nome normalizado para busca (sem acentos, lowercase)
    /// </summary>
    [MaxLength(200)]
    [Column("normalized_name")]
    public string NormalizedName { get; set; } = string.Empty;

    /// <summary>
    /// Sinônimos e nomes alternativos (separados por vírgula)
    /// Ex: "Ácido Ascórbico, Ascorbic Acid, Vit C"
    /// </summary>
    [MaxLength(500)]
    [Column("synonyms")]
    public string? Synonyms { get; set; }

    /// <summary>
    /// Categoria do ingrediente
    /// </summary>
    [MaxLength(100)]
    [Column("category")]
    public string? Category { get; set; }

    /// <summary>
    /// Subcategoria
    /// </summary>
    [MaxLength(100)]
    [Column("subcategory")]
    public string? Subcategory { get; set; }

    /// <summary>
    /// Unidade padrão (g, mg, ml, %, UI)
    /// </summary>
    [MaxLength(20)]
    [Column("default_unit")]
    public string DefaultUnit { get; set; } = "mg";

    /// <summary>
    /// Dosagem mínima típica
    /// </summary>
    [Column("min_dosage")]
    public decimal? MinDosage { get; set; }

    /// <summary>
    /// Dosagem máxima típica
    /// </summary>
    [Column("max_dosage")]
    public decimal? MaxDosage { get; set; }

    /// <summary>
    /// Preço estimado por unidade padrão
    /// </summary>
    [Column("price_per_unit", TypeName = "decimal(10,4)")]
    public decimal? PricePerUnit { get; set; }

    /// <summary>
    /// Descrição curta do ingrediente
    /// </summary>
    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Indicações comuns
    /// </summary>
    [MaxLength(500)]
    [Column("indications")]
    public string? Indications { get; set; }

    /// <summary>
    /// Requer receita médica
    /// </summary>
    [Column("requires_prescription")]
    public bool RequiresPrescription { get; set; } = false;

    /// <summary>
    /// É substância controlada (Portaria 344)
    /// </summary>
    [Column("is_controlled")]
    public bool IsControlled { get; set; } = false;

    /// <summary>
    /// Código DCB (Denominação Comum Brasileira)
    /// </summary>
    [MaxLength(20)]
    [Column("dcb_code")]
    public string? DcbCode { get; set; }

    /// <summary>
    /// Código CAS (Chemical Abstracts Service)
    /// </summary>
    [MaxLength(20)]
    [Column("cas_code")]
    public string? CasCode { get; set; }

    /// <summary>
    /// Ativo para uso
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Ordem de popularidade (maior = mais popular)
    /// </summary>
    [Column("popularity")]
    public int Popularity { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
