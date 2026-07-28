using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Views do PDV
/// NOTA: A API do PDV está em PDVController.cs (api/PDV)
/// Este controller serve apenas as páginas HTML
/// </summary>
[Route("pdv")]
public class PDVViewController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<PDVViewController> _logger;

    public PDVViewController(AppDbContext context, ILogger<PDVViewController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;
    
    private Guid GetEstablishmentId()
    {
        var employee = GetCurrentEmployee();
        if (employee != null) return employee.EstablishmentId;
        
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var id) && id is Guid establishmentId)
            return establishmentId;
        
        return Guid.Empty;
    }

    /// <summary>
    /// Tela principal do PDV (já existe como Index.cshtml)
    /// GET /pdv
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        ViewBag.EmployeeName = employee.FullName;
        ViewBag.EmployeePosition = employee.JobPosition?.Name ?? "";
        return View("~/Views/PDV/Index.cshtml");
    }

    /// <summary>
    /// Tela de orçamentos aprovados para conversão
    /// GET /pdv/orcamentos
    /// </summary>
    [HttpGet("orcamentos")]
    public async Task<IActionResult> Orcamentos()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();

        // Buscar orçamentos aprovados (via ManipulationOrder com status apropriado)
        var ordersAguardando = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId 
                     && (o.Status == "AGUARDANDO_APROVACAO" || o.Status == "AGUARDANDO_PRODUCAO" || o.Status == "ORCAMENTO"))
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync();

        ViewBag.EmployeeName = employee.FullName;
        return View("~/Views/PDVView/Orcamentos.cshtml", ordersAguardando);
    }

    /// <summary>
    /// Tela de venda rápida de orçamento aprovado
    /// GET /pdv/venda-rapida/{orderId}
    /// </summary>
    [HttpGet("venda-rapida/{orderId:guid}")]
    public async Task<IActionResult> VendaRapida(Guid orderId)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();

        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
        {
            TempData["Error"] = "Ordem não encontrada";
            return RedirectToAction("Orcamentos");
        }

        ViewBag.EmployeeName = employee.FullName;
        return View("~/Views/PDVView/VendaRapida.cshtml", order);
    }

    /// <summary>
    /// Tela de recibo/comprovante
    /// GET /pdv/recibo/{saleId}
    /// </summary>
    [HttpGet("recibo/{saleId:guid}")]
    public async Task<IActionResult> Recibo(Guid saleId)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();

        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.EstablishmentId == establishmentId);

        if (sale == null)
        {
            TempData["Error"] = "Venda não encontrada";
            return RedirectToAction("Index");
        }

        // Buscar dados do estabelecimento
        var establishment = await _context.Establishments.FindAsync(establishmentId);
        ViewBag.Establishment = establishment;

        // Buscar cliente se houver
        if (sale.CustomerId.HasValue)
        {
            var customer = await _context.Customers.FindAsync(sale.CustomerId.Value);
            ViewBag.Customer = customer;
        }

        // Buscar pagamentos
        var payments = await _context.SalePayments
            .Where(p => p.SaleId == saleId)
            .ToListAsync();
        ViewBag.Payments = payments;

        return View("~/Views/PDVView/Recibo.cshtml", sale);
    }

    /// <summary>
    /// Tela de histórico de vendas do dia
    /// GET /pdv/historico
    /// </summary>
    [HttpGet("historico")]
    public async Task<IActionResult> Historico([FromQuery] DateTime? date = null)
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();
        var targetDate = date ?? DateTime.UtcNow.Date;

        var vendas = await _context.Sales
            .Include(s => s.Items)
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate.Date == targetDate)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        // Buscar nomes dos clientes
        var customerIds = vendas.Where(v => v.CustomerId.HasValue).Select(v => v.CustomerId!.Value).Distinct();
        var customers = await _context.Customers
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.FullName);
        ViewBag.Customers = customers;

        ViewBag.EmployeeName = employee.FullName;
        ViewBag.SelectedDate = targetDate;
        ViewBag.TotalVendas = vendas.Count(v => v.Status == "FINALIZADA");
        ViewBag.TotalFaturamento = vendas.Where(v => v.Status == "FINALIZADA").Sum(v => v.TotalAmount);

        return View("~/Views/PDVView/Historico.cshtml", vendas);
    }

    /// <summary>
    /// Tela de fechamento de caixa
    /// GET /pdv/fechamento
    /// </summary>
    [HttpGet("fechamento")]
    public async Task<IActionResult> Fechamento()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        // Verificar permissão
        var jobCode = employee.JobPosition?.Code?.ToUpper();
        if (jobCode != "GERENTE" && jobCode != "MANAGER" && jobCode != "PROPRIETARIO" && 
            jobCode != "OWNER" && jobCode != "SUPERVISOR" && jobCode != "FARMACEUTICO_RT")
        {
            TempData["Error"] = "Você não tem permissão para acessar o fechamento de caixa";
            return RedirectToAction("Index");
        }

        var establishmentId = GetEstablishmentId();

        // Buscar caixa aberto
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(cr => cr.EstablishmentId == establishmentId && cr.Status == "ABERTO");

        if (cashRegister == null)
        {
            TempData["Warning"] = "Nenhum caixa aberto";
            return RedirectToAction("Index");
        }

        // Buscar movimentações do dia
        var movements = await _context.CashMovements
            .Where(cm => cm.CashRegisterId == cashRegister.Id)
            .OrderByDescending(cm => cm.MovementDate)
            .ToListAsync();

        ViewBag.CashRegister = cashRegister;
        ViewBag.Movements = movements;
        ViewBag.EmployeeName = employee.FullName;

        // Totais por forma de pagamento
        ViewBag.TotalDinheiro = movements.Where(m => m.PaymentMethod == "DINHEIRO" && m.MovementType == "ENTRADA").Sum(m => m.Amount);
        ViewBag.TotalCartao = movements.Where(m => (m.PaymentMethod == "CARTAO_CREDITO" || m.PaymentMethod == "CARTAO_DEBITO") && m.MovementType == "ENTRADA").Sum(m => m.Amount);
        ViewBag.TotalPix = movements.Where(m => m.PaymentMethod == "PIX" && m.MovementType == "ENTRADA").Sum(m => m.Amount);
        ViewBag.TotalSaidas = movements.Where(m => m.MovementType == "SAIDA" || m.MovementType == "SANGRIA").Sum(m => m.Amount);

        return View("~/Views/PDVView/Fechamento.cshtml");
    }

    /// <summary>
    /// Tela de abertura de caixa
    /// GET /pdv/abrir-caixa
    /// </summary>
    [HttpGet("abrir-caixa")]
    public async Task<IActionResult> AbrirCaixa()
    {
        var employee = GetCurrentEmployee();
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var establishmentId = GetEstablishmentId();

        // Verificar se já existe caixa aberto
        var cashRegister = await _context.CashRegisters
            .FirstOrDefaultAsync(cr => cr.EstablishmentId == establishmentId && cr.Status == "ABERTO");

        if (cashRegister != null)
        {
            TempData["Info"] = "Já existe um caixa aberto";
            return RedirectToAction("Index");
        }

        ViewBag.EmployeeName = employee.FullName;
        return View("~/Views/PDVView/AbrirCaixa.cshtml");
    }
}
