using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Purchasing;
using Service.Purchasing;
using Validators.Purchasing;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly PurchaseOrderService _service;

    public PurchaseOrdersController(AppDbContext context, PurchaseOrderService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? supplierId = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var query = _context.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items)
                .ThenInclude(i => i.RawMaterial)
            .Include(o => o.CreatedByEmployee)
            .Include(o => o.ApprovedByEmployee)
            .Where(o => o.EstablishmentId == establishmentId.Value);

        if (supplierId.HasValue)
            query = query.Where(o => o.SupplierId == supplierId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(o => o.Status.ToUpper() == status.ToUpper());

        if (startDate.HasValue)
            query = query.Where(o => o.OrderDate >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(o => o.OrderDate <= endDate.Value);

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new PurchaseOrderResponseDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                SupplierId = o.SupplierId,
                SupplierName = o.Supplier != null ? o.Supplier.CompanyName : "",
                OrderDate = o.OrderDate,
                ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                ActualDeliveryDate = o.ActualDeliveryDate,
                Status = o.Status,
                TotalValue = o.TotalValue,
                DiscountValue = o.DiscountValue,
                ShippingValue = o.ShippingValue,
                FinalValue = o.FinalValue,
                Notes = o.Notes,
                SupplierInvoiceNumber = o.SupplierInvoiceNumber,
                ApprovedByEmployeeName = o.ApprovedByEmployee != null ? o.ApprovedByEmployee.FullName : null,
                ApprovedAt = o.ApprovedAt,
                CreatedByEmployeeName = o.CreatedByEmployee != null ? o.CreatedByEmployee.FullName : "",
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new PurchaseOrderItemResponseDto
                {
                    Id = i.Id,
                    RawMaterialId = i.RawMaterialId,
                    RawMaterialName = i.RawMaterial != null ? i.RawMaterial.Name : "",
                    QuantityOrdered = i.QuantityOrdered,
                    QuantityReceived = i.QuantityReceived,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DiscountPercentage = i.DiscountPercentage,
                    TotalPrice = i.TotalPrice,
                    Status = i.Status,
                    Notes = i.Notes
                }).ToList()
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var order = await _context.PurchaseOrders
            .Include(o => o.Supplier)
            .Include(o => o.Items)
                .ThenInclude(i => i.RawMaterial)
            .Include(o => o.Items)
                .ThenInclude(i => i.BatchesReceived)
                    .ThenInclude(br => br.Batch)
            .Include(o => o.CreatedByEmployee)
            .Include(o => o.ApprovedByEmployee)
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId.Value)
            .Select(o => new PurchaseOrderResponseDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                SupplierId = o.SupplierId,
                SupplierName = o.Supplier != null ? o.Supplier.CompanyName : "",
                OrderDate = o.OrderDate,
                ExpectedDeliveryDate = o.ExpectedDeliveryDate,
                ActualDeliveryDate = o.ActualDeliveryDate,
                Status = o.Status,
                TotalValue = o.TotalValue,
                DiscountValue = o.DiscountValue,
                ShippingValue = o.ShippingValue,
                FinalValue = o.FinalValue,
                Notes = o.Notes,
                SupplierInvoiceNumber = o.SupplierInvoiceNumber,
                ApprovedByEmployeeName = o.ApprovedByEmployee != null ? o.ApprovedByEmployee.FullName : null,
                ApprovedAt = o.ApprovedAt,
                CreatedByEmployeeName = o.CreatedByEmployee != null ? o.CreatedByEmployee.FullName : "",
                CreatedAt = o.CreatedAt,
                Items = o.Items.Select(i => new PurchaseOrderItemResponseDto
                {
                    Id = i.Id,
                    RawMaterialId = i.RawMaterialId,
                    RawMaterialName = i.RawMaterial != null ? i.RawMaterial.Name : "",
                    QuantityOrdered = i.QuantityOrdered,
                    QuantityReceived = i.QuantityReceived,
                    Unit = i.Unit,
                    UnitPrice = i.UnitPrice,
                    DiscountPercentage = i.DiscountPercentage,
                    TotalPrice = i.TotalPrice,
                    Status = i.Status,
                    Notes = i.Notes
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(new { message = "Pedido não encontrado" });

        return Ok(order);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderDto dto)
    {
        var validator = new CreatePurchaseOrderValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message, order) = await _service.CreatePurchaseOrderAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = order!.Id },
            new { message, orderId = order.Id, orderNumber = order.OrderNumber });
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "GERENTE", "FARMACEUTICO_RT" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.ApprovePurchaseOrderAsync(
            id, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPut("{id}/send")]
    public async Task<IActionResult> Send(int id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _service.SendPurchaseOrderAsync(
            id, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("{id}/receive")]
    public async Task<IActionResult> Receive(int id, [FromBody] ReceivePurchaseOrderDto dto)
    {
        var validator = new ReceivePurchaseOrderValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var (success, message) = await _service.ReceivePurchaseOrderAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sessão inválida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento não encontrado" });

        var hasPermission = await HasPermission(employeeId.Value, new[] { "GERENTE", "FARMACEUTICO_RT" });
        if (!hasPermission)
            return Forbid();

        var (success, message) = await _service.CancelPurchaseOrderAsync(
            id, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
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