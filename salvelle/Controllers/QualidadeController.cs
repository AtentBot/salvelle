using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using Models.Employees;
using System.ComponentModel.DataAnnotations;
using Models.Quality;

namespace Controllers.Api;

/// <summary>
/// API de Gestão da Qualidade - POPs, CAPA, Indicadores
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QualidadeController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<QualidadeController> _logger;

    public QualidadeController(AppDbContext db, ILogger<QualidadeController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private Employee? GetCurrentEmployee() => HttpContext.Items["Employee"] as Employee;
    
    private Guid GetEstablishmentId()
    {
        if (HttpContext.Items.TryGetValue("EstablishmentId", out var estId) && estId is Guid id)
            return id;
        throw new UnauthorizedAccessException("EstablishmentId nao encontrado na sessao");
    }

    #region POPs - Procedimentos Operacionais Padrão

    /// <summary>
    /// Lista POPs do estabelecimento
    /// GET /api/qualidade/pops
    /// </summary>
    [HttpGet("pops")]
    public async Task<IActionResult> ListarPOPs(
        [FromQuery] string? categoria = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var query = _db.Set<POP>()
                .Where(p => p.EstablishmentId == establishmentId);

            if (!string.IsNullOrEmpty(categoria))
                query = query.Where(p => p.Category == categoria);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    p.Code,
                    p.Title,
                    p.Category,
                    p.Version,
                    p.Status,
                    p.EffectiveDate,
                    p.ReviewDate,
                    p.CreatedAt,
                    p.UpdatedAt,
                    vencido = p.ReviewDate < DateTime.UtcNow
                })
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar POPs");
            return StatusCode(500, new { error = "Erro ao carregar POPs" });
        }
    }

    /// <summary>
    /// Obtém detalhes de um POP
    /// GET /api/qualidade/pops/{id}
    /// </summary>
    [HttpGet("pops/{id:guid}")]
    public async Task<IActionResult> GetPOP(Guid id)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var pop = await _db.Set<POP>()
                .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId);

            if (pop == null)
                return NotFound(new { error = "POP não encontrado" });

            return Ok(pop);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter POP {Id}", id);
            return StatusCode(500, new { error = "Erro ao carregar POP" });
        }
    }

    /// <summary>
    /// Cria novo POP
    /// POST /api/qualidade/pops
    /// </summary>
    [HttpPost("pops")]
    public async Task<IActionResult> CriarPOP([FromBody] CriarPOPRequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            var establishmentId = GetEstablishmentId();

            // Gerar código sequencial
            var ultimoCodigo = await _db.Set<POP>()
                .Where(p => p.EstablishmentId == establishmentId)
                .MaxAsync(p => (int?)p.SequenceNumber) ?? 0;

            var pop = new POP
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = $"POP-{ultimoCodigo + 1:D4}",
                SequenceNumber = ultimoCodigo + 1,
                Title = request.Title,
                Category = request.Category,
                Objective = request.Objective,
                Scope = request.Scope,
                Definitions = request.Definitions,
                Responsibilities = request.Responsibilities,
                Procedures = request.Procedures,
                References = request.References,
                Records = request.Records,
                Version = 1,
                Status = "RASCUNHO",
                CreatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Set<POP>().Add(pop);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPOP), new { id = pop.Id }, new
            {
                success = true,
                message = "POP criado com sucesso",
                id = pop.Id,
                code = pop.Code
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar POP");
            return StatusCode(500, new { error = "Erro ao criar POP" });
        }
    }

    /// <summary>
    /// Aprovar POP
    /// POST /api/qualidade/pops/{id}/aprovar
    /// </summary>
    [HttpPost("pops/{id:guid}/aprovar")]
    public async Task<IActionResult> AprovarPOP(Guid id)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            var establishmentId = GetEstablishmentId();

            var pop = await _db.Set<POP>()
                .FirstOrDefaultAsync(p => p.Id == id && p.EstablishmentId == establishmentId);

            if (pop == null)
                return NotFound(new { error = "POP não encontrado" });

            pop.Status = "VIGENTE";
            pop.EffectiveDate = DateTime.UtcNow;
            pop.ReviewDate = DateTime.UtcNow.AddYears(1); // Revisão em 1 ano
            pop.ApprovedByEmployeeId = employee.Id;
            pop.ApprovedAt = DateTime.UtcNow;
            pop.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = "POP aprovado e vigente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao aprovar POP {Id}", id);
            return StatusCode(500, new { error = "Erro ao aprovar POP" });
        }
    }

    /// <summary>
    /// Categorias de POP disponíveis
    /// GET /api/qualidade/pops/categorias
    /// </summary>
    [HttpGet("pops/categorias")]
    public IActionResult GetCategoriasPOP()
    {
        var categorias = new[]
        {
            new { codigo = "MANIPULACAO", nome = "Manipulação", descricao = "Procedimentos de produção" },
            new { codigo = "CONTROLE_QUALIDADE", nome = "Controle de Qualidade", descricao = "Análises e testes" },
            new { codigo = "ESTOQUE", nome = "Estoque", descricao = "Recebimento, armazenamento, expedição" },
            new { codigo = "LIMPEZA", nome = "Limpeza e Sanitização", descricao = "Higienização de áreas e equipamentos" },
            new { codigo = "EQUIPAMENTOS", nome = "Equipamentos", descricao = "Operação e manutenção" },
            new { codigo = "SEGURANCA", nome = "Segurança", descricao = "EPI, emergências, descarte" },
            new { codigo = "CONTROLADOS", nome = "Substâncias Controladas", descricao = "SNGPC e Portaria 344" },
            new { codigo = "ATENDIMENTO", nome = "Atendimento", descricao = "Recepção de receitas e clientes" },
            new { codigo = "ADMINISTRATIVO", nome = "Administrativo", descricao = "Documentação e registros" }
        };
        return Ok(categorias);
    }

    #endregion

    #region CAPAs - Ações Corretivas e Preventivas

    /// <summary>
    /// Lista CAPAs do estabelecimento
    /// GET /api/qualidade/capas
    /// </summary>
    [HttpGet("capas")]
    public async Task<IActionResult> ListarCAPAs(
        [FromQuery] string? tipo = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var query = _db.Set<CAPA>()
                .Where(c => c.EstablishmentId == establishmentId);

            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(c => c.Type == tipo);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Code,
                    c.Type,
                    c.Title,
                    c.Priority,
                    c.Status,
                    c.DueDate,
                    c.CompletedAt,
                    c.CreatedAt,
                    atrasada = c.DueDate < DateTime.UtcNow && c.Status != "CONCLUIDA" && c.Status != "CANCELADA"
                })
                .ToListAsync();

            return Ok(new { items, total, page, pageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar CAPAs");
            return StatusCode(500, new { error = "Erro ao carregar CAPAs" });
        }
    }

    /// <summary>
    /// Cria nova CAPA
    /// POST /api/qualidade/capas
    /// </summary>
    [HttpPost("capas")]
    public async Task<IActionResult> CriarCAPA([FromBody] CriarCAPARequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            var establishmentId = GetEstablishmentId();

            var ultimoCodigo = await _db.Set<CAPA>()
                .Where(c => c.EstablishmentId == establishmentId)
                .MaxAsync(c => (int?)c.SequenceNumber) ?? 0;

            var prefixo = request.Type == "CORRETIVA" ? "AC" : "AP";
            var ano = DateTime.UtcNow.ToString("yy");

            var capa = new CAPA
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                Code = $"{prefixo}-{ano}{ultimoCodigo + 1:D2}",
                SequenceNumber = ultimoCodigo + 1,
                Type = request.Type,
                Title = request.Title,
                Description = request.Description,
                Source = request.Source,
                RootCauseAnalysis = request.RootCauseAnalysis,
                ProposedActions = request.ProposedActions,
                Priority = request.Priority,
                Status = "ABERTA",
                DueDate = request.DueDate,
                ResponsibleEmployeeId = request.ResponsibleEmployeeId,
                CreatedByEmployeeId = employee.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Set<CAPA>().Add(capa);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCAPA), new { id = capa.Id }, new
            {
                success = true,
                message = "CAPA criada com sucesso",
                id = capa.Id,
                code = capa.Code
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar CAPA");
            return StatusCode(500, new { error = "Erro ao criar CAPA" });
        }
    }

    /// <summary>
    /// Obtém detalhes de uma CAPA
    /// GET /api/qualidade/capas/{id}
    /// </summary>
    [HttpGet("capas/{id:guid}")]
    public async Task<IActionResult> GetCAPA(Guid id)
    {
        try
        {
            var establishmentId = GetEstablishmentId();

            var capa = await _db.Set<CAPA>()
                .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);

            if (capa == null)
                return NotFound(new { error = "CAPA não encontrada" });

            return Ok(capa);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter CAPA {Id}", id);
            return StatusCode(500, new { error = "Erro ao carregar CAPA" });
        }
    }

    /// <summary>
    /// Atualiza status da CAPA
    /// PUT /api/qualidade/capas/{id}/status
    /// </summary>
    [HttpPut("capas/{id:guid}/status")]
    public async Task<IActionResult> AtualizarStatusCAPA(Guid id, [FromBody] AtualizarStatusCAPARequest request)
    {
        try
        {
            var employee = GetCurrentEmployee();
            if (employee == null)
                return Unauthorized(new { error = "Autenticação necessária" });

            var establishmentId = GetEstablishmentId();

            var capa = await _db.Set<CAPA>()
                .FirstOrDefaultAsync(c => c.Id == id && c.EstablishmentId == establishmentId);

            if (capa == null)
                return NotFound(new { error = "CAPA não encontrada" });

            capa.Status = request.Status;
            capa.UpdatedAt = DateTime.UtcNow;

            if (request.Status == "CONCLUIDA")
            {
                capa.CompletedAt = DateTime.UtcNow;
                capa.VerificationResults = request.VerificationResults;
                capa.EffectivenessEvaluation = request.EffectivenessEvaluation;
            }

            await _db.SaveChangesAsync();

            return Ok(new { success = true, message = $"Status atualizado para {request.Status}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar status da CAPA {Id}", id);
            return StatusCode(500, new { error = "Erro ao atualizar CAPA" });
        }
    }

    #endregion

    #region Indicadores de Qualidade

    /// <summary>
    /// Indicadores gerais de qualidade
    /// GET /api/qualidade/indicadores
    /// </summary>
    [HttpGet("indicadores")]
    public async Task<IActionResult> GetIndicadores([FromQuery] int meses = 3)
    {
        try
        {
            var establishmentId = GetEstablishmentId();
            var dataInicio = DateTime.UtcNow.AddMonths(-meses);

            // POPs
            var totalPOPs = await _db.Set<POP>()
                .CountAsync(p => p.EstablishmentId == establishmentId && p.Status == "VIGENTE");
            
            var popsVencidos = await _db.Set<POP>()
                .CountAsync(p => p.EstablishmentId == establishmentId && 
                                p.Status == "VIGENTE" && p.ReviewDate < DateTime.UtcNow);

            // CAPAs
            var capasAbertas = await _db.Set<CAPA>()
                .CountAsync(c => c.EstablishmentId == establishmentId && 
                                c.Status != "CONCLUIDA" && c.Status != "CANCELADA");
            
            var capasAtrasadas = await _db.Set<CAPA>()
                .CountAsync(c => c.EstablishmentId == establishmentId && 
                                c.Status != "CONCLUIDA" && c.Status != "CANCELADA" &&
                                c.DueDate < DateTime.UtcNow);

            var capasConcluidas = await _db.Set<CAPA>()
                .CountAsync(c => c.EstablishmentId == establishmentId && 
                                c.Status == "CONCLUIDA" && c.CompletedAt >= dataInicio);

            // Produção
            var ordensTotal = await _db.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && o.CreatedAt >= dataInicio);
            
            var ordensReprovadas = await _db.ManipulationOrders
                .CountAsync(o => o.EstablishmentId == establishmentId && 
                                o.CreatedAt >= dataInicio && o.Status == "REPROVADA");

            var taxaAprovacao = ordensTotal > 0 
                ? Math.Round((decimal)(ordensTotal - ordensReprovadas) / ordensTotal * 100, 1) 
                : 100;

            // Estoque - CORRIGIDO: Acessar via RawMaterial.EstablishmentId
            var lotesVencidos = await _db.Batches
                .Include(b => b.RawMaterial)
                .CountAsync(b => b.RawMaterial != null && 
                                b.RawMaterial.EstablishmentId == establishmentId && 
                                b.ExpiryDate < DateTime.UtcNow && 
                                b.CurrentQuantity > 0);

            var lotesQuarentena = await _db.Batches
                .Include(b => b.RawMaterial)
                .CountAsync(b => b.RawMaterial != null && 
                                b.RawMaterial.EstablishmentId == establishmentId && 
                                b.Status == "QUARENTENA");

            return Ok(new
            {
                pops = new
                {
                    total = totalPOPs,
                    vencidos = popsVencidos,
                    conformidade = totalPOPs > 0 
                        ? Math.Round((decimal)(totalPOPs - popsVencidos) / totalPOPs * 100, 1) 
                        : 100
                },
                capas = new
                {
                    abertas = capasAbertas,
                    atrasadas = capasAtrasadas,
                    concluidas = capasConcluidas
                },
                producao = new
                {
                    total = ordensTotal,
                    reprovadas = ordensReprovadas,
                    taxaAprovacao
                },
                estoque = new
                {
                    lotesVencidos,
                    lotesQuarentena
                },
                periodo = $"Últimos {meses} meses",
                atualizadoEm = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter indicadores de qualidade");
            return StatusCode(500, new { error = "Erro ao carregar indicadores" });
        }
    }

    #endregion
}

// ════════════════════════════════════════════════════════════════════════════
// DTOs
// ════════════════════════════════════════════════════════════════════════════

public class CriarPOPRequest
{
    [Required]
    public string Title { get; set; } = "";
    [Required]
    public string Category { get; set; } = "";
    public string? Objective { get; set; }
    public string? Scope { get; set; }
    public string? Definitions { get; set; }
    public string? Responsibilities { get; set; }
    [Required]
    public string Procedures { get; set; } = "";
    public string? References { get; set; }
    public string? Records { get; set; }
}

public class CriarCAPARequest
{
    [Required]
    public string Type { get; set; } = "CORRETIVA"; // CORRETIVA ou PREVENTIVA
    [Required]
    public string Title { get; set; } = "";
    [Required]
    public string Description { get; set; } = "";
    public string? Source { get; set; } // Auditoria, Reclamação, Desvio, etc.
    public string? RootCauseAnalysis { get; set; }
    public string? ProposedActions { get; set; }
    public string Priority { get; set; } = "MEDIA"; // ALTA, MEDIA, BAIXA
    public DateTime DueDate { get; set; }
    public Guid? ResponsibleEmployeeId { get; set; }
}

public class AtualizarStatusCAPARequest
{
    [Required]
    public string Status { get; set; } = ""; // ABERTA, EM_ANDAMENTO, VERIFICACAO, CONCLUIDA, CANCELADA
    public string? VerificationResults { get; set; }
    public string? EffectivenessEvaluation { get; set; }
}
