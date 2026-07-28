using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Billing;

[Table("invoices")]
public class Invoice
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("subscription_id")]
    public Guid SubscriptionId { get; set; }

    [Column("stripe_invoice_id")]
    [StringLength(255)]
    public string? StripeInvoiceId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("currency")]
    [StringLength(3)]
    public string Currency { get; set; } = "BRL";

    [Column("status")]
    [StringLength(50)]
    public string Status { get; set; } = "PENDING";

    [Column("invoice_number")]
    [StringLength(100)]
    public string? InvoiceNumber { get; set; }

    [Column("invoice_url")]
    [StringLength(500)]
    public string? InvoiceUrl { get; set; }

    [Column("invoice_pdf_url")]
    [StringLength(500)]
    public string? InvoicePdfUrl { get; set; }

    [Column("due_date")]
    public DateTime? DueDate { get; set; }

    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("SubscriptionId")]
    public virtual Subscription? Subscription { get; set; }
}