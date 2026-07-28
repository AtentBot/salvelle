using Models.Employees;
using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

// ============================================
// TEMPLATE DE RÓTULO
// ============================================

[Table("label_templates")]
public class LabelTemplate
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("template_type")]
    [MaxLength(30)]
    public string TemplateType { get; set; } = "PADRAO";
    // PADRAO, CONTROLADO, HOMEOPATICO, FITOTERAPICO, VETERINARIO

    [Column("pharmaceutical_form")]
    [MaxLength(50)]
    public string? PharmaceuticalForm { get; set; }

    [Column("width")]
    public decimal Width { get; set; } = 100;

    [Column("height")]
    public decimal Height { get; set; } = 50;

    [Column("html_template")]
    public string HtmlTemplate { get; set; } = string.Empty;

    [Column("css_styles")]
    public string? CssStyles { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    // Configurações de inclusão de campos
    [Column("include_establishment_name")]
    public bool IncludeEstablishmentName { get; set; } = true;

    [Column("include_pharmacist_name")]
    public bool IncludePharmacistName { get; set; } = true;

    [Column("include_formula_name")]
    public bool IncludeFormulaName { get; set; } = true;

    [Column("include_composition")]
    public bool IncludeComposition { get; set; } = true;

    [Column("include_posology")]
    public bool IncludePosology { get; set; } = true;

    [Column("include_validity")]
    public bool IncludeValidity { get; set; } = true;

    [Column("include_batch_number")]
    public bool IncludeBatchNumber { get; set; } = true;

    [Column("include_manipulation_date")]
    public bool IncludeManipulationDate { get; set; } = true;

    [Column("include_patient_name")]
    public bool IncludePatientName { get; set; } = true;

    [Column("include_qr_code")]
    public bool IncludeQrCode { get; set; } = true;

    [Column("include_warnings")]
    public bool IncludeWarnings { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    [Column("updated_by_employee_id")]
    public Guid? UpdatedByEmployeeId { get; set; }

    // Navegação
    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public virtual Employee? CreatedByEmployee { get; set; }

    [ForeignKey(nameof(UpdatedByEmployeeId))]
    public virtual Employee? UpdatedByEmployee { get; set; }
}

// ============================================
// RÓTULO GERADO (SNAPSHOT)
// ============================================

[Table("generated_labels")]
public class GeneratedLabel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("label_code")]
    [MaxLength(20)]
    public string LabelCode { get; set; } = string.Empty;

    [Column("manipulation_order_id")]
    public Guid ManipulationOrderId { get; set; }

    [Column("template_id")]
    public Guid? TemplateId { get; set; }

    // Dados do Paciente/Prescritor
    [Column("patient_name")]
    [MaxLength(200)]
    public string? PatientName { get; set; }

    [Column("prescriber_name")]
    [MaxLength(200)]
    public string? PrescriberName { get; set; }

    [Column("prescriber_registration")]
    [MaxLength(50)]
    public string? PrescriberRegistration { get; set; }

    // Dados do Produto
    [Column("formula_name")]
    [MaxLength(300)]
    public string FormulaName { get; set; } = string.Empty;

    [Column("pharmaceutical_form")]
    [MaxLength(100)]
    public string? PharmaceuticalForm { get; set; }

    [Column("composition")]
    public string? Composition { get; set; }

    [Column("composition_json", TypeName = "jsonb")]
    public string? CompositionJson { get; set; }

    [Column("quantity")]
    public decimal? Quantity { get; set; }

    [Column("unit")]
    [MaxLength(20)]
    public string? Unit { get; set; }

    [Column("batch_number")]
    [MaxLength(50)]
    public string? BatchNumber { get; set; }

    [Column("manipulation_date")]
    public DateTime ManipulationDate { get; set; }

    [Column("expiration_date")]
    public DateTime ExpirationDate { get; set; }

    [Column("posology")]
    [MaxLength(500)]
    public string? Posology { get; set; }

    [Column("administration_route")]
    [MaxLength(100)]
    public string? AdministrationRoute { get; set; }

    [Column("storage_conditions")]
    [MaxLength(300)]
    public string? StorageConditions { get; set; }

    [Column("storage_instructions")]
    [MaxLength(500)]
    public string? StorageInstructions { get; set; }

    [Column("warnings")]
    [MaxLength(500)]
    public string? Warnings { get; set; }

    [Column("usage_type")]
    [MaxLength(30)]
    public string UsageType { get; set; } = "USO INTERNO";
    // USO INTERNO, USO EXTERNO, USO TÓPICO

    [Column("is_controlled")]
    public bool IsControlled { get; set; } = false;

    [Column("control_schedule")]
    [MaxLength(10)]
    public string? ControlSchedule { get; set; }
    // A1, A2, A3, B1, B2, C1, C2, C3, C4, C5

    // Dados da Farmácia
    [Column("pharmacy_name")]
    [MaxLength(200)]
    public string? PharmacyName { get; set; }

    [Column("pharmacy_cnpj")]
    [MaxLength(18)]
    public string? PharmacyCnpj { get; set; }

    [Column("pharmacy_address")]
    [MaxLength(500)]
    public string? PharmacyAddress { get; set; }

    [Column("pharmacy_phone")]
    [MaxLength(20)]
    public string? PharmacyPhone { get; set; }

    [Column("pharmacist_name")]
    [MaxLength(200)]
    public string? PharmacistName { get; set; }

    [Column("pharmacist_crf")]
    [MaxLength(20)]
    public string? PharmacistCrf { get; set; }

    [Column("pharmacist_crm")]
    [MaxLength(20)]
    public string? PharmacistCrm { get; set; }

    // Códigos
    [Column("qr_code_data")]
    [MaxLength(500)]
    public string? QrCodeData { get; set; }

    [Column("qr_code_image_url")]
    [MaxLength(500)]
    public string? QrCodeImageUrl { get; set; }

    [Column("barcode_data")]
    [MaxLength(50)]
    public string? BarcodeData { get; set; }

    // HTML Gerado
    [Column("generated_html")]
    public string? GeneratedHtml { get; set; }

    // Impressão
    [Column("print_count")]
    public int PrintCount { get; set; } = 0;

    [Column("last_printed_at")]
    public DateTime? LastPrintedAt { get; set; }

    [Column("last_printed_by_id")]
    public Guid? LastPrintedById { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";
    // PENDENTE, IMPRESSO, REIMPRESSO, CANCELADO

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by_employee_id")]
    public Guid? CreatedByEmployeeId { get; set; }

    // Navegação
    [ForeignKey(nameof(EstablishmentId))]
    public virtual Establishment? Establishment { get; set; }

    [ForeignKey(nameof(ManipulationOrderId))]
    public virtual ManipulationOrder? ManipulationOrder { get; set; }

    [ForeignKey(nameof(TemplateId))]
    public virtual LabelTemplate? Template { get; set; }

    [ForeignKey(nameof(CreatedByEmployeeId))]
    public virtual Employee? CreatedByEmployee { get; set; }

    [ForeignKey(nameof(LastPrintedById))]
    public virtual Employee? LastPrintedBy { get; set; }
}

