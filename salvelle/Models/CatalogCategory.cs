using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("CatalogCategories")]
public class CatalogCategory
{
    [Key]
    public Guid Id { get; set; }
    
    public Guid EstablishmentId { get; set; }
    
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [StringLength(50)]
    public string IconClass { get; set; } = "bi-box";
    
    public int SortOrder { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
    
    public virtual ICollection<CatalogProduct> Products { get; set; } = new List<CatalogProduct>();
}
