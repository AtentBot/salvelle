namespace DTOs;

public class CreatePrescriptionDto
{
    public Guid CustomerId { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string DoctorCrmState { get; set; } = string.Empty;
    public string PrescriptionType { get; set; } = "COMUM";
    public string? ControlledType { get; set; }
    public string? PrescriptionColor { get; set; }
    public string Medications { get; set; } = string.Empty;
    public string Posology { get; set; } = string.Empty;
    public string? Observations { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdatePrescriptionDto
{
    public DateTime? PrescriptionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorCrm { get; set; }
    public string? DoctorCrmState { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? PrescriptionType { get; set; }
    public string? ControlledType { get; set; }
    public string? PrescriptionColor { get; set; }
    public string? Medications { get; set; }
    public string? Posology { get; set; }
    public string? Observations { get; set; }
}

public class ValidatePrescriptionDto
{
    public bool IsValid { get; set; }
    public string? ValidationNotes { get; set; }
}

public class CancelPrescriptionDto
{
    public string Reason { get; set; } = string.Empty;
}

public class PrescriptionResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCpf { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public int DaysUntilExpiration { get; set; }
    public bool IsExpired { get; set; }

    public string DoctorName { get; set; } = string.Empty;
    public string DoctorCrm { get; set; } = string.Empty;
    public string DoctorCrmState { get; set; } = string.Empty;

    public string PrescriptionType { get; set; } = string.Empty;
    public string? ControlledType { get; set; }
    public string? PrescriptionColor { get; set; }

    public string Medications { get; set; } = string.Empty;
    public string Posology { get; set; } = string.Empty;
    public string? Observations { get; set; }

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = string.Empty;
    public DateTime? ValidatedAt { get; set; }
    public string? ValidatedByEmployeeName { get; set; }
    public string? ValidationNotes { get; set; }

    public Guid? ManipulationOrderId { get; set; }
    public string? ManipulationOrderCode { get; set; }
    public DateTime? ManipulationGeneratedAt { get; set; }

    public DateTime? CancelledAt { get; set; }
    public string? CancelledByEmployeeName { get; set; }
    public string? CancellationReason { get; set; }

    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByEmployeeName { get; set; }
}

public class PrescriptionListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime PrescriptionDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string PrescriptionType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public int DaysUntilExpiration { get; set; }
}

public class GenerateManipulationFromPrescriptionDto
{
    public Guid FormulaId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime ExpectedDate { get; set; }
    public string? AdditionalNotes { get; set; }
}

// ===== CLASSES PARA OCR E MATCHING =====
public class UploadPrescriptionFileDto
{
    public string FileBase64 { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
}

public class PrescriptionOcrResultDto
{
    public DoctorInfoDto Doctor { get; set; } = new();
    public PatientInfoDto Patient { get; set; } = new();
    public string PrescriptionDate { get; set; } = string.Empty;
    public List<OcrItemDto> Items { get; set; } = new();
    public string? Instructions { get; set; }
    public string? TotalVolume { get; set; }
    public decimal OverallConfidence { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class DoctorInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string? Crm { get; set; }
    public string? Rqe { get; set; }
    public string? Specialty { get; set; }
}

public class PatientInfoDto
{
    public string? Name { get; set; }
    public string? Usage { get; set; }
}

public class OcrItemDto
{
    public int LineNumber { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal Confidence { get; set; }

    // Alias para compatibilidade - Name = Component
    public string Name
    {
        get => Component;
        set => Component = value;
    }
}

public class IngredientMatchResponseDto
{
    public List<IngredientMatchDto> Matches { get; set; } = new();
}

public class IngredientMatchDto
{
    public string OcrText { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public List<RawMaterialSuggestionDto> Suggestions { get; set; } = new();
}

public class RawMaterialSuggestionDto
{
    public Guid RawMaterialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DciName { get; set; }
    public decimal Confidence { get; set; }
    public bool InStock { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    // Campos adicionais para compatibilidade com Service.IngredientMatcherService
    public string Code { get; set; } = string.Empty;
    public string? DcbCode { get; set; }
    public decimal UnitCost { get; set; }
    public string ControlType { get; set; } = "COMUM";
    public bool IsControlled => ControlType != "COMUM";
}

public class CreateOrderFromPrescriptionDto
{
    public Guid CustomerId { get; set; }
    public List<ConfirmedIngredientDto> ConfirmedIngredients { get; set; } = new();
    public string? Instructions { get; set; }
    public string? TotalVolume { get; set; }
}

public class ConfirmedIngredientDto
{
    public Guid RawMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class ManipulationOrderQuoteDto
{
    public Guid OrderId { get; set; }
    public List<QuoteComponentDto> Components { get; set; } = new();
    public decimal TotalCost { get; set; }
    public decimal MarkupPercentage { get; set; }
    public decimal MarkupValue { get; set; }
    public decimal Taxes { get; set; }
    public decimal FinalPrice { get; set; }
    public string ValidUntil { get; set; } = string.Empty;
    public string EstimatedDelivery { get; set; } = string.Empty;
}

public class QuoteComponentDto
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