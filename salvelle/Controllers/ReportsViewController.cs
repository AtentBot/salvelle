using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;

namespace Controllers;

/// <summary>
/// Controller MVC para Views de Relatórios e Dashboard Executivo
/// Rotas: /Reports/*
/// </summary>
[Route("Reports")]
public class ReportsViewController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportsViewController> _logger;

    public ReportsViewController(AppDbContext context, ILogger<ReportsViewController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;
    private Guid GetEstablishmentId() => GetCurrentEmployee()?.EstablishmentId ?? Guid.Empty;

    private bool HasReportPermission()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return false;
        
        var allowedCodes = new[] { "OWNER", "MANAGER", "PHARMACIST_RT", "PHARMACIST", "ACCOUNTANT", "ADMIN", "GENERAL_MANAGER", "SUPERVISOR" };
        var jobCode = employee.JobPosition?.Code?.ToUpper() ?? "";
        return allowedCodes.Contains(jobCode);
    }

    /// <summary>
    /// Dashboard Executivo Principal
    /// GET /Reports
    /// </summary>
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!HasReportPermission()) return Forbid();

        var establishmentId = GetEstablishmentId();
        var today = DateTime.UtcNow.Date;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);

        // KPIs do Dashboard
        var kpis = new DashboardKPIs();

        // Vendas
        kpis.VendasHoje = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate.Date == today && s.Status == "FINALIZADA")
            .SumAsync(s => s.TotalAmount);

        kpis.VendasMes = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate >= startOfMonth && s.Status == "FINALIZADA")
            .SumAsync(s => s.TotalAmount);

        kpis.TicketMedio = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate >= startOfMonth && s.Status == "FINALIZADA")
            .AverageAsync(s => (decimal?)s.TotalAmount) ?? 0;

        kpis.TotalVendasMes = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate >= startOfMonth && s.Status == "FINALIZADA")
            .CountAsync();

        // Produção
        kpis.OrdensProducaoHoje = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && o.CreatedAt.Date == today)
            .CountAsync();

        kpis.OrdensEmProducao = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && o.Status == "EM_PRODUCAO")
            .CountAsync();

        kpis.OrdensFinalizadasMes = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && 
                       o.CompletionDate >= startOfMonth && 
                       (o.Status == "FINALIZADO" || o.Status == "ENTREGUE"))
            .CountAsync();

        kpis.OrdensPendentes = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId && 
                       new[] { "PENDENTE", "AGUARDANDO_APROVACAO", "AGUARDANDO_PRODUCAO" }.Contains(o.Status))
            .CountAsync();

        // Estoque
        kpis.ItensEstoqueBaixo = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.CurrentStock <= r.MinimumStock && r.IsActive)
            .CountAsync();

        kpis.LotesVencendo = await _context.Batches
            .Include(b => b.RawMaterial)
            .Where(b => b.RawMaterial!.EstablishmentId == establishmentId && 
                       b.Status == "APROVADO" &&
                       b.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                       b.ExpiryDate > DateTime.UtcNow)
            .CountAsync();

        kpis.LotesVencidos = await _context.Batches
            .Include(b => b.RawMaterial)
            .Where(b => b.RawMaterial!.EstablishmentId == establishmentId && 
                       b.Status == "APROVADO" &&
                       b.ExpiryDate <= DateTime.UtcNow)
            .CountAsync();

        kpis.ValorEstoque = await _context.Batches
            .Include(b => b.RawMaterial)
            .Where(b => b.RawMaterial!.EstablishmentId == establishmentId && b.Status == "APROVADO")
            .SumAsync(b => b.CurrentQuantity * b.UnitCost);

        // Clientes
        kpis.NovosClientesMes = await _context.Customers
            .Where(c => c.EstablishmentId == establishmentId && c.CreatedAt >= startOfMonth)
            .CountAsync();

        kpis.TotalClientes = await _context.Customers
            .Where(c => c.EstablishmentId == establishmentId && c.Status == "ATIVO")
            .CountAsync();

        // Vendas dos últimos 7 dias para gráfico
        var vendasUltimos7Dias = new List<VendaDiaDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var total = await _context.Sales
                .Where(s => s.EstablishmentId == establishmentId && 
                           s.SaleDate.Date == date && 
                           s.Status == "FINALIZADA")
                .SumAsync(s => (decimal?)s.TotalAmount) ?? 0;

            vendasUltimos7Dias.Add(new VendaDiaDto
            {
                Data = date,
                DiaSemana = date.ToString("ddd"),
                Total = total
            });
        }

        // Top 5 Fórmulas do mês
        var topFormulas = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Where(o => o.EstablishmentId == establishmentId && 
                       o.CreatedAt >= startOfMonth && 
                       o.FormulaId != null)
            .GroupBy(o => new { o.FormulaId, o.Formula!.Name })
            .Select(g => new TopFormulaDto
            {
                FormulaId = g.Key.FormulaId ?? Guid.Empty,
                Nome = g.Key.Name ?? "Sem nome",
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Quantidade)
            .Take(5)
            .ToListAsync();

        // Vendas por forma de pagamento
        var vendasPorPagamento = await _context.SalePayments
            .Include(p => p.Sale)
            .Where(p => p.Sale!.EstablishmentId == establishmentId && 
                       p.Sale.SaleDate >= startOfMonth &&
                       p.PaymentStatus == "APPROVED")
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new VendaPorPagamentoDto
            {
                MetodoPagamento = g.Key ?? "Outro",
                Total = g.Sum(p => p.Amount),
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        ViewBag.KPIs = kpis;
        ViewBag.VendasUltimos7Dias = vendasUltimos7Dias;
        ViewBag.TopFormulas = topFormulas;
        ViewBag.VendasPorPagamento = vendasPorPagamento;
        ViewBag.Establishment = await _context.Establishments.FindAsync(establishmentId);

        return View("Index");
    }

    /// <summary>
    /// Relatório de Vendas
    /// GET /Reports/Vendas
    /// </summary>
    [HttpGet("Vendas")]
    public async Task<IActionResult> Vendas(DateTime? startDate, DateTime? endDate)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!HasReportPermission()) return Forbid();

        var establishmentId = GetEstablishmentId();
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var vendas = await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Items)
            .Include(s => s.CreatedByEmployee)
            .Where(s => s.EstablishmentId == establishmentId && 
                       s.SaleDate >= start && 
                       s.SaleDate <= end.AddDays(1))
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        // Resumo
        var resumo = new VendasResumoDto
        {
            TotalVendas = vendas.Count(v => v.Status == "FINALIZADA"),
            ValorTotal = vendas.Where(v => v.Status == "FINALIZADA").Sum(v => v.TotalAmount),
            TicketMedio = vendas.Where(v => v.Status == "FINALIZADA").Any() 
                ? vendas.Where(v => v.Status == "FINALIZADA").Average(v => v.TotalAmount) : 0,
            VendasCanceladas = vendas.Count(v => v.Status == "CANCELADA"),
            DescontoTotal = vendas.Where(v => v.Status == "FINALIZADA").Sum(v => v.DiscountAmount)
        };

        ViewBag.Vendas = vendas;
        ViewBag.Resumo = resumo;
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;

        return View("Vendas");
    }

    /// <summary>
    /// Relatório de Produção
    /// GET /Reports/Producao
    /// </summary>
    [HttpGet("Producao")]
    public async Task<IActionResult> Producao(DateTime? startDate, DateTime? endDate)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!HasReportPermission()) return Forbid();

        var establishmentId = GetEstablishmentId();
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var ordens = await _context.ManipulationOrders
            .Include(o => o.Formula)
            .Include(o => o.RequestedByEmployee)
            .Include(o => o.ManipulatedByEmployee)
            .Where(o => o.EstablishmentId == establishmentId && 
                       o.CreatedAt >= start && 
                       o.CreatedAt <= end.AddDays(1))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Resumo por status
        var porStatus = ordens
            .GroupBy(o => o.Status)
            .Select(g => new ProducaoPorStatusDto
            {
                Status = g.Key,
                Quantidade = g.Count()
            })
            .ToList();

        // Tempo médio de produção (ordens finalizadas)
        var finalizadas = ordens.Where(o => o.StartDate.HasValue && o.CompletionDate.HasValue).ToList();
        var tempoMedio = finalizadas.Any() 
            ? finalizadas.Average(o => (o.CompletionDate!.Value - o.StartDate!.Value).TotalHours) 
            : 0;

        var resumo = new ProducaoResumoDto
        {
            TotalOrdens = ordens.Count,
            OrdensFinalizadas = ordens.Count(o => o.Status == "FINALIZADO" || o.Status == "ENTREGUE"),
            OrdensCanceladas = ordens.Count(o => o.Status == "CANCELADO"),
            TempoMedioProducaoHoras = Math.Round(tempoMedio, 1)
        };

        ViewBag.Ordens = ordens;
        ViewBag.Resumo = resumo;
        ViewBag.PorStatus = porStatus;
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;

        return View("Producao");
    }

    /// <summary>
    /// Relatório de Estoque
    /// GET /Reports/Estoque
    /// </summary>
    [HttpGet("Estoque")]
    public async Task<IActionResult> Estoque()
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!HasReportPermission()) return Forbid();

        var establishmentId = GetEstablishmentId();

        // Materiais com estoque baixo
        var estoqueBaixo = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && 
                       r.CurrentStock <= r.MinimumStock && 
                       r.IsActive)
            .OrderBy(r => r.CurrentStock / (r.MinimumStock > 0 ? r.MinimumStock : 1))
            .Take(20)
            .ToListAsync();

        // Lotes vencendo em 30 dias
        var lotesVencendo = await _context.Batches
            .Include(b => b.RawMaterial)
            .Include(b => b.Supplier)
            .Where(b => b.RawMaterial!.EstablishmentId == establishmentId && 
                       b.Status == "APROVADO" &&
                       b.ExpiryDate <= DateTime.UtcNow.AddDays(30) &&
                       b.ExpiryDate > DateTime.UtcNow &&
                       b.CurrentQuantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .Take(20)
            .ToListAsync();

        // Lotes vencidos
        var lotesVencidos = await _context.Batches
            .Include(b => b.RawMaterial)
            .Include(b => b.Supplier)
            .Where(b => b.RawMaterial!.EstablishmentId == establishmentId && 
                       b.Status == "APROVADO" &&
                       b.ExpiryDate <= DateTime.UtcNow &&
                       b.CurrentQuantity > 0)
            .OrderBy(b => b.ExpiryDate)
            .Take(20)
            .ToListAsync();

        // Valor total do estoque por categoria - CORRIGIDO: BasePrice ?? 0
        var valorPorCategoria = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .GroupBy(r => !string.IsNullOrEmpty(r.Category) ? r.Category : "Sem Categoria")
            .Select(g => new ValorPorCategoriaDto
            {
                Categoria = g.Key ?? "Sem Categoria",
                QuantidadeItens = g.Count(),
                ValorEstimado = g.Sum(r => r.CurrentStock * (r.BasePrice ?? 0))
            })
            .OrderByDescending(x => x.ValorEstimado)
            .ToListAsync();

        var resumo = new EstoqueResumoDto
        {
            TotalMateriais = await _context.RawMaterials.CountAsync(r => r.EstablishmentId == establishmentId && r.IsActive),
            ItensEstoqueBaixo = estoqueBaixo.Count,
            LotesVencendo30Dias = lotesVencendo.Count,
            LotesVencidos = lotesVencidos.Count,
            ValorTotalEstoque = await _context.Batches
                .Include(b => b.RawMaterial)
                .Where(b => b.RawMaterial!.EstablishmentId == establishmentId && b.Status == "APROVADO")
                .SumAsync(b => b.CurrentQuantity * b.UnitCost)
        };

        ViewBag.EstoqueBaixo = estoqueBaixo;
        ViewBag.LotesVencendo = lotesVencendo;
        ViewBag.LotesVencidos = lotesVencidos;
        ViewBag.ValorPorCategoria = valorPorCategoria;
        ViewBag.Resumo = resumo;

        return View("Estoque");
    }

    /// <summary>
    /// Relatório Financeiro
    /// GET /Reports/Financeiro
    /// </summary>
    [HttpGet("Financeiro")]
    public async Task<IActionResult> Financeiro(DateTime? startDate, DateTime? endDate)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!HasReportPermission()) return Forbid();

        var establishmentId = GetEstablishmentId();
        var start = startDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = endDate ?? DateTime.UtcNow;

        // Receitas (vendas finalizadas)
        var receitas = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && 
                       s.SaleDate >= start && 
                       s.SaleDate <= end.AddDays(1) &&
                       s.Status == "FINALIZADA")
            .SumAsync(s => s.TotalAmount);

        // Recebimentos por método
        var recebimentosPorMetodo = await _context.SalePayments
            .Include(p => p.Sale)
            .Where(p => p.Sale!.EstablishmentId == establishmentId && 
                       p.PaymentDate >= start && 
                       p.PaymentDate <= end.AddDays(1) &&
                       p.PaymentStatus == "APPROVED")
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new RecebimentoPorMetodoDto
            {
                Metodo = g.Key ?? "Outro",
                Total = g.Sum(p => p.Amount),
                Quantidade = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        // Vendas diárias no período
        var vendasDiarias = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && 
                       s.SaleDate >= start && 
                       s.SaleDate <= end.AddDays(1) &&
                       s.Status == "FINALIZADA")
            .GroupBy(s => s.SaleDate.Date)
            .Select(g => new VendaDiariaDto
            {
                Data = g.Key,
                Total = g.Sum(s => s.TotalAmount),
                Quantidade = g.Count()
            })
            .OrderBy(x => x.Data)
            .ToListAsync();

        // Pendências (boletos, pagamentos pendentes)
        var pendencias = await _context.SalePayments
            .Include(p => p.Sale)
            .Where(p => p.Sale!.EstablishmentId == establishmentId && 
                       p.PaymentStatus == "PENDING")
            .SumAsync(p => p.Amount);

        var resumo = new FinanceiroResumoDto
        {
            ReceitaTotal = receitas,
            DescontosConcedidos = await _context.Sales
                .Where(s => s.EstablishmentId == establishmentId && 
                           s.SaleDate >= start && 
                           s.SaleDate <= end.AddDays(1) &&
                           s.Status == "FINALIZADA")
                .SumAsync(s => s.DiscountAmount),
            PendenciasReceber = pendencias,
            MediaDiaria = vendasDiarias.Any() ? vendasDiarias.Average(v => v.Total) : 0
        };

        ViewBag.Resumo = resumo;
        ViewBag.RecebimentosPorMetodo = recebimentosPorMetodo;
        ViewBag.VendasDiarias = vendasDiarias;
        ViewBag.StartDate = start;
        ViewBag.EndDate = end;

        return View("Financeiro");
    }

    /// <summary>
    /// Relatório SNGPC
    /// GET /Reports/SNGPC
    /// </summary>
    [HttpGet("SNGPC")]
    public async Task<IActionResult> SNGPC(int? month, int? year)
    {
        var employee = GetCurrentEmployee();
        if (employee == null) return RedirectToAction("Login", "Account");
        if (!HasReportPermission()) return Forbid();

        var establishmentId = GetEstablishmentId();
        var targetMonth = month ?? DateTime.UtcNow.Month;
        var targetYear = year ?? DateTime.UtcNow.Year;
        var startDate = new DateTime(targetYear, targetMonth, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        // Movimentações de controlados no período
        var movimentacoes = await _context.StockMovements
            .Include(m => m.RawMaterial)
            .Include(m => m.Batch)
            .Include(m => m.PerformedByEmployee)
            .Where(m => m.EstablishmentId == establishmentId &&
                       m.MovementDate >= startDate &&
                       m.MovementDate <= endDate &&
                       m.RawMaterial!.ControlType != null &&
                       m.RawMaterial.ControlType != "COMUM")
            .OrderBy(m => m.MovementDate)
            .ToListAsync();

        // Estoque atual de controlados
        var estoqueControlados = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId &&
                       r.IsActive &&
                       r.ControlType != null &&
                       r.ControlType != "COMUM")
            .OrderBy(r => r.ControlType)
            .ThenBy(r => r.Name)
            .ToListAsync();

        // Resumo por tipo de controle
        var porTipoControle = estoqueControlados
            .GroupBy(r => r.ControlType)
            .Select(g => new ControlePorTipoDto
            {
                TipoControle = g.Key ?? "Outro",
                QuantidadeSubstancias = g.Count(),
                TotalEntradas = movimentacoes.Where(m => m.RawMaterial?.ControlType == g.Key && m.MovementType == "ENTRADA").Sum(m => m.Quantity),
                TotalSaidas = movimentacoes.Where(m => m.RawMaterial?.ControlType == g.Key && m.MovementType != "ENTRADA").Sum(m => m.Quantity)
            })
            .ToList();

        ViewBag.Movimentacoes = movimentacoes;
        ViewBag.EstoqueControlados = estoqueControlados;
        ViewBag.PorTipoControle = porTipoControle;
        ViewBag.Month = targetMonth;
        ViewBag.Year = targetYear;

        return View("SNGPC");
    }
}

