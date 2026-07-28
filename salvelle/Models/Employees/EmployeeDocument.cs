using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Models.Employees;

namespace Models.Employees;

[Index(nameof(EmployeeId))]
[Index(nameof(DocumentType))]
[Index(nameof(ExpiryDate))]
public class EmployeeDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    // ==================== FUNCIONÁRIO ====================
    [Required]
    public Guid EmployeeId { get; set; }
    
    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    // ==================== TIPO DE DOCUMENTO ====================
    [Required, MaxLength(50)]
    public string DocumentType { get; set; } = default!;
    // CPF, RG, CTPS, ASO, ExameAdmissional, ExamePeriodico, ExameDemissional,
    // Contrato, TermoRescisao, Certificado, Diploma, CNH, Outro

    [Required, MaxLength(200)]
    public string DocumentName { get; set; } = default!;

    [MaxLength(500)]
    public string? Description { get; set; }

    // ==================== ARQUIVO ====================
    [Required, MaxLength(500)]
    public string FilePath { get; set; } = default!; // Caminho no storage/cloud

    [Required, MaxLength(100)]
    public string FileName { get; set; } = default!;

    [Required, MaxLength(50)]
    public string FileExtension { get; set; } = default!; // .pdf, .jpg, etc

    [MaxLength(100)]
    public string MimeType { get; set; } = "application/pdf";

    public long FileSizeBytes { get; set; }

    [MaxLength(64)]
    public string? FileHash { get; set; } // SHA-256 para verificação de integridade

    // ==================== VALIDADE ====================
    public DateOnly? IssueDate { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    public bool HasExpiry { get; set; } = false;

    public bool IsExpired { get; set; } = false;

    // ==================== STATUS ====================
    [Required, MaxLength(30)]
    public string Status { get; set; } = "Pendente"; // Pendente, Aprovado, Rejeitado, Vencido

    [MaxLength(500)]
    public string? StatusNotes { get; set; }

    // ==================== APROVAÇÃO ====================
    public Guid? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? RejectedBy { get; set; }

    public DateTime? RejectedAt { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // ==================== CONFIDENCIALIDADE ====================
    public bool IsConfidential { get; set; } = false;

    public bool IsEncrypted { get; set; } = false;

    // ==================== VERSÃO ====================
    public int Version { get; set; } = 1; // Controle de versão do documento

    public Guid? ReplacesDocumentId { get; set; } // ID do documento anterior (se é uma atualização)

    // ==================== AUDITORIA ====================
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? CreatedBy { get; set; }
    
    public Guid? UpdatedBy { get; set; }

    // ==================== TAGS E CATEGORIAS ====================
    [MaxLength(500)]
    public string? Tags { get; set; } // Tags separadas por vírgula para busca
}
