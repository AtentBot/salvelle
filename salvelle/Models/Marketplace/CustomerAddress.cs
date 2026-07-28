using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("customer_addresses")]
public class CustomerAddress
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("label")]
    [MaxLength(50)]
    public string? Label { get; set; } // "Casa", "Trabalho"

    [Required]
    [Column("street")]
    [MaxLength(200)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [Column("number")]
    [MaxLength(20)]
    public string Number { get; set; } = string.Empty;

    [Column("complement")]
    [MaxLength(100)]
    public string? Complement { get; set; }

    [Required]
    [Column("neighborhood")]
    [MaxLength(100)]
    public string Neighborhood { get; set; } = string.Empty;

    [Required]
    [Column("city")]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;

    [Required]
    [Column("state")]
    [MaxLength(2)]
    public string State { get; set; } = string.Empty;

    [Required]
    [Column("zip_code")]
    [MaxLength(8)]
    public string ZipCode { get; set; } = string.Empty;

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}
