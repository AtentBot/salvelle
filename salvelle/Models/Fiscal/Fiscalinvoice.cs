using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Fiscal;

/// <summary>
/// Representa uma Nota Fiscal (NF-e/NFC-e) emitida pela farmácia
/// (Cliente PAGA à Farmácia pela venda de produtos)
/// </summary>
[Table("fiscal_invoices")]
public class FiscalInvoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("establishment_id")]
    [Required]
    public Guid EstablishmentId { get; set; }

    [Column("sale_id")]
    public Guid? SaleId { get; set; }

    [Column("invoice_number")]
    [Required]
    public int InvoiceNumber { get; set; }

    [Column("series")]
    [Required]
    public int Series { get; set; }

    [Column("invoice_key")]
    [MaxLength(44)]
    public string? InvoiceKey { get; set; }

    [Column("protocol")]
    [MaxLength(50)]
    public string? Protocol { get; set; }

    [Column("invoice_type")]
    [MaxLength(20)]
    [Required]
    public string InvoiceType { get; set; } = "NFCE"; // NFCE, NFE

    [Column("total_amount")]
    [Required]
    public decimal TotalAmount { get; set; }

    [Column("issue_date")]
    [Required]
    public DateTime IssueDate { get; set; }

    [Column("authorization_date")]
    public DateTime? AuthorizationDate { get; set; }

    [Column("status")]
    [MaxLength(20)]
    [Required]
    public string Status { get; set; } = "PENDENTE"; // PENDENTE, AUTORIZADO, CANCELADO, REJEITADO

    [Column("cancellation_date")]
    public DateTime? CancellationDate { get; set; }

    [Column("cancellation_reason")]
    [MaxLength(500)]
    public string? CancellationReason { get; set; }

    [Column("xml_path")]
    [MaxLength(500)]
    public string? XmlPath { get; set; }

    [Column("pdf_path")]
    [MaxLength(500)]
    public string? PdfPath { get; set; }

    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // Navigation Properties
    [ForeignKey("SaleId")]
    public Sale? Sale { get; set; }

    [ForeignKey("EstablishmentId")]
    public Establishment? Establishment { get; set; }

    /// <summary>
    /// Itens da nota fiscal com detalhes de tributação
    /// </summary>
    public ICollection<FiscalInvoiceItem> Items { get; set; } = new List<FiscalInvoiceItem>();
}
