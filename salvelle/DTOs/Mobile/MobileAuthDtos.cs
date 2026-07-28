using System.ComponentModel.DataAnnotations;

namespace DTOs.Mobile;

// ==================== AUTH ====================

public class MobileRegisterRequest
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(11)]
    public string? Cpf { get; set; }
}

public class MobileLoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class MobileSocialLoginRequest
{
    [Required]
    public string Provider { get; set; } = string.Empty; // GOOGLE, APPLE

    [Required]
    public string IdToken { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? Name { get; set; }
}

public class MobileRefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}

public class MobileAuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public MobileCustomerProfile? Customer { get; set; }
    public bool RequiresEmailVerification { get; set; }
}

public class MobileVerifyEmailRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;
}

public class MobileResendVerificationRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class MobileCustomerProfile
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string LoginProvider { get; set; } = "EMAIL";
}

// ==================== FORGOT PASSWORD ====================

public class MobileForgotPasswordRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class MobileResetPasswordRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
