namespace DTOs;

public class CreateCustomerDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }

    // Endereço
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    // Informações médicas
    public string? Allergies { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Observations { get; set; }

    // LGPD
    public bool ConsentDataProcessing { get; set; } = false;
}

public class UpdateCustomerDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }

    // Endereço
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    // Informações médicas
    public string? Allergies { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Observations { get; set; }

    public string Status { get; set; } = "ATIVO";
}

public class CustomerResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Rg { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }
    public string? Gender { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }

    // Endereço
    public string? ZipCode { get; set; }
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Complement { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? FullAddress { get; set; }

    // Informações médicas
    public string? Allergies { get; set; }
    public string? MedicalConditions { get; set; }
    public string? Observations { get; set; }

    // LGPD
    public bool ConsentDataProcessing { get; set; }
    public DateTime? ConsentDate { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;
    public string? BlockReason { get; set; }

    // Auditoria
    public DateTime CreatedAt { get; set; }
    public string CreatedByEmployeeName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedByEmployeeName { get; set; }
}

public class CustomerListDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Cpf { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? City { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime LastPurchase { get; set; }
}

public class CustomerHistoryDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
    public List<CustomerOrderHistoryDto> Orders { get; set; } = new();
}

public class CustomerOrderHistoryDto
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public string FormulaName { get; set; } = string.Empty;
}

public class BlockCustomerDto
{
    public string Reason { get; set; } = string.Empty;
}
