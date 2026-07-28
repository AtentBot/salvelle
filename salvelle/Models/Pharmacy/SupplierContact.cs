using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("supplier_contacts")]
public class SupplierContact
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("SupplierId")]
    public Guid SupplierId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("FullName")]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("JobTitle")]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    [Column("Department")]
    public string? Department { get; set; }

    [MaxLength(200)]
    [Column("Email")]
    public string? Email { get; set; }

    [MaxLength(20)]
    [Column("Phone")]
    public string? Phone { get; set; }

    [MaxLength(20)]
    [Column("Mobile")]
    public string? Mobile { get; set; }

    [MaxLength(10)]
    [Column("Extension")]
    public string? Extension { get; set; }

    [Column("IsPrimary")]
    public bool IsPrimary { get; set; }  // ✅ SEM DEFAULT

    [Column("IsEmergencyContact")]
    public bool IsEmergencyContact { get; set; }  // ✅ SEM DEFAULT

    [Column("IsActive")]
    public bool IsActive { get; set; }  // ✅ SEM DEFAULT

    [MaxLength(500)]
    [Column("Notes")]
    public string? Notes { get; set; }

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
}