namespace DTOs.Prescriptions;

// ===== OCR RESULT (OpenAI) =====
public class OcrPrescriptionResultDto
{
    public OcrDoctorInfo Doctor { get; set; } = new();
    public OcrPatientInfo Patient { get; set; } = new();
    public string PrescriptionDate { get; set; } = string.Empty;
    public List<OcrIngredientDto> Items { get; set; } = new();
    public string Instructions { get; set; } = string.Empty;
    public string TotalQuantity { get; set; } = string.Empty;
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public double OverallConfidence { get; set; }
    public string RawText { get; set; } = string.Empty;
}

public class OcrDoctorInfo
{
    public string Name { get; set; } = string.Empty;
    public string Crm { get; set; } = string.Empty;
    public string Rqe { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string CrmState { get; set; } = string.Empty;
}

public class OcrPatientInfo
{
    public string Name { get; set; } = string.Empty;
    public string Usage { get; set; } = string.Empty;
}

// Renomeado de OcrItemDto para evitar conflito com DTOs.OcrItemDto
public class OcrIngredientDto
{
    public string Name { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool IsQsp { get; set; }
}

// ===== INGREDIENT MATCHING =====
public class IngredientMatchResultDto
{
    public string OcrText { get; set; } = string.Empty;
    public string OcrQuantity { get; set; } = string.Empty;
    public string OcrUnit { get; set; } = string.Empty;
    public List<OcrRawMaterialMatchDto> Suggestions { get; set; } = new();
    public OcrRawMaterialMatchDto? BestMatch { get; set; }
    public bool HasMatch => BestMatch != null;
}

// Renomeado de RawMaterialSuggestionDto para evitar conflito com DTOs.RawMaterialSuggestionDto
public class OcrRawMaterialMatchDto
{
    public Guid RawMaterialId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public double Confidence { get; set; }
    public bool InStock { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public bool IsControlled => ControlType != "COMUM";
}

// ===== ORÇAMENTO =====
public class CreateQuoteFromOcrDto
{
    public Guid? PrescriptionId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public OcrDoctorInfo Doctor { get; set; } = new();
    public string Usage { get; set; } = string.Empty;
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public string TotalQuantity { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<OcrConfirmedItemDto> Ingredients { get; set; } = new();
    public string? Observations { get; set; }
}

// Renomeado de ConfirmedIngredientDto para evitar conflito com DTOs.ConfirmedIngredientDto
public class OcrConfirmedItemDto
{
    public Guid RawMaterialId { get; set; }
    public string RawMaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsQsp { get; set; }
}

public class PrescriptionQuoteDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string PublicToken { get; set; } = string.Empty;
    public string PublicUrl { get; set; } = string.Empty;

    // Cliente
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }

    // Médico
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string? DoctorCrmState { get; set; }
    public string? DoctorSpecialty { get; set; }

    // Fórmula
    public string Usage { get; set; } = string.Empty;
    public string? UsageType { get; set; }
    public string PharmaceuticalForm { get; set; } = string.Empty;
    public string TotalQuantity { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;

    // Componentes
    public List<PrescriptionQuoteItemDto> Components { get; set; } = new();

    // Valores
    public decimal MaterialsCost { get; set; }
    public decimal MarkupPercentage { get; set; }
    public decimal MarkupValue { get; set; }
    public decimal LaborCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal FinalPrice { get; set; }

    // Prazos
    public int EstimatedDays { get; set; }
    public DateTime ValidUntil { get; set; }
    public bool IsExpired => DateTime.UtcNow > ValidUntil;

    // Status
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }

    // ========== NOVO: Vínculo com Ordem de Manipulação ==========
    /// <summary>
    /// ID da Ordem de Manipulação criada após aprovação do orçamento
    /// </summary>
    public Guid? ManipulationOrderId { get; set; }

    /// <summary>
    /// Código da Ordem de Manipulação (ex: OM202512190001)
    /// </summary>
    public string? ManipulationOrderCode { get; set; }

    // Farmácia
    public string PharmacyName { get; set; } = string.Empty;
    public string PharmacyPhone { get; set; } = string.Empty;
    public string PharmacyAddress { get; set; } = string.Empty;
    public string? PharmacyLogo { get; set; }
}

// Renomeado de QuoteComponentDto para evitar conflito com DTOs.QuoteComponentDto
public class PrescriptionQuoteItemDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsQsp { get; set; }
    public bool IsControlled { get; set; }
}

// ===== QUICK PROCESS =====
public class QuickProcessPrescriptionDto
{
    public string ImageBase64 { get; set; } = string.Empty;
    public string ImageType { get; set; } = "image/jpeg";
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public bool AutoCreateQuote { get; set; } = true;
    public bool SendToWhatsApp { get; set; } = false;
}

public class QuickProcessResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Etapas
    public OcrPrescriptionResultDto? OcrResult { get; set; }
    public List<IngredientMatchResultDto>? IngredientMatches { get; set; }
    public PrescriptionQuoteDto? Quote { get; set; }

    // IDs para continuação
    public Guid? PrescriptionId { get; set; }
    public Guid? PrescriptionFileId { get; set; }
    public Guid? QuoteId { get; set; }

    // Problemas encontrados
    public List<string> Warnings { get; set; } = new();
    public List<string> UnmatchedIngredients { get; set; } = new();
    public bool RequiresManualReview { get; set; }

    // Armazenar imagem para processamento posterior
    public string? ImageBase64 { get; set; }
    public string? ImageType { get; set; }
}

// ===== APROVAÇÃO =====
public class ApproveQuoteDto
{
    public string Token { get; set; } = string.Empty;
    public string? CustomerObservations { get; set; }
}

public class RejectQuoteDto
{
    public string Token { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? CustomerObservations { get; set; }
}

// ===== CONVERSÃO PARA VENDA =====
public class ConvertQuoteToSaleDto
{
    public Guid QuoteId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public int Installments { get; set; } = 1;
    public decimal AmountPaid { get; set; }
    public decimal? DiscountAmount { get; set; }
    public string? DiscountReason { get; set; }
    public string? Observations { get; set; }
}