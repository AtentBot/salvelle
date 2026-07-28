using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("ProductTypes")]
public class ProductType
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("Name")]
    public string Name { get; set; } = default!;

    [MaxLength(500)]
    [Column("Description")]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("PharmaceuticalForm")]
    public string PharmaceuticalForm { get; set; } = default!;

    [Required]
    [MaxLength(50)]
    [Column("Category")]
    public string Category { get; set; } = default!;

    [Column("IsActive")]
    public bool IsActive { get; set; } = true;

    [Column("DisplayOrder")]
    public int? DisplayOrder { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<ProductSubType>? SubTypes { get; set; }
    public ICollection<CustomerFormula>? CustomerFormulas { get; set; }
}
