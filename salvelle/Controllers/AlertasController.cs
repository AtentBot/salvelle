using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Helpers;
using Models.Employees;

namespace Controllers.Api;

/// <summary>
/// API de Alertas e Notificações do Sistema
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlertasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<AlertasController> _logger;

    public AlertasController(AppDbContext db, ILogger<AlertasController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;

        _logger.LogWarning("EstablishmentId não encontrado no HttpContext - requisição sem autenticação?");
        throw new UnauthorizedAccessException("EstablishmentId não disponível. Faça login novamente.");
    }

    /// <summary>
    /// Contador de alertas para o badge do menu
    /// GET /api/alertas/contador
    /// </summary>
    [HttpGet("contador")]
    public async Task<IActionResult> GetContador()
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var hoje = DateTime.UtcNow.Date;
            var limite15Dias = hoje.AddDays(15);

            var estoqueCritico = await _db.RawMaterials
                .CountAsync(r => r.EstablishmentId == establishmentId && 
                                r.IsActive && r.CurrentStock < r.MinimumStock);

            var lotesVencendo = await _db.Batches
                .Where(b => b.RawMaterial != null && b.RawMaterial.EstablishmentId == establishmentId)
                .CountAsync(b => b.ExpiryDate <= limite15Dias && b.ExpiryDate > hoje &&
                                b.CurrentQuantity > 0);

            var ordensAtrasadas = await _db.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId &&
                                !ManipulationStatus.Closed.Contains(o.Status) &&
                                o.ExpectedDate < hoje);

            var total = estoqueCritico + lotesVencendo + ordensAtrasadas;

            return Ok(new
            {
                total,
                estoqueCritico,
                lotesVencendo,
                ordensAtrasadas
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter contador de alertas");
            return Ok(new { total = 0 });
        }
    }

    /// <summary>
    /// Lista completa de alertas
    /// GET /api/alertas
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAlertas([FromQuery] string? tipo = null)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var hoje = DateTime.UtcNow.Date;
            var alertas = new List<AlertaDto>();

            // ESTOQUE CRÍTICO
            if (tipo == null || tipo == "estoque")
            {
                var criticos = await _db.RawMaterials
                    .Where(r => r.EstablishmentId == establishmentId && 
                               r.IsActive && r.CurrentStock < r.MinimumStock)
                    .OrderBy(r => r.CurrentStock / r.MinimumStock)
                    .Take(20)
                    .Select(r => new { r.Id, r.Name, r.CurrentStock, r.MinimumStock, r.Unit })
                    .ToListAsync();

                foreach (var item in criticos)
                {
                    var percentual = item.MinimumStock > 0 
                        ? Math.Round(item.CurrentStock / item.MinimumStock * 100, 0) 
                        : 0;
                    
                    alertas.Add(new AlertaDto
                    {
                        Id = item.Id.ToString(),
                        Tipo = "ESTOQUE_CRITICO",
                        Prioridade = percentual < 25 ? "ALTA" : percentual < 50 ? "MEDIA" : "BAIXA",
                        Titulo = "Estoque Crítico",
                        Mensagem = $"{item.Name}: {item.CurrentStock:N2} {item.Unit} ({percentual}% do mínimo)",
                        Link = $"/Estoque/MateriaPrima/{item.Id}",
                        Icone = "exclamation-triangle-fill",
                        Cor = percentual < 25 ? "danger" : "warning",
                        CriadoEm = DateTime.UtcNow
                    });
                }
            }

            // LOTES VENCENDO
            if (tipo == null || tipo == "validade")
            {
                var limite30Dias = hoje.AddDays(30);
                var lotes = await _db.Batches
                    .Include(b => b.RawMaterial)
                    .Where(b => b.RawMaterial != null && 
                               b.RawMaterial.EstablishmentId == establishmentId &&
                               b.ExpiryDate <= limite30Dias && b.ExpiryDate > hoje &&
                               b.CurrentQuantity > 0)
                    .OrderBy(b => b.ExpiryDate)
                    .Take(20)
                    .Select(b => new { 
                        b.Id, 
                        Material = b.RawMaterial!.Name, 
                        b.BatchNumber, 
                        b.ExpiryDate,
                        b.CurrentQuantity,
                        b.RawMaterial.Unit
                    })
                    .ToListAsync();

                foreach (var lote in lotes)
                {
                    var dias = (lote.ExpiryDate - hoje).Days;
                    alertas.Add(new AlertaDto
                    {
                        Id = lote.Id.ToString(),
                        Tipo = "VALIDADE",
                        Prioridade = dias <= 7 ? "ALTA" : dias <= 15 ? "MEDIA" : "BAIXA",
                        Titulo = dias <= 0 ? "Lote Vencido!" : $"Vence em {dias} dias",
                        Mensagem = $"{lote.Material} - Lote {lote.BatchNumber} ({lote.CurrentQuantity:N2} {lote.Unit})",
                        Link = "/Estoque/Lotes",
                        Icone = dias <= 7 ? "calendar-x-fill" : "calendar-event",
                        Cor = dias <= 7 ? "danger" : "warning",
                        CriadoEm = DateTime.UtcNow,
                        DataLimite = lote.ExpiryDate
                    });
                }
            }

            // ORDENS ATRASADAS
            if (tipo == null || tipo == "producao")
            {
                var ordens = await _db.ManipulationOrders
                    .Where(o => o.EstablishmentId == establishmentId &&
                               !ManipulationStatus.Closed.Contains(o.Status) &&
                               o.ExpectedDate < hoje)
                    .OrderBy(o => o.ExpectedDate)
                    .Take(10)
                    .Select(o => new { o.Id, o.OrderNumber, o.CustomerName, o.ExpectedDate, o.Status })
                    .ToListAsync();

                foreach (var ordem in ordens)
                {
                    var diasAtraso = (hoje - ordem.ExpectedDate).Days;
                    alertas.Add(new AlertaDto
                    {
                        Id = ordem.Id.ToString(),
                        Tipo = "PRODUCAO_ATRASADA",
                        Prioridade = diasAtraso > 3 ? "ALTA" : "MEDIA",
                        Titulo = $"Ordem Atrasada ({diasAtraso} dias)",
                        Mensagem = $"#{ordem.OrderNumber} - {ordem.CustomerName} - Status: {ordem.Status}",
                        Link = $"/Manipulacao/Ordem/{ordem.Id}",
                        Icone = "clock-history",
                        Cor = "danger",
                        CriadoEm = DateTime.UtcNow
                    });
                }
            }

            // CERTIFICADOS DE FORNECEDORES VENCENDO
            if (tipo == null || tipo == "documentos")
            {
                var limite60Dias = hoje.AddDays(60);
                var certificados = await _db.SupplierCertifications
                    .Include(c => c.Supplier)
                    .Where(c => c.Supplier != null && 
                               c.Supplier.EstablishmentId == establishmentId &&
                               c.ExpirationDate <= limite60Dias && c.ExpirationDate > hoje &&
                               c.Status == "ATIVO")
                    .OrderBy(c => c.ExpirationDate)
                    .Take(10)
                    .Select(c => new { 
                        c.Id, 
                        Fornecedor = c.Supplier!.TradeName ?? c.Supplier.TradeName, 
                        c.CertificationType, 
                        c.CertificationNumber,
                        c.ExpirationDate 
                    })
                    .ToListAsync();

                foreach (var cert in certificados)
                {
                    var dias = (cert.ExpirationDate.Value - hoje).Days;
                    alertas.Add(new AlertaDto
                    {
                        Id = cert.Id.ToString(),
                        Tipo = "CERTIFICADO_VENCENDO",
                        Prioridade = dias <= 15 ? "ALTA" : dias <= 30 ? "MEDIA" : "BAIXA",
                        Titulo = $"Certificado vence em {dias} dias",
                        Mensagem = $"{cert.Fornecedor} - {cert.CertificationType} ({cert.CertificationNumber})",
                        Link = "/Cadastros/Fornecedores",
                        Icone = "file-earmark-x",
                        Cor = dias <= 15 ? "danger" : "warning",
                        CriadoEm = DateTime.UtcNow,
                        DataLimite = cert.ExpirationDate
                    });
                }
            }

            // Ordenar por prioridade
            var ordenados = alertas
                .OrderByDescending(a => a.Prioridade == "ALTA")
                .ThenByDescending(a => a.Prioridade == "MEDIA")
                .ThenBy(a => a.DataLimite)
                .ToList();

            return Ok(new
            {
                total = ordenados.Count,
                alertas = ordenados
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter alertas");
            return StatusCode(500, new { error = "Erro ao carregar alertas" });
        }
    }

    /// <summary>
    /// Marcar alerta como lido (para implementação futura)
    /// POST /api/alertas/{id}/ler
    /// </summary>
    [HttpPost("{id}/ler")]
    public IActionResult MarcarComoLido(string id)
    {
        return Ok(new { success = true, message = "Alerta marcado como lido" });
    }
}

public class AlertaDto
{
    public string Id { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Prioridade { get; set; } = "BAIXA";
    public string Titulo { get; set; } = "";
    public string Mensagem { get; set; } = "";
    public string? Link { get; set; }
    public string Icone { get; set; } = "bell";
    public string Cor { get; set; } = "secondary";
    public DateTime CriadoEm { get; set; }
    public DateTime? DataLimite { get; set; }
    public bool Lido { get; set; } = false;
}
