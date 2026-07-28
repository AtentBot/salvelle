using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Models.Employees;

namespace Models.Auth;

public class LoginAttempt
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Identifier { get; set; } = string.Empty; // CPF ou WhatsApp

    public Guid? EmployeeId { get; set; }  // ? DEVE SER Guid? (nullable)

    [ForeignKey(nameof(EmployeeId))]  // ? ADICIONE ISSO
    public Employee? Employee { get; set; }  // ? ADICIONE ISSO

    public bool Success { get; set; }

    [StringLength(200)]
    public string? FailureReason { get; set; }

    [Required]
    [StringLength(100)]
    public string IpAddress { get; set; } = string.Empty;

    [StringLength(200)]
    public string? UserAgent { get; set; }

    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}