// ════════════════════════════════════════════════════════════════════════════
// DTOs para Relatórios (Views)
// ════════════════════════════════════════════════════════════════════════════

public class DashboardKPIs
{
    public decimal VendasHoje { get; set; }
    public decimal VendasMes { get; set; }
    public decimal TicketMedio { get; set; }
    public int TotalVendasMes { get; set; }
    public int OrdensProducaoHoje { get; set; }
    public int OrdensEmProducao { get; set; }
    public int OrdensFinalizadasMes { get; set; }
    public int OrdensPendentes { get; set; }
    public int ItensEstoqueBaixo { get; set; }
    public int LotesVencendo { get; set; }
    public int LotesVencidos { get; set; }
    public decimal ValorEstoque { get; set; }
    public int NovosClientesMes { get; set; }
    public int TotalClientes { get; set; }
}

public class VendaDiaDto
{
    public DateTime Data { get; set; }
    public string DiaSemana { get; set; } = "";
    public decimal Total { get; set; }
}

public class TopFormulaDto
{
    public Guid FormulaId { get; set; }
    public string Nome { get; set; } = "";
    public int Quantidade { get; set; }
}

public class VendaPorPagamentoDto
{
    public string MetodoPagamento { get; set; } = "";
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
}

public class VendasResumoDto
{
    public int TotalVendas { get; set; }
    public decimal ValorTotal { get; set; }
    public decimal TicketMedio { get; set; }
    public int VendasCanceladas { get; set; }
    public decimal DescontoTotal { get; set; }
}

