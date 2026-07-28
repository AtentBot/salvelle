using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Fiscal;

/// <summary>
/// Item de uma Nota Fiscal (NF-e/NFC-e)
/// </summary>
[Table("fiscal_invoice_items")]
public class FiscalInvoiceItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("fiscal_invoice_id")]
    [Required]
    public Guid FiscalInvoiceId { get; set; }

    [Column("sale_item_id")]
    public Guid? SaleItemId { get; set; }

    [Column("product_id")]
    public Guid? ProductId { get; set; }

    [Column("item_number")]
    [Required]
    public int ItemNumber { get; set; }

    [Column("description")]
    [MaxLength(500)]
    [Required]
    public string Description { get; set; } = "";

    [Column("ncm")]
    [MaxLength(8)]
    public string? Ncm { get; set; }

    [Column("cfop")]
    [MaxLength(4)]
    [Required]
    public string Cfop { get; set; } = "5102";

    [Column("unit")]
    [MaxLength(6)]
    [Required]
    public string Unit { get; set; } = "UN";

    [Column("quantity")]
    [Required]
    public decimal Quantity { get; set; }

    [Column("unit_price")]
    [Required]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    [Required]
    public decimal TotalPrice { get; set; }

    [Column("discount")]
    public decimal Discount { get; set; }

    // Tributação
    [Column("cst_icms")]
    [MaxLength(3)]
    public string? CstIcms { get; set; }

    [Column("icms_base")]
    public decimal IcmsBase { get; set; }

    [Column("icms_rate")]
    public decimal IcmsRate { get; set; }

    [Column("icms_value")]
    public decimal IcmsValue { get; set; }

    [Column("cst_pis")]
    [MaxLength(2)]
    public string? CstPis { get; set; }

    [Column("pis_base")]
    public decimal PisBase { get; set; }

    [Column("pis_rate")]
    public decimal PisRate { get; set; }

    [Column("pis_value")]
    public decimal PisValue { get; set; }

    [Column("cst_cofins")]
    [MaxLength(2)]
    public string? CstCofins { get; set; }

    [Column("cofins_base")]
    public decimal CofinsBase { get; set; }

    [Column("cofins_rate")]
    public decimal CofinsRate { get; set; }

    [Column("cofins_value")]
    public decimal CofinsValue { get; set; }

    // Navigation
    [ForeignKey("FiscalInvoiceId")]
    public FiscalInvoice? FiscalInvoice { get; set; }
}

/// <summary>
/// Configurações fiscais do estabelecimento
/// </summary>
[Table("fiscal_configs")]
public class FiscalConfig
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    [Required]
    public Guid EstablishmentId { get; set; }

    // ========== DADOS DO EMITENTE (para NF-e) ==========
    
    /// <summary>
    /// Inscrição Estadual do estabelecimento (obrigatório para NF-e)
    /// </summary>
    [Column("inscricao_estadual")]
    [MaxLength(20)]
    public string? InscricaoEstadual { get; set; }

    /// <summary>
    /// Inscrição Municipal (opcional, para serviços)
    /// </summary>
    [Column("inscricao_municipal")]
    [MaxLength(20)]
    public string? InscricaoMunicipal { get; set; }

    // ========== Certificado Digital ==========
    [Column("certificate_path")]
    [MaxLength(500)]
    public string? CertificatePath { get; set; }

    [Column("certificate_password")]
    [MaxLength(200)]
    public string? CertificatePassword { get; set; }

    [Column("certificate_expiry")]
    public DateTime? CertificateExpiry { get; set; }

    [Column("certificate_serial")]
    [MaxLength(100)]
    public string? CertificateSerial { get; set; }

    // ========== Ambiente SEFAZ ==========
    [Column("environment")]
    [MaxLength(20)]
    [Required]
    public string Environment { get; set; } = "HOMOLOGACAO";

    [Column("uf")]
    [MaxLength(2)]
    [Required]
    public string Uf { get; set; } = "SP";

    // ========== Séries ==========
    [Column("nfe_series")]
    public int NfeSeries { get; set; } = 1;

    [Column("nfe_last_number")]
    public int NfeLastNumber { get; set; } = 0;

    [Column("nfce_series")]
    public int NfceSeries { get; set; } = 1;

    [Column("nfce_last_number")]
    public int NfceLastNumber { get; set; } = 0;

    // ========== CSC para NFC-e ==========
    [Column("csc_id")]
    [MaxLength(10)]
    public string? CscId { get; set; }

    [Column("csc_token")]
    [MaxLength(100)]
    public string? CscToken { get; set; }

    // ========== Regime Tributário ==========
    [Column("tax_regime")]
    [MaxLength(20)]
    [Required]
    public string TaxRegime { get; set; } = "SIMPLES_NACIONAL";

    // ========== CFOP padrão ==========
    [Column("default_cfop_venda")]
    [MaxLength(4)]
    public string DefaultCfopVenda { get; set; } = "5102";

    [Column("default_cfop_manipulacao")]
    [MaxLength(4)]
    public string DefaultCfopManipulacao { get; set; } = "5101";

    // ========== NCM padrão para manipulados ==========
    [Column("default_ncm_manipulacao")]
    [MaxLength(8)]
    public string DefaultNcmManipulacao { get; set; } = "30049099";

    // ========== Provedor de emissão ==========
    [Column("provider")]
    [MaxLength(50)]
    public string Provider { get; set; } = "INTERNO";

    [Column("provider_api_key")]
    [MaxLength(200)]
    public string? ProviderApiKey { get; set; }

    [Column("provider_api_secret")]
    [MaxLength(200)]
    public string? ProviderApiSecret { get; set; }

    // ========== Configurações de contingência ==========
    [Column("contingency_enabled")]
    public bool ContingencyEnabled { get; set; } = true;

    [Column("contingency_mode")]
    [MaxLength(20)]
    public string? ContingencyMode { get; set; }

    // ========== Opções de impressão ==========
    [Column("print_danfe_auto")]
    public bool PrintDanfeAuto { get; set; } = true;

    [Column("danfe_logo_path")]
    [MaxLength(500)]
    public string? DanfeLogoPath { get; set; }

    // ========== Natureza da operação padrão ==========
    [Column("default_nature")]
    [MaxLength(100)]
    public string DefaultNature { get; set; } = "VENDA DE MERCADORIA";

    // ========== Informações adicionais padrão ==========
    [Column("default_additional_info")]
    [MaxLength(2000)]
    public string? DefaultAdditionalInfo { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("EstablishmentId")]
    public Establishment? Establishment { get; set; }
}

