using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Pharmacy;

namespace Controllers;

/// <summary>
/// Controller MVC para todas as páginas do Portal do Cliente
/// Rota: /Cliente/*
/// </summary>
[Route("Cliente")]
public class ClienteController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ClienteController> _logger;

    public ClienteController(AppDbContext context, ILogger<ClienteController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════════════
    
    private Customer? GetCurrentCustomer()
    {
        return HttpContext.Items["Customer"] as Customer;
    }

    private CustomerSession? GetCurrentSession()
    {
        return HttpContext.Items["CustomerSession"] as CustomerSession;
    }

    private async Task<Establishment?> GetCurrentEstablishment()
    {
        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null) return null;
        
        return await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == session.CurrentEstablishmentId);
    }

    private async Task SetViewBagData()
    {
        var customer = GetCurrentCustomer();
        var establishment = await GetCurrentEstablishment();
        var session = GetCurrentSession();

        ViewBag.CustomerName = customer?.FullName ?? "Cliente";
        ViewBag.PharmacyName = establishment?.NomeFantasia ?? "Selecione uma farmácia";
        ViewBag.PharmacyId = establishment?.Id;
        
        // Contar itens no carrinho
        if (customer != null && session?.CurrentEstablishmentId != null)
        {
            var cartCount = await _context.CustomerCartItems
                .Include(i => i.Cart)
                .Where(i => i.Cart!.CustomerId == customer.Id && 
                           i.Cart.EstablishmentId == session.CurrentEstablishmentId)
                .CountAsync();
            ViewBag.CartCount = cartCount;
        }
        else
        {
            ViewBag.CartCount = 0;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // AUTENTICAÇÃO (páginas públicas)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Página de Login
    /// GET /Cliente/Login
    /// </summary>
    [HttpGet("Login")]
    public IActionResult Login([FromQuery] string? returnUrl = null, [FromQuery] Guid? estabelecimento = null)
    {
        // Só aceitar URLs locais: bloqueia open redirect e XSS via returnUrl
        var safeReturnUrl = (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) ? returnUrl : null;

        // Se já está logado, redireciona
        var session = GetCurrentSession();
        if (session != null)
        {
            if (!string.IsNullOrEmpty(safeReturnUrl))
                return Redirect(safeReturnUrl);
            return RedirectToAction("Inicio");
        }

        ViewBag.ReturnUrl = safeReturnUrl;
        ViewBag.EstabelecimentoId = estabelecimento;
        
        // Se veio de QR Code, buscar nome da farmácia
        if (estabelecimento.HasValue)
        {
            var est = _context.Establishments
                .FirstOrDefault(e => e.Id == estabelecimento.Value && e.IsActive);
            ViewBag.EstabelecimentoNome = est?.NomeFantasia;
        }

        return View("Login");
    }

    /// <summary>
    /// Página de Cadastro
    /// GET /Cliente/Cadastro
    /// </summary>
    [HttpGet("Cadastro")]
    public IActionResult Cadastro([FromQuery] string? returnUrl = null, [FromQuery] Guid? estabelecimento = null)
    {
        var session = GetCurrentSession();
        if (session != null)
            return RedirectToAction("Inicio");

        ViewBag.ReturnUrl = returnUrl;
        ViewBag.EstabelecimentoId = estabelecimento;
        return View("Cadastro");
    }

    /// <summary>
    /// Página de Verificação de Código
    /// GET /Cliente/Verificar
    /// </summary>
    [HttpGet("Verificar")]
    public IActionResult Verificar([FromQuery] string? phone = null, [FromQuery] string? returnUrl = null)
    {
        if (string.IsNullOrEmpty(phone))
            return RedirectToAction("Login");

        ViewBag.Phone = phone;
        ViewBag.ReturnUrl = returnUrl;
        return View("Verificar");
    }

    /// <summary>
    /// Página de Recuperação de Senha
    /// GET /Cliente/EsqueciSenha
    /// </summary>
    [HttpGet("EsqueciSenha")]
    [HttpGet("RecuperarSenha")]  // Alias
    public IActionResult EsqueciSenha()
    {
        return View("EsqueciSenha");
    }

    /// <summary>
    /// Página para Redefinir Senha (após receber código via WhatsApp)
    /// GET /Cliente/RedefinirSenha?phone=XXX
    /// </summary>
    [HttpGet("RedefinirSenha")]
    public IActionResult RedefinirSenha([FromQuery] string? phone = null)
    {
        if (string.IsNullOrEmpty(phone))
            return RedirectToAction("EsqueciSenha");

        ViewBag.Phone = phone;
        return View("RedefinirSenha");
    }

    /// <summary>
    /// Logout
    /// GET /Cliente/Logout
    /// </summary>
    [HttpGet("Logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("CustomerSessionId");
        Response.Cookies.Delete("salvelle_customer_session");
        return RedirectToAction("Login");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // PÁGINAS PRINCIPAIS (requerem autenticação)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Homepage do Portal
    /// GET /Cliente/Inicio
    /// </summary>
    [HttpGet("Inicio")]
    public async Task<IActionResult> Inicio()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        await SetViewBagData();
        return View("Inicio");
    }

    /// <summary>
    /// Seleção de Farmácia
    /// GET /Cliente/Estabelecimentos
    /// </summary>
    [HttpGet("Estabelecimentos")]
    public async Task<IActionResult> Estabelecimentos()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var establishments = await _context.Establishments
            .Where(e => e.IsActive)
            .OrderBy(e => e.NomeFantasia)
            .ToListAsync();

        ViewBag.Establishments = establishments;
        await SetViewBagData();
        return View("Estabelecimentos");
    }

    /// <summary>
    /// Buscar produtos/fórmulas
    /// GET /Cliente/Buscar
    /// </summary>
    [HttpGet("Buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string? q = null)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        ViewBag.SearchQuery = q;
        ViewBag.SearchResults = new List<object>();

        if (!string.IsNullOrEmpty(q))
        {
            // Buscar em matérias-primas
            var rawMaterials = await _context.RawMaterials
                .Where(r => r.EstablishmentId == session.CurrentEstablishmentId &&
                           r.IsActive &&
                           (r.Name.ToLower().Contains(q.ToLower()) ||
                            (r.Synonyms != null && r.Synonyms.ToLower().Contains(q.ToLower()))))
                .Take(20)
                .Select(r => new {
                    Id = r.Id,
                    Name = r.Name,
                    Type = "Matéria-Prima",
                    Category = r.Category,
                    InStock = r.CurrentStock > 0,
                    Price = r.BasePrice ?? r.LastPurchasePrice ?? 0
                })
                .ToListAsync();

            ViewBag.SearchResults = rawMaterials;
        }

        await SetViewBagData();
        return View("Buscar");
    }

    /// <summary>
    /// Enviar Receita (OCR)
    /// GET /Cliente/EnviarReceita
    /// </summary>
    [HttpGet("EnviarReceita")]
    public async Task<IActionResult> EnviarReceita()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        await SetViewBagData();
        return View("EnviarReceita");
    }

    /// <summary>
    /// Minha Fórmula Personalizada (redireciona para CriarFormula)
    /// GET /Cliente/MinhaFormula
    /// </summary>
    [HttpGet("MinhaFormula")]
    public async Task<IActionResult> MinhaFormula()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        // Buscar categorias de produtos
        var productTypes = await _context.ProductTypes
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.DisplayOrder)
            .ToListAsync();

        ViewBag.ProductTypes = productTypes;
        await SetViewBagData();
        return View("CriarFormula");  // Usa mesma view de CriarFormula
    }

    /// <summary>
    /// Catálogo de Produtos
    /// GET /Cliente/Catalogo
    /// </summary>
    [HttpGet("Catalogo")]
    public async Task<IActionResult> Catalogo([FromQuery] string? categoria = null)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        // Buscar categorias
        var categories = await _context.RawMaterials
            .Where(r => r.EstablishmentId == session.CurrentEstablishmentId && 
                       r.IsActive && 
                       r.Category != null)
            .Select(r => r.Category)
            .Distinct()
            .ToListAsync();

        // Buscar produtos por categoria
        var query = _context.RawMaterials
            .Where(r => r.EstablishmentId == session.CurrentEstablishmentId && r.IsActive);

        if (!string.IsNullOrEmpty(categoria))
        {
            query = query.Where(r => r.Category != null && 
                                    r.Category.ToLower().Contains(categoria.ToLower()));
        }

        var products = await query
            .OrderBy(r => r.Name)
            .Take(50)
            .ToListAsync();

        ViewBag.Categories = categories;
        ViewBag.Products = products;
        ViewBag.CurrentCategory = categoria;
        await SetViewBagData();
        return View("Catalogo");
    }

    /// <summary>
    /// Meus Pedidos
    /// GET /Cliente/MeusPedidos
    /// </summary>
    [HttpGet("MeusPedidos")]
    public async Task<IActionResult> MeusPedidos()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var orders = await _context.OnlineOrders
            .Include(o => o.Establishment)
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customer.Id)
            .OrderByDescending(o => o.CreatedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.Orders = orders;
        await SetViewBagData();
        return View("MeusPedidos");
    }

    /// <summary>
    /// Detalhe do Pedido
    /// GET /Cliente/Pedido/{id}
    /// </summary>
    [HttpGet("Pedido/{id}")]
    public async Task<IActionResult> Pedido(Guid id)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var order = await _context.OnlineOrders
            .Include(o => o.Establishment)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customer.Id);

        if (order == null)
            return NotFound();

        ViewBag.Order = order;
        await SetViewBagData();
        return View("PedidoDetalhe");
    }

    /// <summary>
    /// Carrinho
    /// GET /Cliente/Carrinho
    /// </summary>
    [HttpGet("Carrinho")]
    public async Task<IActionResult> Carrinho()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        var cart = await _context.CustomerCarts
            .Include(c => c.Items!)
                .ThenInclude(i => i.CustomerFormula)
            .Include(c => c.Establishment)
            .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && 
                                     c.EstablishmentId == session.CurrentEstablishmentId);

        ViewBag.Cart = cart;
        ViewBag.CartItems = cart?.Items?.ToList() ?? new List<CustomerCartItem>();
        ViewBag.CartTotal = cart?.Items?.Sum(i => i.UnitPrice * i.Quantity) ?? 0;
        await SetViewBagData();
        return View("Carrinho");
    }

    /// <summary>
    /// Perfil do Cliente
    /// GET /Cliente/Perfil
    /// </summary>
    [HttpGet("Perfil")]
    public async Task<IActionResult> Perfil()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        ViewBag.Customer = customer;
        await SetViewBagData();
        return View("Perfil");
    }

    /// <summary>
    /// Notificações
    /// GET /Cliente/Notificacoes
    /// </summary>
    [HttpGet("Notificacoes")]
    public async Task<IActionResult> Notificacoes()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        // TODO: Implementar sistema de notificações
        ViewBag.Notifications = new List<object>();
        await SetViewBagData();
        return View("Notificacoes");
    }

    /// <summary>
    /// Histórico de Fórmulas
    /// GET /Cliente/Historico
    /// </summary>
    [HttpGet("Historico")]
    public async Task<IActionResult> Historico()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var formulas = await _context.CustomerFormulas
            .Include(f => f.Establishment)
            .Include(f => f.ProductType)
            .Where(f => f.CustomerId == customer.Id)
            .OrderByDescending(f => f.CreatedAt)
            .Take(20)
            .ToListAsync();

        ViewBag.Formulas = formulas;
        await SetViewBagData();
        return View("Historico");
    }

    /// <summary>
    /// Meus Cupons
    /// GET /Cliente/MeusCupons
    /// </summary>
    [HttpGet("MeusCupons")]
    public async Task<IActionResult> MeusCupons()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        
        // Buscar cupons disponíveis para o cliente
        var coupons = await _context.Coupons
            .Where(c => c.IsActive && 
                       c.ValidFrom <= DateTime.UtcNow &&
                       c.ValidUntil >= DateTime.UtcNow &&
                       (c.EstablishmentId == null || c.EstablishmentId == session!.CurrentEstablishmentId) &&
                       (c.MaxUses == null || c.UsedCount < c.MaxUses))
            .OrderByDescending(c => c.DiscountPercentage)
            .ToListAsync();

        ViewBag.Coupons = coupons;
        await SetViewBagData();
        return View("MeusCupons");
    }

    /// <summary>
    /// Selecionar Farmácia via QR Code
    /// GET /Cliente/SelecionarFarmacia
    /// </summary>
    [HttpGet("SelecionarFarmacia")]
    public async Task<IActionResult> SelecionarFarmacia()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        await SetViewBagData();
        return View("Estabelecimentos");
    }

    /// <summary>
    /// Criar Fórmula Personalizada
    /// GET /Cliente/CriarFormula
    /// </summary>
    [HttpGet("CriarFormula")]
    public async Task<IActionResult> CriarFormula()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        // Buscar tipos de produtos para formulário
        var productTypes = await _context.ProductTypes
            .Where(pt => pt.IsActive)
            .OrderBy(pt => pt.DisplayOrder)
            .ToListAsync();

        ViewBag.ProductTypes = productTypes;
        await SetViewBagData();
        return View("CriarFormula");
    }

    /// <summary>
    /// Ler QR Code da Farmácia
    /// GET /Cliente/LerQRCode
    /// </summary>
    [HttpGet("LerQRCode")]
    public async Task<IActionResult> LerQRCode()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        await SetViewBagData();
        return View("LerQRCode");
    }

    /// <summary>
    /// Meus Dados Pessoais
    /// GET /Cliente/MeusDados
    /// </summary>
    [HttpGet("MeusDados")]
    public async Task<IActionResult> MeusDados()
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        ViewBag.Customer = customer;
        await SetViewBagData();
        return View("MeusDados");
    }

    /// <summary>
    /// Detalhe do Produto
    /// GET /Cliente/Produto/{id}
    /// </summary>
    [HttpGet("Produto/{id}")]
    public async Task<IActionResult> Produto(Guid id)
    {
        var customer = GetCurrentCustomer();
        if (customer == null)
            return RedirectToAction("Login");

        var session = GetCurrentSession();
        if (session?.CurrentEstablishmentId == null)
            return RedirectToAction("Estabelecimentos");

        // Buscar produto do catálogo
        var product = await _context.CatalogProducts
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id && 
                                     p.EstablishmentId == session.CurrentEstablishmentId &&
                                     p.IsActive);

        if (product == null)
            return RedirectToAction("Catalogo");

        ViewBag.Product = product;
        await SetViewBagData();
        return View("Produto");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // REDIRECT PADRÃO
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Redireciona /Cliente para /Cliente/Inicio
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction("Inicio");
    }
}
