using Data;
using Models.Security;

namespace Service;

public class AuditService
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(AppDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        string? entityType = null,
        string? entityId = null,
        string? details = null,
        Guid? userId = null,
        string? userType = null,
        string? ipAddress = null,
        Guid? establishmentId = null)
    {
        try
        {
            var log = new AuditLog
            {
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Details = details?.Length > 1000 ? details[..1000] : details,
                UserId = userId,
                UserType = userType,
                IpAddress = ipAddress,
                EstablishmentId = establishmentId,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar audit log: {Action}", action);
        }
    }

    public Task LogAsync(HttpContext httpContext, string action, string? entityType = null, string? entityId = null, string? details = null)
    {
        Guid? userId = null;
        string? userType = null;
        Guid? establishmentId = null;

        if (httpContext.Items.TryGetValue("AdminId", out var adminId) && adminId is Guid aId)
        {
            userId = aId;
            userType = "ADMIN";
        }
        else if (httpContext.Items.TryGetValue("EmployeeId", out var empId) && empId is Guid eId)
        {
            userId = eId;
            userType = "EMPLOYEE";
        }

        if (httpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid esId)
            establishmentId = esId;

        var ip = httpContext.Connection.RemoteIpAddress?.ToString();

        return LogAsync(action, entityType, entityId, details, userId, userType, ip, establishmentId);
    }
}
