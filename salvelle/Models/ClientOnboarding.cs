using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("client_onboarding")]
    public class ClientOnboarding
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("establishment_id")]
        [Required]
        public Guid EstablishmentId { get; set; }

        [ForeignKey("EstablishmentId")]
        public Establishment? Establishment { get; set; }

        [Column("whats_app")]
        [MaxLength(30)]
        [Required]
        public string WhatsApp { get; set; } = string.Empty;

        [Column("numero")]
        [Range(100000, 999999, ErrorMessage = "O n�mero deve conter 6 d�gitos.")]
        [Required]
        public int Numero { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("onboarding_completed")]
        public bool OnboardingCompleted { get; set; } = false;

        [Column("expires_at")]
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(30);

        [Column("is_used")]
        public bool IsUsed { get; set; } = false;
    }
}
