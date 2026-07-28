using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models;

/// <summary>
/// QR Code para acesso ao portal do cliente
/// </summary>
[Table("EstablishmentQRCodes")]
public class EstablishmentQRCode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EstablishmentId { get; set; }

    [Required]
    [StringLength(10)]
    public string Code { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int ScanCount { get; set; } = 0;

    public DateTime? LastScannedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedByEmployeeId { get; set; }

    // ============================================================
    // NAVIGATION PROPERTIES
    // ============================================================

    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public virtual Employee? CreatedByEmployee { get; set; }
}