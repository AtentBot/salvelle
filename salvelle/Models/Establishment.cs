using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Core;

namespace Models;

[Index(nameof(Cnpj), IsUnique = true)]
[Index(nameof(City))]
[Index(nameof(State))]
public class Establishment
{
    [Key]
    public Guid Id { get; set; }

    // Chave estrangeira -> categoria
    [Required]
    public Guid CategoryId { get; set; }

    // Raz�o social / Nome fantasia / CNPJ
    [Required, MaxLength(200)]
    public string RazaoSocial { get; set; } = default!;

    [Required, MaxLength(200)]
    public string NomeFantasia { get; set; } = default!;

    [MaxLength(20)]
    public string? InscricaoEstadual { get; set; }    

    // CNPJ apenas d�gitos (formata��o fica para o front/DTO)
    [MaxLength(14)]
    public string? Cnpj { get; set; }  // Nullable para permitir cadastro sem CNPJ inicialmente

    // Endere�o (Brasil) - Todos opcionais para signup inicial
    [MaxLength(200)]
    public string? Street { get; set; }      // Logradouro

    [MaxLength(20)]
    public string? Number { get; set; }

    [MaxLength(120)]
    public string? Complement { get; set; }

    [MaxLength(120)]
    public string? Neighborhood { get; set; } // Bairro

    [MaxLength(120)]
    public string? City { get; set; }

    [MaxLength(2)]
    public string? State { get; set; } // UF (ex.: SP)

    [MaxLength(8)]
    public string? PostalCode { get; set; } // CEP (s� d�gitos)

    [MaxLength(60)]
    public string Country { get; set; } = "Brasil";

    // Geolocaliza��o (opcional)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Contatos
    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(20)]
    public string? WhatsApp { get; set; }

    [MaxLength(200), EmailAddress]
    public string? Email { get; set; }

    // Redes sociais
    [MaxLength(200)]
    public string? Instagram { get; set; }

    [MaxLength(200)]
    public string? Facebook { get; set; }

    [MaxLength(200)]
    public string? TikTok { get; set; }

    // Seguran�a de senha (armazenar apenas hash e metadados)
    [Required]
    public string PasswordHash { get; set; } = default!;

    public DateTime PasswordCreatedAt { get; set; }
    public DateTime? PasswordLastRehash { get; set; }

    [Required, MaxLength(40)]
    public string PasswordAlgorithm { get; set; } = "argon2id-v1";

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // ==================== NAVEGA��O ====================
    [Required]
    public Guid AccessLevelId { get; set; }

    [ForeignKey(nameof(AccessLevelId))]
    public AccessLevel? AccessLevel { get; set; }

    // Navega��o para Category
    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    // Status do estabelecimento
    public bool OnboardingCompleted { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public ICollection<Models.Pharmacy.Supplier>? Suppliers { get; set; }
        = new List<Models.Pharmacy.Supplier>();

    [Column("Subscription_status")]
    [StringLength(50)]
    public string? SubscriptionStatus { get; set; }

    [Column("TrialEndsAt")]
    public DateTime? TrialEndsAt { get; set; }

    [Column("MaxEmployeesLimit")]
    public int? MaxEmployeesLimit { get; set; }

    [Column("MaxOrdersLimit")]
    public int? MaxOrdersLimit { get; set; }

    [Column("FeaturesEnabled", TypeName = "jsonb")]
    public string? FeaturesEnabled { get; set; }

    // ==================== MARKETPLACE ====================

    [Column("IsMarketplaceActive")]
    public bool IsMarketplaceActive { get; set; } = false;

    [Column("MarketplaceDescription")]
    [MaxLength(2000)]
    public string? MarketplaceDescription { get; set; }

    [Column("LogoUrl")]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    [Column("BannerUrl")]
    [MaxLength(500)]
    public string? BannerUrl { get; set; }

    [Column("AverageRating", TypeName = "decimal(3,2)")]
    public decimal AverageRating { get; set; } = 0;

    [Column("TotalRatings")]
    public int TotalRatings { get; set; } = 0;

    [Column("DeliveryRadiusKm", TypeName = "decimal(5,1)")]
    public decimal DeliveryRadiusKm { get; set; } = 10;

    [Column("MinOrderAmount", TypeName = "decimal(10,2)")]
    public decimal MinOrderAmount { get; set; } = 0;

    [Column("AverageDeliveryMinutes")]
    public int AverageDeliveryMinutes { get; set; } = 60;

    [Column("StripeConnectAccountId")]
    [MaxLength(200)]
    public string? StripeConnectAccountId { get; set; }

    [Column("MarketplaceOpeningHours", TypeName = "jsonb")]
    public string? MarketplaceOpeningHours { get; set; }

    [Column("AcceptingOrders")]
    public bool AcceptingOrders { get; set; } = true;
}