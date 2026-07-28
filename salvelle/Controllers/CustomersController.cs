using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using Service;
using Validators;
using Models;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly CustomerService _service;

    private static string MaskCpf(string? cpf)
    {
        if (string.IsNullOrEmpty(cpf)) return "";
        var digits = cpf.Replace(".", "").Replace("-", "").Replace(" ", "");
        if (digits.Length < 4) return "***";
        return $"***.***.*{digits[^4..^2]}-{digits[^2..]}";
    }

    public CustomersController(AppDbContext context, CustomerService service)
    {
        _context = context;
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
    [FromQuery] string? status = null,
    [FromQuery] string? search = null)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var query = _context.Set<Customer>()
            .Where(c => c.EstablishmentId == establishmentId.Value);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status.ToUpper() == status.ToUpper());

        // CORRE��O: Usar ILike para busca case-insensitive no PostgreSQL
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = $"%{search}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.FullName, searchPattern) ||
                (c.Cpf != null && EF.Functions.ILike(c.Cpf, searchPattern)) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, searchPattern)) ||
                (c.Email != null && EF.Functions.ILike(c.Email, searchPattern)));
        }

        var customers = await query
            .OrderBy(c => c.FullName)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                Code = c.Code,
                FullName = c.FullName,
                Cpf = c.Cpf,
                Phone = c.Phone,
                WhatsApp = c.WhatsApp,
                City = c.City,
                Status = c.Status,
                LastPurchase = DateTime.MinValue
            })
            .ToListAsync();

        // Mascarar CPF em listagens
        foreach (var c in customers)
            c.Cpf = MaskCpf(c.Cpf);

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var customer = await _context.Set<Customer>()
            .Include(c => c.CreatedByEmployee)
            .Include(c => c.UpdatedByEmployee)
            .Where(c => c.Id == id && c.EstablishmentId == establishmentId.Value)
            .Select(c => new CustomerResponseDto
            {
                Id = c.Id,
                Code = c.Code,
                FullName = c.FullName,
                Cpf = c.Cpf,
                Rg = c.Rg,
                BirthDate = c.BirthDate,
                Age = c.BirthDate.HasValue ?
                    (int)((DateTime.Today - c.BirthDate.Value).TotalDays / 365.25) : null,
                Gender = c.Gender,
                Phone = c.Phone,
                WhatsApp = c.WhatsApp,
                Email = c.Email,
                ZipCode = c.ZipCode,
                Street = c.Street,
                Number = c.Number,
                Complement = c.Complement,
                Neighborhood = c.Neighborhood,
                City = c.City,
                State = c.State,
                FullAddress = !string.IsNullOrEmpty(c.Street) ?
                    $"{c.Street}, {c.Number} - {c.Neighborhood}, {c.City}/{c.State}" : null,
                Allergies = c.Allergies,
                MedicalConditions = c.MedicalConditions,
                Observations = c.Observations,
                ConsentDataProcessing = c.ConsentDataProcessing,
                ConsentDate = c.ConsentDate,
                Status = c.Status,
                BlockReason = c.BlockReason,
                CreatedAt = c.CreatedAt,
                CreatedByEmployeeName = c.CreatedByEmployee != null ? c.CreatedByEmployee.FullName : "",
                UpdatedAt = c.UpdatedAt,
                UpdatedByEmployeeName = c.UpdatedByEmployee != null ? c.UpdatedByEmployee.FullName : null
            })
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound(new { message = "Cliente n�o encontrado" });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        var validator = new CreateCustomerValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var (success, message, customer) = await _service.CreateCustomerAsync(
            dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = customer!.Id },
            new { message, customerId = customer.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        var validator = new UpdateCustomerValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var (success, message) = await _service.UpdateCustomerAsync(
            id, dto, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId.Value);

        if (customer == null)
            return NotFound(new { message = "Cliente n�o encontrado" });

        customer.Status = "INATIVO";
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedByEmployeeId = employeeId.Value;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Cliente desativado com sucesso" });
    }

    [HttpPost("{id}/block")]
    public async Task<IActionResult> Block(Guid id, [FromBody] BlockCustomerDto dto)
    {
        var validator = new BlockCustomerValidator();
        var validationResult = await validator.ValidateAsync(dto);

        if (!validationResult.IsValid)
            return BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });

        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var (success, message) = await _service.BlockCustomerAsync(
            id, dto.Reason, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpPost("{id}/unblock")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var (success, message) = await _service.UnblockCustomerAsync(
            id, establishmentId.Value, employeeId.Value);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        if (string.IsNullOrWhiteSpace(query))
            return BadRequest(new { message = "Termo de busca � obrigat�rio" });

        query = query.ToUpper().Replace(".", "").Replace("-", "");

        var customers = await _context.Set<Customer>()
            .Where(c => c.EstablishmentId == establishmentId.Value &&
                       (c.FullName.Contains(query) ||
                        (c.Cpf != null && c.Cpf.Contains(query)) ||
                        (c.Phone != null && c.Phone.Contains(query))))
            .Take(10)
            .Select(c => new
            {
                c.Id,
                c.Code,
                c.FullName,
                Cpf = c.Cpf,
                c.Phone,
                c.Status
            })
            .ToListAsync();

        var masked = customers.Select(c => new
        {
            c.Id, c.Code, c.FullName,
            Cpf = MaskCpf(c.Cpf),
            c.Phone, c.Status
        });

        return Ok(masked);
    }

    [HttpGet("{id}/history")]
    public async Task<IActionResult> GetHistory(Guid id)
    {
        var employeeId = GetEmployeeId();
        if (!employeeId.HasValue)
            return Unauthorized(new { message = "Sess�o inv�lida" });

        var establishmentId = await GetEstablishmentId(employeeId.Value);
        if (!establishmentId.HasValue)
            return NotFound(new { message = "Estabelecimento n�o encontrado" });

        var customer = await _context.Set<Customer>()
            .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId.Value);

        if (customer == null)
            return NotFound(new { message = "Cliente n�o encontrado" });

        // TODO: Implementar quando tiver m�dulo de vendas
        var history = new CustomerHistoryDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            TotalOrders = 0,
            TotalSpent = 0,
            LastOrderDate = null,
            Orders = new List<CustomerOrderHistoryDto>()
        };

        return Ok(history);
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
}
