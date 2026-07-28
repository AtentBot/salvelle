using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("supplier_certificates")]
public class SupplierCertificate
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("SupplierId")]
    public Guid SupplierId { get; set; }

    [Required]
    [MaxLength(50)]
    [Column("CertificateType")]
    public string CertificateType { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Column("Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("Number")]
    public string? Number { get; set; }

    [MaxLength(200)]
    [Column("IssuingAuthority")]
    public string? IssuingAuthority { get; set; }

    [Column("IssueDate")]
    public DateTime? IssueDate { get; set; }

    [Column("ExpiryDate")]
    public DateTime? ExpiryDate { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = string.Empty;  // ✅ SEM DEFAULT

    [MaxLength(500)]
    [Column("FilePath")]
    public string? FilePath { get; set; }

    [Column("Notes")]
    public string? Notes { get; set; }

    [Column("AlertBeforeExpiry")]
    public bool AlertBeforeExpiry { get; set; }  // ✅ SEM DEFAULT

    [Column("AlertDaysBefore")]
    public int AlertDaysBefore { get; set; }  // ✅ SEM DEFAULT

    [Column("IsActive")]
    public bool IsActive { get; set; }  // ✅ SEM DEFAULT

    // ==================== AUDIT ====================

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }  // ✅ SEM DEFAULT

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }  // ✅ SEM DEFAULT

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("UpdatedByEmployeeId")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // ==================== NAVEGAÇÃO ====================

    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    // ==================== COMPUTED PROPERTIES ====================

    [NotMapped]
    public bool IsValid => ExpiryDate == null || ExpiryDate > DateTime.UtcNow;

    [NotMapped]
    public int? DaysUntilExpiry => ExpiryDate.HasValue
        ? (int)(ExpiryDate.Value - DateTime.UtcNow).TotalDays
        : null;

    [NotMapped]
    public bool IsExpiringSoon => DaysUntilExpiry.HasValue &&
        DaysUntilExpiry.Value <= AlertDaysBefore &&
        DaysUntilExpiry.Value > 0;
}