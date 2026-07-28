using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

[Table("supplier_evaluations")]
public class SupplierEvaluation
{
    [Key]
    [Column("Id")]
    public Guid Id { get; set; }

    [Required]
    [Column("SupplierId")]
    public Guid SupplierId { get; set; }

    [Required]
    [Column("EvaluationDate")]
    public DateTime EvaluationDate { get; set; }  // ✅ SEM DEFAULT

    [MaxLength(50)]
    [Column("Period")]
    public string? Period { get; set; }

    // ==================== CRITÉRIOS DE AVALIAÇÃO (0-10) ====================

    [Column("QualityScore")]
    public decimal? QualityScore { get; set; }

    [Column("DeliveryScore")]
    public decimal? DeliveryScore { get; set; }

    [Column("PriceScore")]
    public decimal? PriceScore { get; set; }

    [Column("ServiceScore")]
    public decimal? ServiceScore { get; set; }

    [Column("DocumentationScore")]
    public decimal? DocumentationScore { get; set; }

    [Column("ComplianceScore")]
    public decimal? ComplianceScore { get; set; }

    [Column("OverallScore")]
    public decimal OverallScore { get; set; }

    [MaxLength(1)]
    [Column("Classification")]
    public string? Classification { get; set; }

    // ==================== ESTATÍSTICAS DO PERÍODO ====================

    [Column("TotalOrders")]
    public int TotalOrders { get; set; }  // ✅ SEM DEFAULT

    [Column("OnTimeDeliveries")]
    public int OnTimeDeliveries { get; set; }  // ✅ SEM DEFAULT

    [Column("LateDeliveries")]
    public int LateDeliveries { get; set; }  // ✅ SEM DEFAULT

    [Column("NonConformities")]
    public int NonConformities { get; set; }  // ✅ SEM DEFAULT

    [Column("Returns")]
    public int Returns { get; set; }  // ✅ SEM DEFAULT

    // ==================== COMENTÁRIOS ====================

    [Column("Strengths")]
    public string? Strengths { get; set; }

    [Column("Weaknesses")]
    public string? Weaknesses { get; set; }

    [Column("CorrectiveActions")]
    public string? CorrectiveActions { get; set; }

    [Column("Comments")]
    public string? Comments { get; set; }

    [MaxLength(20)]
    [Column("Recommendation")]
    public string? Recommendation { get; set; }

    [Column("IsApproved")]
    public bool IsApproved { get; set; }  // ✅ SEM DEFAULT

    // ==================== AUDIT ====================

    [Column("EvaluatedByEmployeeId")]
    public Guid? EvaluatedByEmployeeId { get; set; }

    [Column("ApprovedByEmployeeId")]
    public Guid? ApprovedByEmployeeId { get; set; }

    [Column("CreatedAt")]
    public DateTime CreatedAt { get; set; }  // ✅ SEM DEFAULT

    [Column("UpdatedAt")]
    public DateTime UpdatedAt { get; set; }  // ✅ SEM DEFAULT

    // ==================== NAVEGAÇÃO ====================

    [ForeignKey("SupplierId")]
    public virtual Supplier? Supplier { get; set; }

    // ==================== COMPUTED PROPERTIES ====================

    [NotMapped]
    public decimal OnTimeDeliveryRate => TotalOrders > 0
        ? (decimal)OnTimeDeliveries / TotalOrders * 100
        : 0;
}