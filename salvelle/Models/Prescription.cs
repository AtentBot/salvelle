using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Prescrição médica (receita)
/// </summary>
[Table("prescriptions")]
public class Prescription
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("prescription_date")]
    public DateTime PrescriptionDate { get; set; }

    [Column("expiration_date")]
    public DateTime ExpirationDate { get; set; }

    // Médico prescritor
    [Column("doctor_name")]
    [MaxLength(200)]
    public string DoctorName { get; set; } = string.Empty;

    [Column("doctor_crm")]
    [MaxLength(20)]
    public string DoctorCrm { get; set; } = string.Empty;

    [Column("doctor_crm_state")]
    [MaxLength(2)]
    public string DoctorCrmState { get; set; } = string.Empty;

    // Tipo de receita
    [Column("prescription_type")]
    [MaxLength(20)]
    public string PrescriptionType { get; set; } = "COMUM";
    // COMUM, CONTROLE_ESPECIAL, ANTIBIOTICO

    [Column("controlled_type")]
    [MaxLength(10)]
    public string? ControlledType { get; set; }
    // A1, A2, A3, B1, B2, C1, C2, C3, C4, C5

    [Column("prescription_color")]
    [MaxLength(20)]
    public string? PrescriptionColor { get; set; }
    // BRANCA, AMARELA, AZUL

    // Conteúdo da receita
    [Column("medications")]
    public string Medications { get; set; } = string.Empty;

    [Column("posology")]
    public string Posology { get; set; } = string.Empty;

    [Column("observations")]
    public string? Observations { get; set; }

    // Imagem da receita (legado - usar PrescriptionFile)
    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("image_path")]
    public string? ImagePath { get; set; }

    // Validação farmacêutica
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";
    // PENDENTE, VALIDADA, MANIPULADA, CANCELADA, EXPIRADA

    [Column("validated_at")]
    public DateTime? ValidatedAt { get; set; }

    [Column("validated_by_employee_id")]
    public Guid? ValidatedByEmployeeId { get; set; }

    [Column("validation_notes")]
    public string? ValidationNotes { get; set; }

    // Vínculo com Ordem de Manipulação
    [Column("manipulation_order_id")]
    public Guid? ManipulationOrderId { get; set; }

    [Column("manipulation_generated_at")]
    public DateTime? ManipulationGeneratedAt { get; set; }

    // Cancelamento
    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    [Column("cancelled_by_employee_id")]
    public Guid? CancelledByEmployeeId { get; set; }

    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // Navigation properties
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }

    [ForeignKey("ManipulationOrderId")]
    public virtual ManipulationOrder? ManipulationOrder { get; set; }

    // Coleção de arquivos
    public virtual ICollection<PrescriptionFile>? Files { get; set; }
}