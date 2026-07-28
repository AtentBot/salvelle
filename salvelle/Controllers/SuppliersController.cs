using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using Models.Pharmacy;
using System.ComponentModel.DataAnnotations;

namespace Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(AppDbContext db, ILogger<SuppliersController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? classification,
        [FromQuery] bool? isQualified,
        [FromQuery] bool? afeExpiring,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        try
        {
            var query = _db.Suppliers
                .Where(s => s.EstablishmentId == employee.EstablishmentId && s.IsActive);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s =>
                    s.CompanyName.Contains(search) ||
                    (s.TradeName != null && s.TradeName.Contains(search)) ||
                    s.Cnpj.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (!string.IsNullOrEmpty(classification))
                query = query.Where(s => s.Classification == classification);

            if (isQualified.HasValue)
                query = query.Where(s => s.IsQualified == isQualified.Value);

            if (afeExpiring.HasValue && afeExpiring.Value)
            {
                var expiryDate = DateTime.UtcNow.AddDays(30);
                query = query.Where(s =>
                    s.AfeExpiryDate.HasValue &&
                    s.AfeExpiryDate.Value <= expiryDate &&
                    s.AfeExpiryDate.Value > DateTime.UtcNow);
            }

            var totalRecords = await query.CountAsync();

            var suppliers = await query
                .OrderByDescending(s => s.IsPreferred)
                .ThenByDescending(s => s.Rating)
                .ThenBy(s => s.CompanyName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new
                {
                    s.Id,
                    s.CompanyName,
                    s.TradeName,
                    s.Cnpj,
                    s.Status,
                    s.Classification,
                    s.Rating,
                    s.IsQualified,
                    s.IsPreferred,
                    s.City,
                    s.State,
                    s.Phone,
                    s.Email,
                    s.TotalOrders,
                    s.NonConformitiesCount,
                    s.LastOrderDate,
                    s.LastEvaluationDate,
                    s.CreatedAt,
                    ContactsCount = s.Contacts != null ? s.Contacts.Count(c => c.IsActive) : 0,
                    CertificatesCount = s.Certificates != null ? s.Certificates.Count(c => c.IsActive) : 0
                })
                .ToListAsync();

            return Ok(new
            {
                data = suppliers,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalRecords,
                    totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize)
                },
                filters = new { search, status, classification, isQualified, afeExpiring }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar fornecedores");
            return StatusCode(500, new { error = "Erro ao listar fornecedores" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        try
        {
            var supplier = await _db.Suppliers
                .Include(s => s.Contacts!.Where(c => c.IsActive))
                .Include(s => s.Certificates!.Where(c => c.IsActive))
                .Include(s => s.Evaluations)
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            Employee? createdByEmp = null;
            if (supplier.CreatedByEmployeeId.HasValue)
            {
                createdByEmp = await _db.Employees
                    .Include(e => e.JobPosition)
                    .FirstOrDefaultAsync(e => e.Id == supplier.CreatedByEmployeeId.Value);
            }

            return Ok(new
            {
                supplier.Id,
                supplier.CompanyName,
                supplier.TradeName,
                supplier.Cnpj,
                supplier.StateRegistration,
                supplier.MunicipalRegistration,
                address = new
                {
                    supplier.Street,
                    supplier.Number,
                    supplier.Complement,
                    supplier.Neighborhood,
                    supplier.City,
                    supplier.State,
                    supplier.PostalCode,
                    supplier.Country
                },
                contact = new
                {
                    supplier.Phone,
                    supplier.WhatsApp,
                    supplier.Email,
                    supplier.Website
                },
                supplier.Status,
                supplier.Classification,
                supplier.Rating,
                supplier.IsQualified,
                supplier.QualifiedAt,
                supplier.IsPreferred,
                supplier.AverageDeliveryTime,
                supplier.PaymentTermDays,
                supplier.MinimumOrderValue,
                supplier.Notes,
                supplier.ProductTypes,
                certifications = new
                {
                    supplier.HasGmpCertificate,
                    supplier.HasIsoCertificate,
                    supplier.HasAnvisaAuthorization
                },
                afe = new
                {
                    supplier.AfeNumber,
                    supplier.AfeExpiryDate,
                    Status = supplier.AfeExpiryDate.HasValue
                        ? (supplier.AfeExpiryDate.Value < DateTime.UtcNow ? "Vencida" :
                           supplier.AfeExpiryDate.Value < DateTime.UtcNow.AddDays(30) ? "Vencendo" : "Válida")
                        : null,
                    DaysUntilExpiry = supplier.AfeExpiryDate.HasValue
                        ? (supplier.AfeExpiryDate.Value - DateTime.UtcNow).Days
                        : (int?)null
                },
                products = new
                {
                    supplier.SuppliesControlled,
                    supplier.SuppliesAntibiotics
                },
                statistics = new
                {
                    supplier.TotalOrders,
                    supplier.NonConformitiesCount,
                    supplier.LastOrderDate,
                    supplier.LastEvaluationDate
                },
                contacts = supplier.Contacts,
                certificates = supplier.Certificates,
                evaluations = supplier.Evaluations!.OrderByDescending(e => e.EvaluationDate).Take(5),
                supplier.CreatedAt,
                supplier.UpdatedAt,
                createdBy = createdByEmp != null ? new
                {
                    createdByEmp.Id,
                    createdByEmp.FullName,
                    JobPosition = createdByEmp.JobPosition?.Name
                } : null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao buscar fornecedor" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        var cnpj = RemoveFormatting(dto.Cnpj);
        if (string.IsNullOrEmpty(cnpj) || cnpj.Length != 14)
            return BadRequest(new { error = "CNPJ inválido" });

        var existingSupplier = await _db.Suppliers
            .Where(s => s.EstablishmentId == employee.EstablishmentId && s.Cnpj == cnpj && s.IsActive)
            .FirstOrDefaultAsync();

        if (existingSupplier != null)
            return BadRequest(new { error = "Já existe um fornecedor cadastrado com este CNPJ" });

        try
        {
            var supplier = new Supplier
            {
                Id = Guid.NewGuid(),
                EstablishmentId = employee.EstablishmentId,
                CompanyName = dto.CompanyName.Trim(),
                TradeName = dto.TradeName?.Trim(),
                Cnpj = cnpj,
                StateRegistration = dto.StateRegistration?.Trim(),
                Street = dto.Street?.Trim(),
                Number = dto.Number?.Trim(),
                Complement = dto.Complement?.Trim(),
                Neighborhood = dto.Neighborhood?.Trim(),
                City = dto.City?.Trim(),
                State = dto.State?.Trim(),
                PostalCode = RemoveFormatting(dto.PostalCode),
                Country = dto.Country?.Trim() ?? "Brasil",
                Phone = dto.Phone?.Trim(),
                WhatsApp = dto.WhatsApp?.Trim(),
                Email = dto.Email?.Trim(),
                Website = dto.Website?.Trim(),
                AfeNumber = dto.AfeNumber?.Trim(),
                AfeExpiryDate = dto.AfeExpiryDate,
                SuppliesControlled = dto.SuppliesControlled,
                SuppliesAntibiotics = dto.SuppliesAntibiotics,
                Classification = dto.Classification?.Trim()?.ToUpper(),
                Rating = dto.Rating,
                IsQualified = dto.IsQualified,
                AverageDeliveryTime = dto.AverageDeliveryTime,
                PaymentTermDays = dto.PaymentTermDays,
                MinimumOrderValue = dto.MinimumOrderValue,
                ProductTypes = dto.ProductTypes?.Trim(),
                HasGmpCertificate = dto.HasGmpCertificate,
                HasIsoCertificate = dto.HasIsoCertificate,
                HasAnvisaAuthorization = dto.HasAnvisaAuthorization,
                Status = "Em Avaliação",
                Notes = dto.Notes?.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employee.Id,
                UpdatedByEmployeeId = employee.Id
            };

            _db.Suppliers.Add(supplier);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Fornecedor {Name} criado por {EmployeeName}", supplier.CompanyName, employee.FullName);

            return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, new
            {
                supplier.Id,
                supplier.CompanyName,
                supplier.TradeName,
                supplier.Cnpj,
                supplier.Status,
                supplier.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar fornecedor");
            return StatusCode(500, new { error = "Erro ao criar fornecedor" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            if (!string.IsNullOrEmpty(dto.CompanyName))
                supplier.CompanyName = dto.CompanyName.Trim();
            if (dto.TradeName != null)
                supplier.TradeName = dto.TradeName.Trim();
            if (!string.IsNullOrEmpty(dto.StateRegistration))
                supplier.StateRegistration = dto.StateRegistration.Trim();
            if (!string.IsNullOrEmpty(dto.Street))
                supplier.Street = dto.Street.Trim();
            if (!string.IsNullOrEmpty(dto.Number))
                supplier.Number = dto.Number.Trim();
            if (dto.Complement != null)
                supplier.Complement = dto.Complement.Trim();
            if (!string.IsNullOrEmpty(dto.Neighborhood))
                supplier.Neighborhood = dto.Neighborhood.Trim();
            if (!string.IsNullOrEmpty(dto.City))
                supplier.City = dto.City.Trim();
            if (!string.IsNullOrEmpty(dto.State))
                supplier.State = dto.State.Trim();
            if (!string.IsNullOrEmpty(dto.PostalCode))
                supplier.PostalCode = RemoveFormatting(dto.PostalCode);
            if (!string.IsNullOrEmpty(dto.Phone))
                supplier.Phone = dto.Phone.Trim();
            if (!string.IsNullOrEmpty(dto.WhatsApp))
                supplier.WhatsApp = dto.WhatsApp.Trim();
            if (!string.IsNullOrEmpty(dto.Email))
                supplier.Email = dto.Email.Trim();
            if (!string.IsNullOrEmpty(dto.Website))
                supplier.Website = dto.Website.Trim();
            if (!string.IsNullOrEmpty(dto.AfeNumber))
                supplier.AfeNumber = dto.AfeNumber.Trim();
            if (dto.AfeExpiryDate.HasValue)
                supplier.AfeExpiryDate = dto.AfeExpiryDate;
            if (dto.SuppliesControlled.HasValue)
                supplier.SuppliesControlled = dto.SuppliesControlled.Value;
            if (dto.SuppliesAntibiotics.HasValue)
                supplier.SuppliesAntibiotics = dto.SuppliesAntibiotics.Value;
            if (!string.IsNullOrEmpty(dto.Classification))
                supplier.Classification = dto.Classification.Trim().ToUpper();
            if (dto.Rating.HasValue)
                supplier.Rating = dto.Rating.Value;
            if (dto.IsQualified.HasValue)
                supplier.IsQualified = dto.IsQualified.Value;
            if (dto.IsPreferred.HasValue)
                supplier.IsPreferred = dto.IsPreferred.Value;
            if (dto.AverageDeliveryTime.HasValue)
                supplier.AverageDeliveryTime = dto.AverageDeliveryTime.Value;
            if (dto.PaymentTermDays.HasValue)
                supplier.PaymentTermDays = dto.PaymentTermDays.Value;
            if (dto.MinimumOrderValue.HasValue)
                supplier.MinimumOrderValue = dto.MinimumOrderValue.Value;
            if (dto.ProductTypes != null)
                supplier.ProductTypes = dto.ProductTypes.Trim();
            if (dto.HasGmpCertificate.HasValue)
                supplier.HasGmpCertificate = dto.HasGmpCertificate.Value;
            if (dto.HasIsoCertificate.HasValue)
                supplier.HasIsoCertificate = dto.HasIsoCertificate.Value;
            if (dto.HasAnvisaAuthorization.HasValue)
                supplier.HasAnvisaAuthorization = dto.HasAnvisaAuthorization.Value;
            if (dto.Notes != null)
                supplier.Notes = dto.Notes.Trim();

            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogInformation("Fornecedor {Name} atualizado por {EmployeeName}", supplier.CompanyName, employee.FullName);

            return Ok(new
            {
                supplier.Id,
                supplier.CompanyName,
                supplier.TradeName,
                supplier.Status,
                supplier.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao atualizar fornecedor" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteSupplierDto dto)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return Unauthorized(new { error = "Funcionário não autenticado" });

        if (!IsEmployeeActive(employee))
            return Unauthorized(new { error = "Funcionário inativo" });

        if (!await HasSupplierManagementPermission(employee))
            return StatusCode(403, new { error = "Sem permissão para gerenciar fornecedores" });

        try
        {
            var supplier = await _db.Suppliers
                .Where(s => s.Id == id && s.EstablishmentId == employee.EstablishmentId && s.IsActive)
                .FirstOrDefaultAsync();

            if (supplier == null)
                return NotFound(new { error = "Fornecedor não encontrado" });

            var batches = await _db.Batches
                .Where(b => b.SupplierId == id && b.Status == "APROVADO")
                .CountAsync();

            if (batches > 0)
            {
                return BadRequest(new
                {
                    error = "Não é possível desativar fornecedor com lotes ativos",
                    activeBatchesCount = batches
                });
            }

            supplier.IsActive = false;
            supplier.Status = "Inativo";
            supplier.InactivatedAt = DateTime.UtcNow;
            supplier.InactivatedByEmployeeId = employee.Id;
            supplier.InactivationReason = dto.Reason?.Trim();
            supplier.UpdatedAt = DateTime.UtcNow;
            supplier.UpdatedByEmployeeId = employee.Id;

            await _db.SaveChangesAsync();

            _logger.LogWarning("Fornecedor {Name} inativado por {EmployeeName}. Motivo: {Reason}", supplier.CompanyName, employee.FullName, dto.Reason);

            return Ok(new
            {
                message = "Fornecedor inativado com sucesso",
                supplier.Id,
                supplier.CompanyName,
                supplier.InactivatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inativar fornecedor {SupplierId}", id);
            return StatusCode(500, new { error = "Erro ao inativar fornecedor" });
        }
    }

    private bool IsEmployeeActive(Employee employee)
    {
        return employee.Status == "Ativo" &&
               (!employee.TerminationDate.HasValue || employee.TerminationDate.Value > DateOnly.FromDateTime(DateTime.UtcNow)) &&
               (!employee.LockedUntil.HasValue || employee.LockedUntil.Value <= DateTime.UtcNow);
    }

    private async Task<bool> HasSupplierManagementPermission(Employee employee)
    {
        if (employee.JobPosition == null)
        {
            await _db.Entry(employee).Reference(e => e.JobPosition).LoadAsync();
        }

        if (employee.JobPosition == null) return false;

        var allowedPositions = new[]
        {
            "FARMACÊUTICO", "FARMACÊUTICO RESPONSÁVEL", "GERENTE", "ADMINISTRADOR", "COMPRADOR"
        };

        return allowedPositions.Contains(employee.JobPosition.Name.ToUpper());
    }

    private static string RemoveFormatting(string? document)
    {
        if (string.IsNullOrEmpty(document))
            return string.Empty;

        return new string(document.Where(char.IsDigit).ToArray());
    }

    public class CreateSupplierDto
    {
        [Required(ErrorMessage = "Nome é obrigatório")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TradeName { get; set; }

        [Required(ErrorMessage = "CNPJ é obrigatório")]
        [MaxLength(18)]
        public string Cnpj { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? StateRegistration { get; set; }

        [MaxLength(100)]
        public string? Street { get; set; }

        [MaxLength(10)]
        public string? Number { get; set; }

        [MaxLength(100)]
        public string? Complement { get; set; }

        [MaxLength(100)]
        public string? Neighborhood { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? WhatsApp { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? AfeNumber { get; set; }

        public DateTime? AfeExpiryDate { get; set; }

        public bool SuppliesControlled { get; set; }
        public bool SuppliesAntibiotics { get; set; }

        [MaxLength(1)]
        public string? Classification { get; set; }

        [Range(0, 10)]
        public decimal? Rating { get; set; }

        public bool IsQualified { get; set; }
        public int? AverageDeliveryTime { get; set; }
        public int? PaymentTermDays { get; set; }
        public decimal? MinimumOrderValue { get; set; }
        public string? ProductTypes { get; set; }
        public bool HasGmpCertificate { get; set; }
        public bool HasIsoCertificate { get; set; }
        public bool HasAnvisaAuthorization { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateSupplierDto
    {
        [MaxLength(200)]
        public string? CompanyName { get; set; }

        [MaxLength(200)]
        public string? TradeName { get; set; }

        [MaxLength(20)]
        public string? StateRegistration { get; set; }

        [MaxLength(100)]
        public string? Street { get; set; }

        [MaxLength(10)]
        public string? Number { get; set; }

        [MaxLength(100)]
        public string? Complement { get; set; }

        [MaxLength(100)]
        public string? Neighborhood { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(10)]
        public string? PostalCode { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(20)]
        public string? WhatsApp { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(200)]
        public string? Website { get; set; }

        [MaxLength(50)]
        public string? AfeNumber { get; set; }

        public DateTime? AfeExpiryDate { get; set; }
        public bool? SuppliesControlled { get; set; }
        public bool? SuppliesAntibiotics { get; set; }

        [MaxLength(1)]
        public string? Classification { get; set; }

        [Range(0, 10)]
        public decimal? Rating { get; set; }

        public bool? IsQualified { get; set; }
        public bool? IsPreferred { get; set; }
        public int? AverageDeliveryTime { get; set; }
        public int? PaymentTermDays { get; set; }
        public decimal? MinimumOrderValue { get; set; }
        public string? ProductTypes { get; set; }
        public bool? HasGmpCertificate { get; set; }
        public bool? HasIsoCertificate { get; set; }
        public bool? HasAnvisaAuthorization { get; set; }
        public string? Notes { get; set; }
    }

    public class DeleteSupplierDto
    {
        [Required(ErrorMessage = "Motivo da inativação é obrigatório")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }
}
