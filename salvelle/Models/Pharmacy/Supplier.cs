using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

/// <summary>
/// Fornecedor de matérias-primas e produtos para farmácia de manipulação
/// </summary>
[Table("suppliers")]
public class Supplier
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("EstablishmentId")]
    public Guid EstablishmentId { get; set; }

    [ForeignKey(nameof(EstablishmentId))]
    public Establishment? Establishment { get; set; }

    public virtual ICollection<Batch>? Batches { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("CompanyName")]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Column("TradeName")]
    public string? TradeName { get; set; }

    [Required]
    [MaxLength(14)]
    [Column("Cnpj")]
    public string Cnpj { get; set; } = string.Empty;

    [MaxLength(20)]
    [Column("StateRegistration")]
    public string? StateRegistration { get; set; }

    [MaxLength(20)]
    [Column("MunicipalRegistration")]
    public string? MunicipalRegistration { get; set; }

    // ==================== ENDEREÇO ====================

    [MaxLength(100)]
    [Column("Street")]
    public string? Street { get; set; }

    [MaxLength(10)]
    [Column("Number")]
    public string? Number { get; set; }

    [MaxLength(100)]
    [Column("Complement")]
    public string? Complement { get; set; }

    [MaxLength(100)]
    [Column("Neighborhood")]
    public string? Neighborhood { get; set; }

    [MaxLength(100)]
    [Column("City")]
    public string? City { get; set; }

    [MaxLength(2)]
    [Column("State")]
    public string? State { get; set; }

    [MaxLength(8)]
    [Column("PostalCode")]
    public string? PostalCode { get; set; }

    [MaxLength(50)]
    [Column("Country")]
    public string? Country { get; set; }  // ✅ SEM DEFAULT

    // ==================== CONTATO ====================

    [MaxLength(20)]
    [Column("Phone")]
    public string? Phone { get; set; }

    [MaxLength(20)]
    [Column("WhatsApp")]
    public string? WhatsApp { get; set; }

    [MaxLength(200)]
    [Column("Email")]
    public string? Email { get; set; }

    [MaxLength(200)]
    [Column("Website")]
    public string? Website { get; set; }

    // ==================== CLASSIFICAÇÃO ====================

    [Required]
    [MaxLength(20)]
    [Column("Status")]
    public string Status { get; set; } = string.Empty;  // ✅ SEM DEFAULT

    [MaxLength(1)]
    [Column("Classification")]
    public string? Classification { get; set; }

    [Column("Rating")]
    public decimal? Rating { get; set; }

    [Column("IsQualified")]
    public bool IsQualified { get; set; }  // ✅ SEM DEFAULT

    [Column("QualifiedAt")]
    public DateTime? QualifiedAt { get; set; }

    [Column("IsPreferred")]
    public bool IsPreferred { get; set; }  // ✅ SEM DEFAULT

    // ==================== DADOS ADICIONAIS ====================

    [Column("AverageDeliveryTime")]
    public int? AverageDeliveryTime { get; set; }

    [Column("PaymentTermDays")]
    public int? PaymentTermDays { get; set; }

    [Column("MinimumOrderValue")]
    public decimal? MinimumOrderValue { get; set; }

    [Column("Notes")]
    public string? Notes { get; set; }

    [Column("ProductTypes")]
    public string? ProductTypes { get; set; }

    // ==================== CERTIFICAÇÕES ====================

    [Column("HasGmpCertificate")]
    public bool HasGmpCertificate { get; set; }  // ✅ SEM DEFAULT

    [Column("HasIsoCertificate")]
    public bool HasIsoCertificate { get; set; }  // ✅ SEM DEFAULT

    [Column("HasAnvisaAuthorization")]
    public bool HasAnvisaAuthorization { get; set; }  // ✅ SEM DEFAULT

    // ==================== ESTATÍSTICAS ====================

    [Column("TotalOrders")]
    public int TotalOrders { get; set; }  // ✅ SEM DEFAULT

    [Column("NonConformitiesCount")]
    public int NonConformitiesCount { get; set; }  // ✅ SEM DEFAULT

    [Column("LastOrderDate")]
    public DateTime? LastOrderDate { get; set; }

    [Column("LastEvaluationDate")]
    public DateTime? LastEvaluationDate { get; set; }

    // ==================== AUDIT ====================

    [Column("IsActive")]
    public bool IsActive { get; set; }  // ✅ SEM DEFAULT

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }  // ✅ SEM DEFAULT

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }  // ✅ SEM DEFAULT

    [Column("CreatedByEmployeeId")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("UpdatedByEmployeeId")]
    public Guid? UpdatedByEmployeeId { get; set; }

    [Column("InactivatedAt")]
    public DateTime? InactivatedAt { get; set; }

    [Column("InactivatedByEmployeeId")]
    public Guid? InactivatedByEmployeeId { get; set; }

    [Column("InactivationReason")]
    public string? InactivationReason { get; set; }

    // ==================== BLOQUEIO ====================

    [MaxLength(500)]
    [Column("BlockedReason")]
    public string? BlockedReason { get; set; }

    [Column("BlockedByEmployeeId")]
    public Guid? BlockedByEmployeeId { get; set; }

    [Column("BlockedAt")]
    public DateTime? BlockedAt { get; set; }

    // ==================== AFE E CONTROLADOS ====================

    [MaxLength(50)]
    [Column("AfeNumber")]
    public string? AfeNumber { get; set; }

    [Column("AfeExpiryDate")]
    public DateTime? AfeExpiryDate { get; set; }

    [Column("SuppliesControlled")]
    public bool SuppliesControlled { get; set; }  // ✅ SEM DEFAULT

    [Column("SuppliesAntibiotics")]
    public bool SuppliesAntibiotics { get; set; }  // ✅ SEM DEFAULT

    // ==================== NAVEGAÇÃO ====================
    

    public virtual ICollection<SupplierContact>? Contacts { get; set; }
    public virtual ICollection<SupplierCertificate>? Certificates { get; set; }
    public virtual ICollection<SupplierEvaluation>? Evaluations { get; set; }
}