/// <summary>
/// Fila de notas para emissão/reprocessamento
/// </summary>
[Table("fiscal_queue")]
public class FiscalQueue
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    [Required]
    public Guid EstablishmentId { get; set; }

    [Column("sale_id")]
    [Required]
    public Guid SaleId { get; set; }

    [Column("invoice_type")]
    [MaxLength(10)]
    [Required]
    public string InvoiceType { get; set; } = "NFCE";

    [Column("status")]
    [MaxLength(20)]
    [Required]
    public string Status { get; set; } = "PENDENTE";

    [Column("attempts")]
    public int Attempts { get; set; } = 0;

    [Column("max_attempts")]
    public int MaxAttempts { get; set; } = 3;

    [Column("last_attempt")]
    public DateTime? LastAttempt { get; set; }

    [Column("next_attempt")]
    public DateTime? NextAttempt { get; set; }

    [Column("error_message")]
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    [Column("fiscal_invoice_id")]
    public Guid? FiscalInvoiceId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("SaleId")]
    public Sale? Sale { get; set; }

    [ForeignKey("FiscalInvoiceId")]
    public FiscalInvoice? FiscalInvoice { get; set; }
}

/// <summary>
/// Log de eventos fiscais
/// </summary>
[Table("fiscal_logs")]
public class FiscalLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    [Required]
    public Guid EstablishmentId { get; set; }

    [Column("fiscal_invoice_id")]
    public Guid? FiscalInvoiceId { get; set; }

    [Column("event_type")]
    [MaxLength(50)]
    [Required]
    public string EventType { get; set; } = "";

    [Column("event_description")]
    [MaxLength(1000)]
    public string? EventDescription { get; set; }

    [Column("request_xml")]
    public string? RequestXml { get; set; }

    [Column("response_xml")]
    public string? ResponseXml { get; set; }

    [Column("status_code")]
    [MaxLength(10)]
    public string? StatusCode { get; set; }

    [Column("status_message")]
    [MaxLength(500)]
    public string? StatusMessage { get; set; }

    [Column("ip_address")]
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Inutilização de numeração
/// </summary>
[Table("fiscal_number_gaps")]
public class FiscalNumberGap
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    [Required]
    public Guid EstablishmentId { get; set; }

    [Column("invoice_type")]
    [MaxLength(10)]
    [Required]
    public string InvoiceType { get; set; } = "NFE";

    [Column("series")]
    [Required]
    public int Series { get; set; }

    [Column("start_number")]
    [Required]
    public int StartNumber { get; set; }

    [Column("end_number")]
    [Required]
    public int EndNumber { get; set; }

    [Column("justification")]
    [MaxLength(500)]
    [Required]
    public string Justification { get; set; } = "";

    [Column("protocol")]
    [MaxLength(50)]
    public string? Protocol { get; set; }

    [Column("status")]
    [MaxLength(20)]
    [Required]
    public string Status { get; set; } = "PENDENTE";

    [Column("processed_at")]
    public DateTime? ProcessedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }
}
