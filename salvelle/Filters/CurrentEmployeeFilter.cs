using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Filters;

/// <summary>
/// Preenche ViewBag.CurrentEmployee para o _Layout.cshtml em todas as actions
/// </summary>
public class CurrentEmployeeFilter : IAsyncActionFilter
{
    private readonly AppDbContext _db;

    public CurrentEmployeeFilter(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Verificar se é um Controller MVC (não API)
        if (context.Controller is Controller controller)
        {
            var employeeIdStr = context.HttpContext.Session.GetString("EmployeeId");

            if (!string.IsNullOrEmpty(employeeIdStr) && Guid.TryParse(employeeIdStr, out var employeeId))
            {
                // Primeiro tentar pegar do HttpContext.Items (se já foi carregado pelo middleware)
                var employee = context.HttpContext.Items["Employee"] as Employee;

                if (employee == null)
                {
                    // Se não está no Items, carregar do banco
                    employee = await _db.Employees
                        .Include(e => e.JobPosition)
                        .Include(e => e.Establishment)
                        .FirstOrDefaultAsync(e => e.Id == employeeId);
                }

                if (employee != null)
                {
                    // Preencher ViewBag para o _Layout.cshtml
                    controller.ViewBag.CurrentEmployee = employee;

                    // Preencher contadores de pendências para alertas do layout
                    var establishmentId = employee.EstablishmentId;

                    // Aprovações de controlados pendentes
                    controller.ViewBag.PendingControlledApprovals = await _db.ManipulationOrders
                        .CountAsync(o => o.EstablishmentId == establishmentId
                            && o.Status == "AGUARDANDO_APROVACAO_CONTROLADOS");

                    // Transmissões SNGPC pendentes
                    controller.ViewBag.PendingSngpcTransmissions = await _db.ControlledSubstanceMovements
                        .CountAsync(m => m.EstablishmentId == establishmentId && !m.SngpcSent);

                    // Certificados vencendo em 30 dias
                    var in30Days = DateTime.UtcNow.AddDays(30);
                    controller.ViewBag.ExpiringCertificates = await _db.SupplierCertificates
                        .CountAsync(c => c.Supplier!.EstablishmentId == establishmentId
                            && c.ExpiryDate <= in30Days
                            && c.Status == "Válido");
                }
            }
        }

        await next();
    }
}