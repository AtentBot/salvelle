using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("search_history")]
public class SearchHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    [Required]
    [Column("search_term")]
    [MaxLength(300)]
    public string SearchTerm { get; set; } = string.Empty;

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("result_count")]
    public int ResultCount { get; set; }

    [Column("search_type")]
    [MaxLength(20)]
    public string SearchType { get; set; } = "PRODUCT"; // PRODUCT, PHARMACY

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}
