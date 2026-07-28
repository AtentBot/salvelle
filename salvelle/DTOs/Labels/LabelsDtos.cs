namespace DTOs.Labels;

// ============================================
// TEMPLATES
// ============================================

public class LabelTemplateResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateType { get; set; } = "PADRAO";
    public string? PharmaceuticalForm { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? CssStyles { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TemplateType { get; set; } = "PADRAO";
    public string? PharmaceuticalForm { get; set; }
    public decimal Width { get; set; } = 100;
    public decimal Height { get; set; } = 50;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string? CssStyles { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IncludeEstablishmentName { get; set; } = true;
    public bool IncludePharmacistName { get; set; } = true;
    public bool IncludeFormulaName { get; set; } = true;
    public bool IncludeComposition { get; set; } = true;
    public bool IncludePosology { get; set; } = true;
    public bool IncludeValidity { get; set; } = true;
    public bool IncludeBatchNumber { get; set; } = true;
    public bool IncludeManipulationDate { get; set; } = true;
    public bool IncludePatientName { get; set; } = true;
    public bool IncludeQrCode { get; set; } = true;
    public bool IncludeWarnings { get; set; } = true;
}

public class UpdateLabelTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? HtmlTemplate { get; set; }
    public string? CssStyles { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; } = false;
}

// ============================================
// GERAÇÃO DE RÓTULOS
// ============================================

public class GenerateLabelDto
{
    public Guid ManipulationOrderId { get; set; }
    public Guid? TemplateId { get; set; }
    public bool GenerateQrCode { get; set; } = true;
    public bool GenerateBarcode { get; set; } = true;
    public string? CustomWarnings { get; set; }
    public string? CustomStorageInstructions { get; set; }
    public LabelDataOverrideDto? Override { get; set; }
}

public class LabelDataOverrideDto
{
    public string? PatientName { get; set; }
    public string? Posology { get; set; }
    public string? Warnings { get; set; }
    public string? StorageConditions { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class GeneratedLabelResponseDto
{
    public Guid Id { get; set; }
    public string LabelCode { get; set; } = string.Empty;
    public Guid ManipulationOrderId { get; set; }
    public string ManipulationOrderCode { get; set; } = string.Empty;

    // Paciente/Prescritor
    public string? PatientName { get; set; }
    public string? PrescriberName { get; set; }
    public string? PrescriberRegistration { get; set; }

    // Produto
    public string FormulaName { get; set; } = string.Empty;
    public string? PharmaceuticalForm { get; set; }
    public string? Composition { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime ManipulationDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string? Posology { get; set; }
    public string? AdministrationRoute { get; set; }
    public string? StorageConditions { get; set; }
    public string? Warnings { get; set; }
    public string UsageType { get; set; } = "USO INTERNO";
    public bool IsControlled { get; set; }
    public string? ControlSchedule { get; set; }

    // Farmácia
    public string? PharmacyName { get; set; }
    public string? PharmacistName { get; set; }
    public string? PharmacistCrf { get; set; }

    // Códigos
    public string? QrCodeData { get; set; }
    public string? QrCodeImageUrl { get; set; }
    public string? BarcodeData { get; set; }

    // HTML
    public string? GeneratedHtml { get; set; }

    // Impressão
    public int PrintCount { get; set; }
    public DateTime? LastPrintedAt { get; set; }
    public string Status { get; set; } = "PENDENTE";
    public DateTime CreatedAt { get; set; }
}

// ============================================
// IMPRESSÃO
// ============================================

public class PrintLabelDto
{
    public int Copies { get; set; } = 1;
    public string Format { get; set; } = "HTML";
    public string? PrinterName { get; set; }
    public string PrintReason { get; set; } = "IMPRESSAO";
    public string? Notes { get; set; }
}

public class LabelPrintLogResponseDto
{
    public Guid Id { get; set; }
    public Guid GeneratedLabelId { get; set; }
    public Guid PrintedById { get; set; }
    public string PrintedByName { get; set; } = string.Empty;
    public DateTime PrintedAt { get; set; }
    public int Copies { get; set; }
    public string Format { get; set; } = "HTML";
    public string? PrinterName { get; set; }
    public string PrintReason { get; set; } = "IMPRESSAO";
    public string? Notes { get; set; }
}

// ============================================
// IMPRESSÃO EM LOTE
// ============================================

public class BatchPrintDto
{
    public List<Guid> LabelIds { get; set; } = new();
    public int Copies { get; set; } = 1;
    public string Format { get; set; } = "HTML";
    public string? PrinterName { get; set; }
}

public class BatchPrintResponseDto
{
    public int TotalRequested { get; set; }
    public int TotalPrinted { get; set; }
    public List<BatchPrintItemDto> Results { get; set; } = new();
}

public class BatchPrintItemDto
{
    public Guid LabelId { get; set; }
    public string LabelCode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

// ============================================
// VALIDAÇÃO
// ============================================

public class LabelValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public bool HasPharmacist { get; set; }
    public bool HasBatchNumber { get; set; }
    public bool HasExpiration { get; set; }
    public bool HasComposition { get; set; }
    public bool HasPrescription { get; set; }
}

// ============================================
// BUSCA
// ============================================

public class LabelSearchDto
{
    public Guid? ManipulationOrderId { get; set; }
    public string? PatientName { get; set; }
    public string? BatchNumber { get; set; }
    public string? Status { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IsControlled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}