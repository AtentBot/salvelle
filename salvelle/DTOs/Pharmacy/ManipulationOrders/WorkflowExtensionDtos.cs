namespace DTOs.Pharmacy.ManipulationOrders;

// ===================================================================
// DTOs - SEPARAÇÃO
// ===================================================================

public class StartSeparacaoDto
{
    public DateTime? StartTime { get; set; }
    public List<ItemSeparadoDto> Items { get; set; } = new();
    public string? AreaSeparacao { get; set; }
    public string? Observations { get; set; }
}

public class ItemSeparadoDto
{
    public Guid RawMaterialId { get; set; }
    public Guid BatchId { get; set; }
    public string BatchNumber { get; set; } = default!;
    public decimal QuantityRequired { get; set; }
    public decimal QuantitySeparated { get; set; }
    public string Unit { get; set; } = default!;
    public string? StorageLocation { get; set; }
    public bool RequiresRefrigeration { get; set; }
    public bool IsControlled { get; set; }
}

// ===================================================================
// DTOs - EXPEDIÇÃO
// ===================================================================

public class StartExpedicaoDto
{
    public DateTime? StartTime { get; set; }
    public string DeliveryMethod { get; set; } = default!;
    public string? TrackingCode { get; set; }
    public string? DeliveryPersonName { get; set; }
    public string? DeliveryPersonPhone { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverDocument { get; set; }
    public bool CustomerNotified { get; set; }
    public string? NotificationMethod { get; set; }
    public string? Observations { get; set; }
}

public class ConfirmDeliveryDto
{
    public DateTime DeliveryDate { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverDocument { get; set; }
    public string? ReceiverSignature { get; set; }
    public string? Observations { get; set; }
}

// ===================================================================
// NOTA: StartAprovacaoDto foi REMOVIDO daqui
// Usar a definição em ManipulationAprovacaoDtos.cs
// ===================================================================