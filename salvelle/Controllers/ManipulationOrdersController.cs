using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Pharmacy.ManipulationOrders;
using Models.Pharmacy;
using Models.Employees;
using DTOs.Formulas;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class ManipulationOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ManipulationOrdersController(AppDbContext context)
    {
        _context = context;
    }

    // ===================================================================
    // MÉTODOS HELPER (PRIVADOS) - CORRIGIDOS PARA USAR HttpContext.Items
    // ===================================================================

    /// <summary>
    /// Obtém o ID do estabelecimento do usuário autenticado via middleware
    /// </summary>
    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            throw new UnauthorizedAccessException("Usuário não autenticado");
        return employee.EstablishmentId;
    }

    /// <summary>
    /// Obtém o ID do funcionário autenticado via middleware
    /// </summary>
    private Guid GetEmployeeId()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            throw new UnauthorizedAccessException("Usuário não autenticado");
        return employee.Id;
    }

    /// <summary>
    /// Gera número único de ordem de manipulação
    /// </summary>
    private async Task<string> GenerateOrderNumber()
    {
        var establishmentId = GetEstablishmentId();
        var count = await _context.ManipulationOrders
            .CountAsync(o => o.EstablishmentId == establishmentId);
        var year = DateTime.UtcNow.Year;
        return $"OM-{year}-{(count + 1):D5}";
    }

    // ===================================================================
    // CRUD BÁSICO
    // ===================================================================

    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ManipulationOrderListDto>>> GetAll(
        [FromQuery] ManipulationOrderFilterDto filter)
    {
        var establishmentId = GetEstablishmentId();

        var query = _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId);

        // Filtros
        if (!string.IsNullOrEmpty(filter.OrderNumber))
            query = query.Where(o => o.OrderNumber.Contains(filter.OrderNumber));

        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(o => o.Status == filter.Status);

        if (filter.FormulaId.HasValue)
            query = query.Where(o => o.FormulaId == filter.FormulaId.Value);

        if (!string.IsNullOrEmpty(filter.CustomerName))
            query = query.Where(o => o.CustomerName.Contains(filter.CustomerName));

        if (filter.RequestedByEmployeeId.HasValue)
            query = query.Where(o => o.RequestedByEmployeeId == filter.RequestedByEmployeeId.Value);

        if (filter.ManipulatedByEmployeeId.HasValue)
            query = query.Where(o => o.ManipulatedByEmployeeId == filter.ManipulatedByEmployeeId.Value);

        if (filter.OrderDateFrom.HasValue)
            query = query.Where(o => o.OrderDate >= filter.OrderDateFrom.Value);

        if (filter.OrderDateTo.HasValue)
            query = query.Where(o => o.OrderDate <= filter.OrderDateTo.Value);

        if (filter.ExpectedDateFrom.HasValue)
            query = query.Where(o => o.ExpectedDate >= filter.ExpectedDateFrom.Value);

        if (filter.ExpectedDateTo.HasValue)
            query = query.Where(o => o.ExpectedDate <= filter.ExpectedDateTo.Value);

        if (filter.OnlyOverdue.HasValue && filter.OnlyOverdue.Value)
            query = query.Where(o => o.ExpectedDate < DateTime.UtcNow &&
                                    o.Status != "FINALIZADO" &&
                                    o.Status != "CANCELADO");

        if (filter.PassedQualityControl.HasValue)
            query = query.Where(o => o.PassedQualityControl == filter.PassedQualityControl.Value);

        if (filter.OnlySpecialControl.HasValue && filter.OnlySpecialControl.Value)
            query = query.Where(o => o.Formula!.RequiresSpecialControl);

        var totalItems = await query.CountAsync();

        // Ordenação
        query = filter.SortBy?.ToLower() switch
        {
            "ordernumber" => filter.Ascending ?
                query.OrderBy(o => o.OrderNumber) :
                query.OrderByDescending(o => o.OrderNumber),
            "customername" => filter.Ascending ?
                query.OrderBy(o => o.CustomerName) :
                query.OrderByDescending(o => o.CustomerName),
            "status" => filter.Ascending ?
                query.OrderBy(o => o.Status) :
                query.OrderByDescending(o => o.Status),
            "expecteddate" => filter.Ascending ?
                query.OrderBy(o => o.ExpectedDate) :
                query.OrderByDescending(o => o.ExpectedDate),
            "updatedat" => filter.Ascending ?
                query.OrderBy(o => o.UpdatedAt) :
                query.OrderByDescending(o => o.UpdatedAt),
            _ => filter.Ascending ?
                query.OrderBy(o => o.OrderDate) :
                query.OrderByDescending(o => o.OrderDate)
        };

        var now = DateTime.UtcNow;
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => new ManipulationOrderListDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                FormulaCode = o.Formula != null ? o.Formula.Code : null,
                FormulaName = o.Formula != null ? o.Formula.Name : null,
                PrescriptionNumber = o.PrescriptionNumber,
                CustomerName = o.CustomerName,
                CustomerPhone = o.CustomerPhone,
                QuantityToProduce = o.QuantityToProduce,
                Unit = o.Unit,
                Status = o.Status,
                OrderDate = o.OrderDate,
                ExpectedDate = o.ExpectedDate,
                CompletionDate = o.CompletionDate,
                RequestedByEmployeeName = "N/A",
                ManipulatedByEmployeeName = null,
                PassedQualityControl = o.PassedQualityControl,
                IsOverdue = o.ExpectedDate < now &&
                           o.Status != "FINALIZADO" &&
                           o.Status != "CANCELADO",
                DaysUntilExpected = (int)(o.ExpectedDate - now).TotalDays
            })
            .ToListAsync();

        return Ok(new PagedResultDto<ManipulationOrderListDto>(
            items, totalItems, filter.PageNumber, filter.PageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<ManipulationOrderDetailsDto>>> GetById(Guid id)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse<ManipulationOrderDetailsDto>.ErrorResponse(
                "Ordem não encontrada"));

        var now = DateTime.UtcNow;
        var dto = new ManipulationOrderDetailsDto
        {
            Id = order.Id,
            EstablishmentId = order.EstablishmentId,
            OrderNumber = order.OrderNumber,
            FormulaId = order.FormulaId,
            FormulaCode = order.Formula?.Code,
            FormulaName = order.Formula?.Name,
            FormulaCategory = order.Formula?.Category,
            PharmaceuticalForm = order.Formula?.PharmaceuticalForm,
            PrescriptionNumber = order.PrescriptionNumber,
            PrescriberName = order.PrescriberName,
            PrescriberRegistration = order.PrescriberRegistration,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone,
            QuantityToProduce = order.QuantityToProduce,
            Unit = order.Unit,
            SpecialInstructions = order.SpecialInstructions,
            Status = order.Status,
            OrderDate = order.OrderDate,
            ExpectedDate = order.ExpectedDate,
            StartDate = order.StartDate,
            CompletionDate = order.CompletionDate,
            ExpiryDate = order.ExpiryDate,
            PassedQualityControl = order.PassedQualityControl,
            QualityNotes = order.QualityNotes,
            IsOverdue = order.ExpectedDate < now &&
                       order.Status != "FINALIZADO" &&
                       order.Status != "CANCELADO",
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Components = order.Formula?.Components?.Select(c => new FormulaComponentDto
            {
                Id = c.Id,
                RawMaterialId = c.RawMaterialId,
                RawMaterialName = c.RawMaterial?.Name ?? "N/A",
                Quantity = c.Quantity,
                Unit = c.Unit
            }).ToList() ?? new List<FormulaComponentDto>()
        };

        return Ok(ApiResponse<ManipulationOrderDetailsDto>.SuccessResponse(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ManipulationOrderDetailsDto>>> Create(
        [FromBody] CreateManipulationOrderDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        // Gerar número de ordem se não fornecido
        if (string.IsNullOrEmpty(dto.OrderNumber))
            dto.OrderNumber = await GenerateOrderNumber();

        var order = new ManipulationOrder
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            OrderNumber = dto.OrderNumber,
            FormulaId = dto.FormulaId,
            PrescriptionNumber = dto.PrescriptionNumber,
            PrescriberName = dto.PrescriberName,
            PrescriberRegistration = dto.PrescriberRegistration,
            CustomerName = dto.CustomerName,
            CustomerPhone = dto.CustomerPhone,
            QuantityToProduce = dto.QuantityToProduce,
            Unit = dto.Unit,
            SpecialInstructions = dto.SpecialInstructions,
            Status = "PENDENTE",
            OrderDate = DateTime.UtcNow,
            ExpectedDate = dto.ExpectedDate,
            RequestedByEmployeeId = employeeId,
            PassedQualityControl = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationOrders.Add(order);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            ApiResponse<ManipulationOrderDetailsDto>.SuccessResponse(
                null!, "Ordem criada com sucesso"));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> Update(
        Guid id, [FromBody] UpdateManipulationOrderDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        if (order.Status != "PENDENTE")
            return BadRequest(ApiResponse.ErrorResponse(
                "Apenas ordens pendentes podem ser editadas"));

        order.PrescriptionNumber = dto.PrescriptionNumber;
        order.PrescriberName = dto.PrescriberName;
        order.PrescriberRegistration = dto.PrescriberRegistration;
        order.CustomerName = dto.CustomerName;
        order.CustomerPhone = dto.CustomerPhone;
        order.QuantityToProduce = dto.QuantityToProduce;
        order.Unit = dto.Unit;
        order.SpecialInstructions = dto.SpecialInstructions;
        order.ExpectedDate = dto.ExpectedDate;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Ordem atualizada com sucesso"));
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<ApiResponse>> ChangeStatus(
        Guid id, [FromBody] ChangeOrderStatusDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        var validStatuses = new[] {
            "PENDENTE", "EM_PRODUCAO", "PESAGEM", "MISTURA",
            "ENVASE", "ROTULAGEM", "CONFERENCIA", "FINALIZADO", "CANCELADO"
        };

        if (!validStatuses.Contains(dto.NewStatus))
            return BadRequest(ApiResponse.ErrorResponse("Status inválido"));

        if (order.Status == "FINALIZADO" || order.Status == "CANCELADO")
            return BadRequest(ApiResponse.ErrorResponse(
                "Não é possível alterar status de ordem finalizada ou cancelada"));

        order.Status = dto.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.NewStatus == "EM_PRODUCAO" && !order.StartDate.HasValue)
            order.StartDate = DateTime.UtcNow;

        if (dto.NewStatus == "FINALIZADO" && !order.CompletionDate.HasValue)
            order.CompletionDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse($"Status alterado para {dto.NewStatus}"));
    }

    [HttpPost("{id}/assign")]
    public async Task<ActionResult<ApiResponse>> AssignManipulator(
        Guid id, [FromBody] AssignManipulatorDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        order.ManipulatedByEmployeeId = dto.EmployeeId;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Manipulador atribuído com sucesso"));
    }

    [HttpPost("{id}/check")]
    public async Task<ActionResult<ApiResponse>> RegisterCheck(
        Guid id, [FromBody] RegisterCheckDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        order.CheckedByEmployeeId = dto.CheckedByEmployeeId;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Conferência registrada com sucesso"));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse>> Approve(
        Guid id, [FromBody] PharmaceuticalApprovalDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        order.ApprovedByPharmacistId = dto.PharmacistId;
        order.PassedQualityControl = dto.Approved;
        order.QualityNotes = dto.QualityNotes;
        order.ExpiryDate = dto.ExpiryDate;
        order.UpdatedAt = DateTime.UtcNow;

        if (dto.Approved)
        {
            order.Status = "FINALIZADO";
            if (!order.CompletionDate.HasValue)
                order.CompletionDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse(
            dto.Approved ? "Ordem aprovada" : "Ordem reprovada"));
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<ApiResponse>> Cancel(
        Guid id, [FromBody] CancelOrderDto dto)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Where(o => o.Id == id && o.EstablishmentId == establishmentId)
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse.ErrorResponse("Ordem não encontrada"));

        if (order.Status == "FINALIZADO")
            return BadRequest(ApiResponse.ErrorResponse(
                "Não é possível cancelar ordem finalizada"));

        order.Status = "CANCELADO";
        order.SpecialInstructions = (order.SpecialInstructions ?? "") +
                                   $"\n[CANCELADO] {dto.Reason}";
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(ApiResponse.SuccessResponse("Ordem cancelada com sucesso"));
    }
}