// ============================================
// HISTÓRICO DE IMPRESSÃO
// ============================================

[Table("label_print_logs")]
public class LabelPrintLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("generated_label_id")]
    public Guid GeneratedLabelId { get; set; }

    [Column("printed_by_id")]
    public Guid PrintedById { get; set; }

    [Column("printed_at")]
    public DateTime PrintedAt { get; set; } = DateTime.UtcNow;

    [Column("copies")]
    public int Copies { get; set; } = 1;

    [Column("format")]
    [MaxLength(10)]
    public string Format { get; set; } = "HTML";
    // HTML, PDF, ZPL

    [Column("printer_name")]
    [MaxLength(100)]
    public string? PrinterName { get; set; }

    [Column("print_reason")]
    [MaxLength(30)]
    public string PrintReason { get; set; } = "IMPRESSAO";
    // IMPRESSAO, REIMPRESSAO, TESTE

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    // Navegação
    [ForeignKey(nameof(GeneratedLabelId))]
    public virtual GeneratedLabel? GeneratedLabel { get; set; }

    [ForeignKey(nameof(PrintedById))]
    public virtual Employee? PrintedBy { get; set; }
}

// ============================================
// CONFIGURAÇÕES DA EMPRESA (manter compatibilidade)
// ============================================

[Table("company_settings")]
public class CompanySettings
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("razao_social")]
    [MaxLength(200)]
    public string RazaoSocial { get; set; } = default!;

    [Column("nome_fantasia")]
    [MaxLength(200)]
    public string? NomeFantasia { get; set; }

    [Required]
    [Column("cnpj")]
    [MaxLength(18)]
    public string Cnpj { get; set; } = default!;

    [Column("inscricao_estadual")]
    [MaxLength(20)]
    public string? InscricaoEstadual { get; set; }

    [Column("inscricao_municipal")]
    [MaxLength(20)]
    public string? InscricaoMunicipal { get; set; }

    [Column("logradouro")]
    [MaxLength(200)]
    public string? Logradouro { get; set; }

    [Column("numero")]
    [MaxLength(20)]
    public string? Numero { get; set; }

    [Column("complemento")]
    [MaxLength(100)]
    public string? Complemento { get; set; }

    [Column("bairro")]
    [MaxLength(100)]
    public string? Bairro { get; set; }

    [Column("cidade")]
    [MaxLength(100)]
    public string? Cidade { get; set; }

    [Column("uf")]
    [MaxLength(2)]
    public string? Uf { get; set; }

    [Column("cep")]
    [MaxLength(9)]
    public string? Cep { get; set; }

    [NotMapped]
    public string EnderecoCompleto =>
        $"{Logradouro}, {Numero}" +
        (!string.IsNullOrEmpty(Complemento) ? $" - {Complemento}" : "") +
        $" - {Bairro}, {Cidade}/{Uf} - CEP: {Cep}";

    [Column("telefone")]
    [MaxLength(20)]
    public string? Telefone { get; set; }

    [Column("celular")]
    [MaxLength(20)]
    public string? Celular { get; set; }

    [Column("email")]
    [MaxLength(200)]
    public string? Email { get; set; }

    [Column("website")]
    [MaxLength(200)]
    public string? Website { get; set; }

    [Column("alvara_sanitario")]
    [MaxLength(50)]
    public string? AlvaraSanitario { get; set; }

    [Column("alvara_validade")]
    public DateTime? AlvaraValidade { get; set; }

    [Column("autorizacao_anvisa")]
    [MaxLength(50)]
    public string? AutorizacaoAnvisa { get; set; }

    [Column("autorizacao_especial")]
    [MaxLength(50)]
    public string? AutorizacaoEspecial { get; set; }

    [Column("logo_base64")]
    public string? LogoBase64 { get; set; }

    [Column("logo_url")]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [Column("prazo_validade_padrao_dias")]
    public int PrazoValidadePadraoDias { get; set; } = 180;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}