using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Service;
using Validators;
using Models;
using DTOs.Sales;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SaleService _service;

    public SalesController(AppDbContext context, SaleService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? customerId = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.Sales
            .Where(s => s.EstablishmentId == establishmentId.Value);

        if (startDate.HasValue)
            query = query.Where(s => s.SaleDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.SaleDate <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(s => s.Status.ToUpper() == status.ToUpper());

        if (customerId.HasValue)
            query = query.Where(s => s.CustomerId == customerId.Value);

        var sales = await query
            .OrderByDescending(s => s.SaleDate)
            .Select(s => new SaleListDto
            {
                Id = s.Id,
                Code = s.Code,
                CustomerName = s.CustomerId.HasValue ?
                    _context.Customers
                        .Where(c => c.Id == s.CustomerId.Value)
                        .Select(c => c.FullName)
                        .FirstOrDefault() : "CLIENTE NÃO IDENTIFICADO",
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod,
                PaymentStatus = s.PaymentStatus,
                Status = s.Status,
                ItemsCount = s.Items.Count
            })
            .ToListAsync();

        return Ok(sales);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var sale = await _context.Sales
            .Include(s => s.Items)
            .Where(s => s.Id == id && s.EstablishmentId == establishmentId.Value)
            .FirstOrDefaultAsync();

        if (sale == null)
            return NotFound(new { message = "Venda não encontrada" });

        var response = new SaleResponseDto
        {
            Id = sale.Id,
            Code = sale.Code,
            CustomerId = sale.CustomerId,
            CustomerName = sale.CustomerId.HasValue ?
                await _context.Customers
                    .Where(c => c.Id == sale.CustomerId.Value)
                    .Select(c => c.FullName)
                    .FirstOrDefaultAsync() : "CLIENTE NÃO IDENTIFICADO",
            CustomerCpf = sale.CustomerId.HasValue ?
                await _context.Customers
                    .Where(c => c.Id == sale.CustomerId.Value)
                    .Select(c => c.Cpf)
                    .FirstOrDefaultAsync() : null,
            SaleDate = sale.SaleDate,
            Subtotal = sale.Subtotal,
            DiscountPercentage = sale.DiscountPercentage,
            DiscountAmount = sale.DiscountAmount,
            TotalAmount = sale.TotalAmount,
            PaymentMethod = sale.PaymentMethod,
            PaymentStatus = sale.PaymentStatus,
            PaidAmount = sale.PaidAmount,
            ChangeAmount = sale.ChangeAmount,
            PaymentDate = sale.PaymentDate,
            InvoiceNumber = sale.InvoiceNumber,
            InvoiceKey = sale.InvoiceKey,
            InvoiceStatus = sale.InvoiceStatus,
            Status = sale.Status,
            CancelledAt = sale.CancelledAt,
            CancelledByEmployeeName = "",
            CancellationReason = sale.CancellationReason,
            Observations = sale.Observations,
            CreatedAt = sale.CreatedAt,
            CreatedByEmployeeName = "",
            UpdatedAt = sale.UpdatedAt,
            UpdatedByEmployeeName = "",
            Items = sale.Items.Select(i => new SaleItemResponseDto
            {
                Id = i.Id,
                ManipulationOrderId = i.ManipulationOrderId,
                ManipulationOrderCode = null,
                PrescriptionId = i.PrescriptionId,
                PrescriptionCode = null,
                FormulaId = i.FormulaId,
                FormulaName = null,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercentage = i.DiscountPercentage,
                DiscountAmount = i.DiscountAmount,
                TotalPrice = i.TotalPrice,
                CostPrice = i.CostPrice,
                ProfitMargin = i.ProfitMargin,
                Observations = i.Observations
            }).ToList()
        };

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSaleDto dto)
    {
        var validator = new CreateSaleValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, sale) = await _service.CreateSaleAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = sale!.Id },
            new { message, saleId = sale.Id });
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelSaleDto dto)
    {
        var validator = new CancelSaleValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "GERENTE", "FARMACEUTICO_RT" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.CancelSaleAsync(
            id, dto.Reason, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("daily-report")]
    public async Task<IActionResult> GetDailyReport([FromQuery] DateTime? date = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var reportDate = date ?? DateTime.Today;
        var report = await _service.GetDailySalesReportAsync(establishmentId.Value, reportDate);

        return Ok(report);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var start = startDate ?? DateTime.Today.AddDays(-30);
        var end = endDate ?? DateTime.Today;

        var sales = await _context.Sales
            .Include(s => s.Items)
            .Where(s => s.EstablishmentId == establishmentId.Value &&
                       s.SaleDate >= start &&
                       s.SaleDate <= end &&
                       s.Status == "FINALIZADA")
            .ToListAsync();

        var stats = new
        {
            TotalSales = sales.Count,
            TotalRevenue = sales.Sum(s => s.TotalAmount),
            AverageTicket = sales.Count > 0 ? sales.Average(s => s.TotalAmount) : 0,
            TotalItems = sales.SelectMany(s => s.Items).Count(),
            TopPaymentMethod = sales.GroupBy(s => s.PaymentMethod)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .FirstOrDefault(),
            SalesByDay = sales.GroupBy(s => s.SaleDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count(), Total = g.Sum(s => s.TotalAmount) })
                .OrderBy(x => x.Date)
                .ToList()
        };

        return Ok(stats);
    }

    private Guid? GetEmployeeId()
    {
        var sessionToken = Request.Cookies["SessionId"];
        if (string.IsNullOrEmpty(sessionToken))
            return null;

        var session = _context.EmployeeSessions
            .FirstOrDefault(s => s.Token == sessionToken &&
                                s.ExpiresAt > DateTime.UtcNow &&
                                s.IsActive);

        return session?.EmployeeId;
    }

    private async Task<Guid?> GetEstablishmentId(Guid employeeId)
    {
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        return employee?.EstablishmentId;
    }

    private async Task<bool> HasPermission(Guid employeeId, string[] allowedPositions)
    {
        var employee = await _context.Employees
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employeeId);

        if (employee?.JobPosition == null)
            return false;

        return allowedPositions.Contains(employee.JobPosition.Code, StringComparer.OrdinalIgnoreCase);
    }
}