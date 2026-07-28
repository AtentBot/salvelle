using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Pharmacy;

namespace Models.Controlled;

// ========== APROVAÇÕES FARMACÊUTICAS ==========

[Table("pharmacist_approvals")]
public class PharmacistApproval
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }

    [Column("pharmacist_employee_id")]
    public Guid PharmacistEmployeeId { get; set; }

    [Column("pharmacist_name")]
    public string PharmacistName { get; set; } = "";

    [Column("pharmacist_crf")]
    public string PharmacistCrf { get; set; } = "";

    [Column("pharmacist_crf_state")]
    public string PharmacistCrfState { get; set; } = "";

    [Column("approval_type")]
    public string ApprovalType { get; set; } = "";

    [Column("approval_status")]
    public string ApprovalStatus { get; set; } = "";

    [Column("prescription_valid")]
    public bool PrescriptionValid { get; set; }

    [Column("prescription_within_validity")]
    public bool PrescriptionWithinValidity { get; set; }

    [Column("dose_within_limits")]
    public bool DoseWithinLimits { get; set; }

    [Column("no_interactions_detected")]
    public bool NoInteractionsDetected { get; set; }

    [Column("patient_data_complete")]
    public bool PatientDataComplete { get; set; }

    [Column("controlled_list_verified")]
    public string? ControlledListVerified { get; set; }

    [Column("observations")]
    public string? Observations { get; set; }

    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("digital_signature")]
    public string? DigitalSignature { get; set; }

    [Column("record_hash")]
    public string? RecordHash { get; set; }

    [Column("previous_record_hash")]
    public string? PreviousRecordHash { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ========== INVENTÁRIO DE CONTROLADOS ==========

[Table("controlled_inventory_checks")]
public class ControlledInventoryCheck
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("reference_month")]
    public int ReferenceMonth { get; set; }

    [Column("reference_year")]
    public int ReferenceYear { get; set; }

    [Column("check_date")]
    public DateTime CheckDate { get; set; }

    [Column("performed_by_employee_id")]
    public Guid PerformedByEmployeeId { get; set; }

    [Column("witnessed_by_employee_id")]
    public Guid? WitnessedByEmployeeId { get; set; }

    [Column("pharmacist_employee_id")]
    public Guid PharmacistEmployeeId { get; set; }

    [Column("status")]
    public string Status { get; set; } = "IN_PROGRESS";

    [Column("total_items_checked")]
    public int TotalItemsChecked { get; set; }

    [Column("items_ok")]
    public int ItemsOk { get; set; }

    [Column("items_with_divergence")]
    public int ItemsWithDivergence { get; set; }

    [Column("observations")]
    public string? Observations { get; set; }

    [Column("corrective_actions")]
    public string? CorrectiveActions { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<ControlledInventoryItem>? Items { get; set; }
}

[Table("controlled_inventory_items")]
public class ControlledInventoryItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("inventory_check_id")]
    public Guid InventoryCheckId { get; set; }

    [Column("raw_material_id")]
    public Guid RawMaterialId { get; set; }

    [Column("batch_id")]
    public Guid? BatchId { get; set; }

    [Column("controlled_list")]
    public string ControlledList { get; set; } = "";

    [Column("system_balance")]
    public decimal SystemBalance { get; set; }

    [Column("physical_balance")]
    public decimal PhysicalBalance { get; set; }

    [Column("unit")]
    public string Unit { get; set; } = "";

    [NotMapped]
    public decimal Divergence => PhysicalBalance - SystemBalance;

    [Column("divergence_justified")]
    public bool DivergenceJustified { get; set; }

    [Column("justification")]
    public string? Justification { get; set; }

    [Column("photo_evidence_path")]
    public string? PhotoEvidencePath { get; set; }

    [Column("adjustment_made")]
    public bool AdjustmentMade { get; set; }

    [Column("adjustment_movement_id")]
    public Guid? AdjustmentMovementId { get; set; }

    public ControlledInventoryCheck? InventoryCheck { get; set; }
}

// ========== CERTIFICAÇÕES DE FORNECEDORES ==========

