using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("customer_devices")]
public class CustomerDevice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Required]
    [Column("device_token")]
    [MaxLength(500)]
    public string DeviceToken { get; set; } = string.Empty;

    /// <summary>
    /// ANDROID, IOS
    /// </summary>
    [Required]
    [Column("platform")]
    [MaxLength(10)]
    public string Platform { get; set; } = string.Empty;

    [Column("device_model")]
    [MaxLength(100)]
    public string? DeviceModel { get; set; }

    [Column("os_version")]
    [MaxLength(20)]
    public string? OsVersion { get; set; }

    [Column("app_version")]
    [MaxLength(20)]
    public string? AppVersion { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}
