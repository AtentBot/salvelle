using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

/// <summary>
/// Arquivo de imagem/PDF da receita médica para processamento OCR
/// </summary>
[Table("prescription_files")]
public class PrescriptionFile
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("prescription_id")]
    public Guid PrescriptionId { get; set; }

    [Required]
    [MaxLength(255)]
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    [Column("file_type")]
    public string FileType { get; set; } = string.Empty;
    // image/jpeg, image/png, application/pdf

    [Column("file_url")]
    public string? FileUrl { get; set; }

    [Column("file_base64")]
    public string? FileBase64 { get; set; }

    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    [Required]
    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Column("uploaded_by_employee_id")]
    public Guid? UploadedByEmployeeId { get; set; }

    // OCR - Processamento com OpenAI Vision
    [Required]
    [MaxLength(20)]
    [Column("ocr_status")]
    public string OcrStatus { get; set; } = "PENDING";
    // PENDING, PROCESSING, COMPLETED, FAILED

    [Column("ocr_processed_at")]
    public DateTime? OcrProcessedAt { get; set; }

    [Column("ocr_result", TypeName = "jsonb")]
    public string? OcrResult { get; set; } // JSON com resultado estruturado

    [Column("ocr_confidence")]
    public decimal? OcrConfidence { get; set; } // 0.00 a 100.00

    [Column("ocr_error_message")]
    public string? OcrErrorMessage { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("PrescriptionId")]
    public virtual Prescription? Prescription { get; set; }
}