[Table("supplier_certifications")]
public class SupplierCertification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("supplier_id")]
    public Guid SupplierId { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("certification_type")]
    public string CertificationType { get; set; } = "";

    [Column("certification_number")]
    public string? CertificationNumber { get; set; }

    [Column("issuing_authority")]
    public string? IssuingAuthority { get; set; }

    [Column("issue_date")]
    public DateTime IssueDate { get; set; }

    [Column("expiration_date")]
    public DateTime? ExpirationDate { get; set; }

    [Column("document_path")]
    public string? DocumentPath { get; set; }

    [Column("document_hash")]
    public string? DocumentHash { get; set; }

    [Column("status")]
    public string Status { get; set; } = "ACTIVE";

    [Column("verified_by_employee_id")]
    public Guid? VerifiedByEmployeeId { get; set; }

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    [Column("allows_controlled_substances")]
    public bool AllowsControlledSubstances { get; set; }

    [Column("controlled_lists_allowed")]
    public string? ControlledListsAllowed { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("SupplierId")]
    public Supplier? Supplier { get; set; }
}

// ========== ACESSO DE AUDITORES ==========

[Table("auditor_access_requests")]
public class AuditorAccessRequest
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("auditor_name")]
    public string AuditorName { get; set; } = "";

    [Column("auditor_document")]
    public string AuditorDocument { get; set; } = "";

    [Column("auditor_institution")]
    public string AuditorInstitution { get; set; } = "";

    [Column("auditor_credential")]
    public string? AuditorCredential { get; set; }

    [Column("requested_at")]
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    [Column("access_reason")]
    public string AccessReason { get; set; } = "";

    [Column("requested_reports")]
    public string[]? RequestedReports { get; set; }

    [Column("approved_by_employee_id")]
    public Guid? ApprovedByEmployeeId { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("access_granted")]
    public bool AccessGranted { get; set; }

    [Column("access_token")]
    public string? AccessToken { get; set; }

    [Column("access_valid_from")]
    public DateTime? AccessValidFrom { get; set; }

    [Column("access_valid_until")]
    public DateTime? AccessValidUntil { get; set; }

    [Column("last_access_at")]
    public DateTime? LastAccessAt { get; set; }

    [Column("access_count")]
    public int AccessCount { get; set; }
}

[Table("auditor_access_logs")]
public class AuditorAccessLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("auditor_access_id")]
    public Guid AuditorAccessId { get; set; }

    [Column("action_type")]
    public string ActionType { get; set; } = "";

    [Column("action_details")]
    public string? ActionDetails { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    [Column("user_agent")]
    public string? UserAgent { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("AuditorAccessId")]
    public AuditorAccessRequest? AuditorAccess { get; set; }
}

// ========== SCORES DE QUALIDADE ==========

[Table("supplier_quality_scores")]
public class SupplierQualityScore
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("supplier_id")]
    public Guid SupplierId { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("evaluation_period_start")]
    public DateTime EvaluationPeriodStart { get; set; }

    [Column("evaluation_period_end")]
    public DateTime EvaluationPeriodEnd { get; set; }

    [Column("total_deliveries")]
    public int TotalDeliveries { get; set; }

    [Column("on_time_deliveries")]
    public int OnTimeDeliveries { get; set; }

    [Column("total_batches_received")]
    public int TotalBatchesReceived { get; set; }

    [Column("batches_approved")]
    public int BatchesApproved { get; set; }

    [Column("batches_rejected")]
    public int BatchesRejected { get; set; }

    [Column("non_conformities_count")]
    public int NonConformitiesCount { get; set; }

    [Column("delivery_score")]
    public decimal? DeliveryScore { get; set; }

    [Column("quality_score")]
    public decimal? QualityScore { get; set; }

    [Column("documentation_score")]
    public decimal? DocumentationScore { get; set; }

    [Column("overall_score")]
    public decimal? OverallScore { get; set; }

    [Column("classification")]
    public string? Classification { get; set; }

    [Column("calculated_at")]
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("SupplierId")]
    public Supplier? Supplier { get; set; }
}
