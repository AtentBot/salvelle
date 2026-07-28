using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Pharmacy;

/// <summary>
/// Orçamento de prescrição magistral
/// Pode ser enviado ao cliente via link público para aprovação
/// </summary>
[Table("prescription_quotes")]
public class PrescriptionQuote
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Token único para acesso público ao orçamento
    /// </summary>
    [Column("public_token")]
    [MaxLength(64)]
    public string PublicToken { get; set; } = string.Empty;

    // Relacionamentos
    [Column("prescription_id")]
    public Guid? PrescriptionId { get; set; }

    [Column("prescription_file_id")]
    public Guid? PrescriptionFileId { get; set; }

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }

    // Dados do cliente (pode não ter cadastro)
    [Column("customer_name")]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [Column("customer_phone")]
    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [Column("customer_email")]
    [MaxLength(200)]
    public string? CustomerEmail { get; set; }

    // Dados do médico
    [Column("doctor_name")]
    [MaxLength(200)]
    public string DoctorName { get; set; } = string.Empty;

    [Column("doctor_crm")]
    [MaxLength(20)]
    public string? DoctorCrm { get; set; }

    [Column("doctor_crm_state")]
    [MaxLength(2)]
    public string? DoctorCrmState { get; set; }

    [Column("doctor_specialty")]
    [MaxLength(100)]
    public string? DoctorSpecialty { get; set; }

    // Dados da fórmula
    [Column("usage_type")]
    [MaxLength(50)]
    public string UsageType { get; set; } = string.Empty; // ORAL, TOPICO, LOCAL, etc

    [Column("pharmaceutical_form")]
    [MaxLength(100)]
    public string PharmaceuticalForm { get; set; } = string.Empty; // CÁPSULAS, CREME, LOÇÃO, etc

    [Column("total_quantity")]
    [MaxLength(50)]
    public string TotalQuantity { get; set; } = string.Empty; // "50g", "90 cápsulas"

    [Column("total_quantity_numeric")]
    public decimal TotalQuantityNumeric { get; set; }

    [Column("total_quantity_unit")]
    [MaxLength(20)]
    public string TotalQuantityUnit { get; set; } = string.Empty;

    [Column("instructions")]
    public string? Instructions { get; set; }

    // Componentes (JSON)
    [Column("components_json", TypeName = "jsonb")]
    public string ComponentsJson { get; set; } = "[]";

    // Valores
    [Column("materials_cost")]
    public decimal MaterialsCost { get; set; }

    [Column("markup_percentage")]
    public decimal MarkupPercentage { get; set; } = 150; // 150% padrão

    [Column("markup_value")]
    public decimal MarkupValue { get; set; }

    [Column("labor_cost")]
    public decimal LaborCost { get; set; }

    [Column("packaging_cost")]
    public decimal PackagingCost { get; set; }

    [Column("subtotal")]
    public decimal Subtotal { get; set; }

    [Column("discount_percentage")]
    public decimal DiscountPercentage { get; set; }

    [Column("discount_value")]
    public decimal DiscountValue { get; set; }

    [Column("final_price")]
    public decimal FinalPrice { get; set; }

    // Prazos
    [Column("estimated_days")]
    public int EstimatedDays { get; set; } = 3;

    [Column("valid_until")]
    public DateTime ValidUntil { get; set; }

    // Status
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE"; // PENDENTE, APROVADO, RECUSADO, EXPIRADO, CONVERTIDO

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("approved_ip")]
    [MaxLength(50)]
    public string? ApprovedIp { get; set; }

    [Column("rejected_at")]
    public DateTime? RejectedAt { get; set; }

    [Column("rejection_reason")]
    public string? RejectionReason { get; set; }

    [Column("customer_observations")]
    public string? CustomerObservations { get; set; }

    // Venda gerada (quando convertido)
    [Column("sale_id")]
    public Guid? SaleId { get; set; }

    [Column("manipulation_order_id")]
    public Guid? ManipulationOrderId { get; set; }

    // WhatsApp
    [Column("whatsapp_sent")]
    public bool WhatsAppSent { get; set; }

    [Column("whatsapp_sent_at")]
    public DateTime? WhatsAppSentAt { get; set; }

    [Column("whatsapp_message_id")]
    [MaxLength(100)]
    public string? WhatsAppMessageId { get; set; }

    // Email
    [Column("email_sent")]
    public bool EmailSent { get; set; }

    [Column("email_sent_at")]
    public DateTime? EmailSentAt { get; set; }

    [Column("email_sent_to")]
    [MaxLength(200)]
    public string? EmailSentTo { get; set; }

    // Observações
    [Column("internal_notes")]
    public string? InternalNotes { get; set; }

    // Auditoria
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    public Guid CreatedByEmployeeId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // Views/acessos
    [Column("view_count")]
    public int ViewCount { get; set; }

    [Column("last_viewed_at")]
    public DateTime? LastViewedAt { get; set; }

    // ===== NAVEGAÇÃO =====

    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; set; }
}

/// <summary>
/// Componente do orçamento (para serialização JSON)
/// </summary>
public class QuoteComponent
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsQsp { get; set; }
    public bool IsControlled { get; set; }
    public string ControlType { get; set; } = "COMUM";
}