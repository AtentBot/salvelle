using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

[Table("revoked_jwts")]
public class RevokedJwt
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// O claim 'jti' do JWT revogado.
    /// </summary>
    [Column("jwt_id")]
    [Required]
    [MaxLength(128)]
    public string JwtId { get; set; } = string.Empty;

    /// <summary>
    /// Quando o token original expira — permite limpeza automática da tabela.
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTime RevokedAt { get; set; } = DateTime.UtcNow;

    [Column("customer_id")]
    public Guid? CustomerId { get; set; }
}
