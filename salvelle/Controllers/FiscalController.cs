using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Employees;
using Models.Fiscal;
using ViewModels.Fiscal;

namespace Controllers;

/// <summary>
/// Controller MVC para Views do módulo Fiscal
/// Rota: /Fiscal
/// </summary>
[Authorize]
public class FiscalController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<FiscalController> _logger;

    public FiscalController(AppDbContext db, ILogger<FiscalController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ================================================================
    // DASHBOARD E PÁGINAS PRINCIPAIS
    // ================================================================

    /// <summary>
    /// Página principal de Notas Fiscais
    /// GET /Fiscal
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var establishmentId = employee.EstablishmentId;
        var today = DateTime.UtcNow.Date;
        var primeiroDiaMes = new DateTime(today.Year, today.Month, 1);

        ViewBag.NFesHoje = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate.Date == today
                && f.InvoiceType == "NFE"
                && f.Status == "AUTORIZADO")
            .CountAsync();

        ViewBag.NFCesHoje = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate.Date == today
                && f.InvoiceType == "NFCE"
                && f.Status == "AUTORIZADO")
            .CountAsync();

        ViewBag.FaturamentoHoje = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate.Date == today
                && f.Status == "AUTORIZADO")
            .SumAsync(f => (decimal?)f.TotalAmount) ?? 0;

        ViewBag.TotalNotasMes = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate >= primeiroDiaMes
                && f.Status == "AUTORIZADO")
            .CountAsync();

        ViewBag.FaturamentoMes = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.IssueDate >= primeiroDiaMes
                && f.Status == "AUTORIZADO")
            .SumAsync(f => (decimal?)f.TotalAmount) ?? 0;

        ViewBag.NotasPendentes = await _db.FiscalQueues
            .Where(q => q.EstablishmentId == establishmentId && q.Status == "PENDENTE")
            .CountAsync();

        ViewBag.NotasComErro = await _db.FiscalInvoices
            .Where(f => f.EstablishmentId == establishmentId 
                && f.Status == "REJEITADO"
                && f.IssueDate >= today.AddDays(-7))
            .CountAsync();

        var config = await _db.FiscalConfigs
            .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId && c.IsActive);

        ViewBag.ConfiguracaoOk = config != null;
        ViewBag.Ambiente = config?.Environment ?? "NÃO CONFIGURADO";
        ViewBag.CertificadoValido = config?.CertificateExpiry > DateTime.UtcNow;
        ViewBag.CertificadoExpira = config?.CertificateExpiry;

        return View();
    }

    /// <summary>
    /// Página de configurações fiscais
    /// GET /Fiscal/Config
    /// </summary>
    [HttpGet]
    public IActionResult Config()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        return View();
    }

    /// <summary>
    /// Página de detalhes de uma nota fiscal
    /// GET /Fiscal/Details/{id}
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var invoice = await _db.FiscalInvoices
            .Include(f => f.Items)
            .Include(f => f.Sale)
            .FirstOrDefaultAsync(f => f.Id == id && f.EstablishmentId == employee.EstablishmentId);

        if (invoice == null)
            return NotFound();

        return View(invoice);
    }

    // ================================================================
    // NFC-e VISUALIZAÇÃO
    // ================================================================

    /// <summary>
    /// Visualizar NFC-e MOCK (demonstração)
    /// GET /Fiscal/NFCe/Mock
    /// </summary>
    [HttpGet("Fiscal/NFCe/Mock")]
    [AllowAnonymous]
    public async Task<IActionResult> NFCeMock()
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        
        NFCeViewModel vm;
        
        if (employee != null)
        {
            var establishment = await _db.Establishments
                .FirstOrDefaultAsync(e => e.Id == employee.EstablishmentId);
            
            if (establishment != null)
            {
                vm = NFCeViewModelFactory.CreateMock(
                    establishment.RazaoSocial,
                    establishment.NomeFantasia,
                    establishment.Cnpj
                );
                
                vm.Emitente.IE = establishment.InscricaoEstadual;
                vm.Emitente.Endereco = BuildEndereco(establishment.Street, establishment.Number);
                vm.Emitente.Bairro = establishment.Neighborhood ?? "Centro";
                vm.Emitente.Cidade = establishment.City ?? "São Paulo";
                vm.Emitente.UF = establishment.State ?? "SP";
                vm.Emitente.CEP = FormatCEP(establishment.PostalCode ?? "01000000");
                vm.Emitente.Fone = establishment.Phone;
                
                vm.UrlConsulta = GetUrlConsultaByUF(establishment.State ?? "SP");
            }
            else
            {
                vm = NFCeViewModelFactory.CreateMock();
            }
        }
        else
        {
            vm = NFCeViewModelFactory.CreateMock();
        }
        
        return View("NFCe", vm);
    }

    /// <summary>
    /// Visualizar NFC-e de uma venda específica
    /// GET /Fiscal/NFCe/Venda/{saleId}
    /// </summary>
    [HttpGet("Fiscal/NFCe/Venda/{saleId}")]
    public async Task<IActionResult> NFCeFromSale(Guid saleId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var sale = await _db.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId && s.EstablishmentId == employee.EstablishmentId);

        if (sale == null)
            return NotFound("Venda não encontrada");

        var establishment = await _db.Establishments
            .FirstOrDefaultAsync(e => e.Id == sale.EstablishmentId);

        if (establishment == null)
            return NotFound("Estabelecimento não encontrado");

        var customer = sale.CustomerId.HasValue
            ? await _db.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId)
            : null;

        var fiscalDoc = await _db.FiscalInvoices
            .FirstOrDefaultAsync(fd => fd.SaleId == saleId);

        var vm = NFCeViewModelFactory.FromSale(sale, establishment, customer, fiscalDoc);

        return View("NFCe", vm);
    }

    /// <summary>
    /// Visualizar NFC-e por ID do documento fiscal
    /// GET /Fiscal/NFCe/{docId}
    /// </summary>
    [HttpGet("Fiscal/NFCe/{docId}")]
    public async Task<IActionResult> NFCeFromDocument(Guid docId)
    {
        var employee = HttpContext.Items["Employee"] as Employee;
        if (employee == null)
            return RedirectToAction("Login", "Account");

        var fiscalDoc = await _db.FiscalInvoices
            .Include(f => f.Items)
            .FirstOrDefaultAsync(fd => fd.Id == docId && fd.EstablishmentId == employee.EstablishmentId);

        if (fiscalDoc == null)
            return NotFound("Documento fiscal não encontrado");

        var establishment = await _db.Establishments
            .FirstOrDefaultAsync(e => e.Id == fiscalDoc.EstablishmentId);

        if (establishment == null)
            return NotFound("Estabelecimento não encontrado");

        Customer? customer = null;
        Sale? sale = null;
        
        if (fiscalDoc.SaleId.HasValue)
        {
            sale = await _db.Sales
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == fiscalDoc.SaleId);
            
            if (sale?.CustomerId.HasValue == true)
            {
                customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId);
            }
        }

        NFCeViewModel vm;
        if (sale != null)
        {
            vm = NFCeViewModelFactory.FromSale(sale, establishment, customer, fiscalDoc);
        }
        else
        {
            vm = NFCeViewModelFactory.FromFiscalInvoice(fiscalDoc, establishment);
        }

        return View("NFCe", vm);
    }

    /// <summary>
    /// Visualizar NFC-e por chave de acesso (público)
    /// GET /Fiscal/NFCe/Chave/{chave}
    /// IMPORTANTE: FiscalInvoice usa InvoiceKey, NÃO AccessKey
    /// </summary>
    [HttpGet("Fiscal/NFCe/Chave/{chave}")]
    [AllowAnonymous]
    public async Task<IActionResult> NFCeByChave(string chave)
    {
        chave = new string(chave.Where(char.IsDigit).ToArray());
        
        // CORRIGIDO: FiscalInvoice usa InvoiceKey, não AccessKey
        var fiscalDoc = await _db.FiscalInvoices
            .Include(f => f.Items)
            .FirstOrDefaultAsync(fd => fd.InvoiceKey == chave);

        if (fiscalDoc == null)
            return NotFound("Documento não encontrado para esta chave de acesso");

        var establishment = await _db.Establishments
            .FirstOrDefaultAsync(e => e.Id == fiscalDoc.EstablishmentId);

        if (establishment == null)
            return NotFound("Estabelecimento não encontrado");

        var vm = NFCeViewModelFactory.FromFiscalInvoice(fiscalDoc, establishment);

        return View("NFCe", vm);
    }

    // ================================================================
    // HELPER METHODS
    // ================================================================

    private static string BuildEndereco(string? street, string? number)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(street)) parts.Add(street);
        if (!string.IsNullOrWhiteSpace(number)) parts.Add(number);
        return string.Join(", ", parts);
    }

    private static string FormatCEP(string cep)
    {
        cep = new string(cep.Where(char.IsDigit).ToArray());
        if (cep.Length != 8) return cep;
        return $"{cep.Substring(0, 5)}-{cep.Substring(5, 3)}";
    }

    private static string GetUrlConsultaByUF(string uf)
    {
        return uf.ToUpper() switch
        {
            "SP" => "www.nfce.fazenda.sp.gov.br",
            "RJ" => "www.fazenda.rj.gov.br/nfce/consulta",
            "MG" => "nfce.fazenda.mg.gov.br/portalnfce",
            "RS" => "www.sefaz.rs.gov.br/NFE/NFE-NFC.aspx",
            "PR" => "www.fazenda.pr.gov.br/nfce",
            "SC" => "www.sef.sc.gov.br/nfce/consulta",
            "BA" => "nfe.sefaz.ba.gov.br/servicos/nfce/default.aspx",
            "PE" => "nfce.sefaz.pe.gov.br/nfce/consulta",
            "CE" => "nfce.sefaz.ce.gov.br/pages/consultaNota.jsf",
            "GO" => "nfce.sefaz.go.gov.br/nfeweb/sites/nfce/consulta",
            _ => "www.nfe.fazenda.gov.br/portal/consultaRecaptcha.aspx"
        };
    }
}
