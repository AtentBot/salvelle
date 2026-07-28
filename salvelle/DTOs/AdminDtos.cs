namespace DTOs;

public class AdminLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AdminLoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AdminDto Admin { get; set; } = new();
}

public class AdminDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class CreateAdminDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "ADMIN";
}

public class UpdateAdminDto
{
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public bool? IsActive { get; set; }
}

public class DashboardMetricsDto
{
    public int TotalEstablishments { get; set; }
    public int ActiveEstablishments { get; set; }
    public int InactiveEstablishments { get; set; }
    public int TrialingEstablishments { get; set; }
    public int PastDueEstablishments { get; set; }
    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal AnnualRecurringRevenue { get; set; }
    public int NewSubscriptionsThisMonth { get; set; }
    public decimal ChurnRate { get; set; }
    public Dictionary<string, int> SubscriptionsByPlan { get; set; } = new();
}

public class EstablishmentAdminDto
{
    public Guid Id { get; set; }
    public string NomeFantasia { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string WhatsApp { get; set; } = string.Empty;
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; }
    public bool OnboardingCompleted { get; set; }
    public SubscriptionResponseDto? Subscription { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BlockEstablishmentDto
{
    public string Reason { get; set; } = string.Empty;
}
