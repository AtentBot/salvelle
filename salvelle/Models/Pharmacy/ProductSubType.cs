using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("ProductSubTypes")]
public class ProductSubType
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("ProductTypeId")]
    public Guid ProductTypeId { get; set; }
    
    [ForeignKey("ProductTypeId")]
    public ProductType? ProductType { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("Name")]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    [Column("Description")]
    public string? Description { get; set; }

    [Column("BaseFormulaId")]
    public Guid? BaseFormulaId { get; set; }

    [Column("StandardQuantity")]
    public decimal? StandardQuantity { get; set; }

    [MaxLength(10)]
    [Column("StandardUnit")]
    public string? StandardUnit { get; set; }

    [Column("PriceModifier")]
    public decimal PriceModifier { get; set; } = 1.0m;

    [Column("ManipulationCostBase")]
    public decimal ManipulationCostBase { get; set; } = 15.00m;

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("DisplayOrder")]
    public int? DisplayOrder { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<CustomerFormula>? CustomerFormulas { get; set; }
}
