using Microsoft.AspNetCore.Mvc;
using Data;
using Models.Employees;
using Microsoft.EntityFrameworkCore;

namespace Controllers;

/// <summary>
/// Controller MVC para views de gerenciamento de funcionários
/// Este é diferente do API EmployeesController (que está em /api/Employees)
/// Este controller serve as páginas HTML de gerenciamento
/// </summary>
public class EmployeesController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(AppDbContext db, ILogger<EmployeesController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ==================== INDEX (REDIRECIONA PARA LIST) ====================
    /// <summary>
    /// GET: /Employees
    /// Redireciona para a listagem
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    // ==================== LISTAGEM ====================
    /// <summary>
    /// GET: /Employees/List
    /// Exibe a página de listagem de funcionários
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        // Verificar se está autenticado (middleware já faz isso)
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verificar permissão para gerenciar funcionários
        if (!CanManageEmployees(employee))
        {
            TempData["ErrorMessage"] = "Você não tem permissão para gerenciar funcionários";
            return RedirectToAction("Index", "Dashboard");
        }

        _logger.LogInformation("Funcionário {EmployeeId} acessou listagem de funcionários", employee.Id);
        return View();
    }

    // ==================== CRIAR ====================
    /// <summary>
    /// GET: /Employees/Create
    /// Exibe o formulário de cadastro de novo funcionário
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!CanManageEmployees(employee))
        {
            TempData["ErrorMessage"] = "Você não tem permissão para criar funcionários";
            return RedirectToAction("Index", "Dashboard");
        }

        // Carregar dados necessários do funcionário logado
        var fullEmployee = await _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == employee.Id);

        if (fullEmployee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Passar EstablishmentId e outros dados para a view
        ViewBag.EstablishmentId = fullEmployee.EstablishmentId;
        ViewBag.EstablishmentName = fullEmployee.Establishment?.NomeFantasia;
        ViewBag.CurrentEmployeeName = fullEmployee.FullName;

        _logger.LogInformation("Funcionário {EmployeeId} acessou formulário de criação de funcionário", employee.Id);
        return View();
    }

    // ==================== DETALHES ====================
    /// <summary>
    /// GET: /Employees/Details/{id}
    /// Exibe os detalhes de um funcionário específico
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verificar se pode visualizar
        // Pode ver próprio perfil OU se for gerente/owner
        if (employee.Id != id && !CanManageEmployees(employee))
        {
            TempData["ErrorMessage"] = "Você não tem permissão para visualizar este funcionário";
            return RedirectToAction("Index", "Dashboard");
        }

        // Verificar se o funcionário existe
        var targetEmployee = await _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (targetEmployee == null)
        {
            TempData["ErrorMessage"] = "Funcionário não encontrado";
            return RedirectToAction("List");
        }

        // Verificar se é do mesmo estabelecimento
        if (targetEmployee.EstablishmentId != employee.EstablishmentId)
        {
            TempData["ErrorMessage"] = "Você não tem permissão para visualizar este funcionário";
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.IsOwnProfile = employee.Id == id;
        ViewBag.CanEdit = employee.Id == id || CanManageEmployees(employee);

        _logger.LogInformation("Funcionário {EmployeeId} visualizou detalhes do funcionário {TargetId}",
            employee.Id, id);

        return View();
    }

    // ==================== EDITAR ====================
    /// <summary>
    /// GET: /Employees/Edit/{id}
    /// Exibe o formulário de edição de funcionário
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verificar se pode editar
        // Pode editar próprio perfil OU se for gerente/owner
        if (employee.Id != id && !CanManageEmployees(employee))
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar este funcionário";
            return RedirectToAction("Index", "Dashboard");
        }

        // Verificar se o funcionário existe
        var targetEmployee = await _db.Employees
            .Include(e => e.Establishment)
            .Include(e => e.JobPosition)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (targetEmployee == null)
        {
            TempData["ErrorMessage"] = "Funcionário não encontrado";
            return RedirectToAction("List");
        }

        // Verificar se é do mesmo estabelecimento
        if (targetEmployee.EstablishmentId != employee.EstablishmentId)
        {
            TempData["ErrorMessage"] = "Você não tem permissão para editar este funcionário";
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.IsOwnProfile = employee.Id == id;
        ViewBag.CanEditAll = CanManageEmployees(employee);

        _logger.LogInformation("Funcionário {EmployeeId} acessou edição do funcionário {TargetId}",
            employee.Id, id);

        return View();
    }

    // ==================== GERAR HASH ====================
    /// <summary>
    /// GET: /Employees/GenerateHash
    /// Ferramenta administrativa para gerar hashes de senha
    /// </summary>
    [HttpGet]
    public IActionResult GenerateHash()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Apenas OWNER e MANAGER podem acessar
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        var jobPositionCode = employee.JobPosition?.Code ?? "";

        if (!allowedCodes.Contains(jobPositionCode))
        {
            _logger.LogWarning("Funcionário {EmployeeId} tentou acessar GenerateHash sem permissão",
                employee.Id);
            TempData["ErrorMessage"] = "Você não tem permissão para gerar hashes de senha";
            return RedirectToAction("Index", "Dashboard");
        }

        _logger.LogInformation("Funcionário {EmployeeId} acessou gerador de hash", employee.Id);
        return View();
    }

    // ==================== MEU PERFIL ====================
    /// <summary>
    /// GET: /Employees/Profile
    /// Redireciona para os detalhes do próprio perfil
    /// </summary>
    [HttpGet]
    public IActionResult Profile()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        return RedirectToAction("Details", new { id = employee.Id });
    }

    // ==================== ALTERAR SENHA ====================
    /// <summary>
    /// GET: /Employees/ChangePassword/{id}
    /// Exibe o formulário de alteração de senha
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ChangePassword(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Verificar se pode alterar senha
        // Pode alterar própria senha OU se for gerente/owner
        if (employee.Id != id && !CanManageEmployees(employee))
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar a senha deste funcionário";
            return RedirectToAction("Index", "Dashboard");
        }

        // Verificar se o funcionário existe
        var targetEmployee = await _db.Employees
            .Include(e => e.Establishment)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (targetEmployee == null)
        {
            TempData["ErrorMessage"] = "Funcionário não encontrado";
            return RedirectToAction("List");
        }

        // Verificar se é do mesmo estabelecimento
        if (targetEmployee.EstablishmentId != employee.EstablishmentId)
        {
            TempData["ErrorMessage"] = "Você não tem permissão para alterar a senha deste funcionário";
            return RedirectToAction("Index", "Dashboard");
        }

        ViewBag.EmployeeId = id;
        ViewBag.IsOwnProfile = employee.Id == id;

        _logger.LogInformation("Funcionário {EmployeeId} acessou alteração de senha do funcionário {TargetId}",
            employee.Id, id);

        return View();
    }

    // ==================== MÉTODOS AUXILIARES ====================

    /// <summary>
    /// Verifica se o funcionário tem permissão para gerenciar outros funcionários
    /// </summary>
    private bool CanManageEmployees(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER" };
        var jobPositionCode = employee.JobPosition?.Code ?? "";
        return allowedCodes.Contains(jobPositionCode);
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão para visualizar relatórios
    /// </summary>
    private bool CanViewReports(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "pharmacist_rt", "SUPERVISOR" };
        var jobPositionCode = employee.JobPosition?.Code ?? "";
        return allowedCodes.Contains(jobPositionCode);
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão para gerenciar inventário
    /// </summary>
    private bool CanManageInventory(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "pharmacist_rt", "SUPERVISOR", "STOCK_CONTROLLER" };
        var jobPositionCode = employee.JobPosition?.Code ?? "";
        return allowedCodes.Contains(jobPositionCode);
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão para gerenciar fórmulas
    /// </summary>
    private bool CanManageFormulas(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "pharmacist_rt", "PHARMACIST" };
        var jobPositionCode = employee.JobPosition?.Code ?? "";
        return allowedCodes.Contains(jobPositionCode);
    }

    /// <summary>
    /// Verifica se o funcionário tem permissão para gerenciar compras
    /// </summary>
    private bool CanManagePurchases(Employee employee)
    {
        var allowedCodes = new[] { "OWNER", "GENERAL_MANAGER", "pharmacist_rt", "SUPERVISOR", "PURCHASER" };
        var jobPositionCode = employee.JobPosition?.Code ?? "";
        return allowedCodes.Contains(jobPositionCode);
    }
}
