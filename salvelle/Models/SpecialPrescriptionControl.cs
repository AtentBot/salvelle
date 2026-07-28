using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("special_prescription_controls")]
public class SpecialPrescriptionControl
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    // Numeração do receituário
    [Column("prescription_type")]
    [MaxLength(20)]
    public string PrescriptionType { get; set; } = string.Empty;
    // AMARELA, AZUL, BRANCA_2_VIAS

    [Column("prescription_number")]
    [MaxLength(50)]
    public string PrescriptionNumber { get; set; } = string.Empty;

    [Column("prescription_series")]
    [MaxLength(20)]
    public string? PrescriptionSeries { get; set; }

    [Column("issue_date")]
    public DateTime IssueDate { get; set; }

    [Column("validity_date")]
    public DateTime ValidityDate { get; set; }

    // Prescritor
    [Column("doctor_name")]
    [MaxLength(200)]
    public string DoctorName { get; set; } = string.Empty;

    [Column("doctor_crm")]
    [MaxLength(20)]
    public string DoctorCrm { get; set; } = string.Empty;

    [Column("doctor_crm_state")]
    [MaxLength(2)]
    public string DoctorCrmState { get; set; } = string.Empty;

    // Paciente
    [Column("patient_name")]
    [MaxLength(200)]
    public string PatientName { get; set; } = string.Empty;

    [Column("patient_document")]
    [MaxLength(20)]
    public string PatientDocument { get; set; } = string.Empty;

    [Column("patient_address")]
    [MaxLength(500)]
    public string? PatientAddress { get; set; }

    [Column("patient_city")]
    [MaxLength(100)]
    public string? PatientCity { get; set; }

    [Column("patient_state")]
    [MaxLength(2)]
    public string? PatientState { get; set; }

    // Medicamento
    [Column("medication")]
    public string Medication { get; set; } = string.Empty;

    [Column("quantity")]
    [MaxLength(100)]
    public string Quantity { get; set; } = string.Empty;

    [Column("posology")]
    public string Posology { get; set; } = string.Empty;

    // Status
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "ATIVA";
    // ATIVA, UTILIZADA, VENCIDA, CANCELADA

    [Column("used_at")]
    public DateTime? UsedAt { get; set; }

    [Column("sale_id")]
    public Guid? SaleId { get; set; }

    // Retenção
    [Column("retained")]
    public bool Retained { get; set; } = false;

    [Column("retention_reason")]
    public string? RetentionReason { get; set; }

    [Column("observations")]
    public string? Observations { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }
}