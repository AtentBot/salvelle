using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models.Marketplace;

[Table("pharmacy_payout_accounts")]
public class PharmacyPayoutAccount
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("establishment_id")]
    public Guid EstablishmentId { get; set; }

    [Column("stripe_connect_account_id")]
    [MaxLength(200)]
    public string? StripeConnectAccountId { get; set; }

    [Column("bank_name")]
    [MaxLength(100)]
    public string? BankName { get; set; }

    [Column("agency_number")]
    [MaxLength(20)]
    public string? AgencyNumber { get; set; }

    [Column("account_number")]
    [MaxLength(30)]
    public string? AccountNumber { get; set; }

    [Column("pix_key")]
    [MaxLength(200)]
    public string? PixKey { get; set; }

    /// <summary>
    /// PENDENTE, VERIFICADO, ATIVO, SUSPENSO
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "PENDENTE";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    [ForeignKey("EstablishmentId")]
    public virtual Establishment? Establishment { get; set; }
}