public class ProducaoResumoDto
{
    public int TotalOrdens { get; set; }
    public int OrdensFinalizadas { get; set; }
    public int OrdensCanceladas { get; set; }
    public double TempoMedioProducaoHoras { get; set; }
}

public class ProducaoPorStatusDto
{
    public string Status { get; set; } = "";
    public int Quantidade { get; set; }
}

public class EstoqueResumoDto
{
    public int TotalMateriais { get; set; }
    public int ItensEstoqueBaixo { get; set; }
    public int LotesVencendo30Dias { get; set; }
    public int LotesVencidos { get; set; }
    public decimal ValorTotalEstoque { get; set; }
}

public class ValorPorCategoriaDto
{
    public string Categoria { get; set; } = "";
    public int QuantidadeItens { get; set; }
    public decimal ValorEstimado { get; set; }
}

public class FinanceiroResumoDto
{
    public decimal ReceitaTotal { get; set; }
    public decimal DescontosConcedidos { get; set; }
    public decimal PendenciasReceber { get; set; }
    public decimal MediaDiaria { get; set; }
}

public class RecebimentoPorMetodoDto
{
    public string Metodo { get; set; } = "";
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
}

public class VendaDiariaDto
{
    public DateTime Data { get; set; }
    public decimal Total { get; set; }
    public int Quantidade { get; set; }
}

public class ControlePorTipoDto
{
    public string TipoControle { get; set; } = "";
    public int QuantidadeSubstancias { get; set; }
    public decimal TotalEntradas { get; set; }
    public decimal TotalSaidas { get; set; }
}
