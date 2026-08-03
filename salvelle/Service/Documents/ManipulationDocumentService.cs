using Microsoft.EntityFrameworkCore;
using Data;
using Models;
using Models.Pharmacy;
using Models.Employees;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Service.Documents;

/// <summary>
/// Serviço de geração de documentos PDF para manipulação farmacêutica
/// - Ficha de Manipulação (documento de produção)
/// - Certificado de Manipulação (documento final para cliente)
/// </summary>
public class ManipulationDocumentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ManipulationDocumentService> _logger;

    public ManipulationDocumentService(
        AppDbContext context, 
        ILogger<ManipulationDocumentService> logger)
    {
        _context = context;
        _logger = logger;
        
        // Configurar licença QuestPDF (Community para uso comercial limitado)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ════════════════════════════════════════════════════════════════════════
    // FICHA DE MANIPULAÇÃO (Documento interno de produção)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera a ficha de manipulação para uso no laboratório
    /// </summary>
    public async Task<byte[]> GenerateManipulationSheetAsync(Guid orderId, Guid establishmentId)
    {
        // Escopo de tenant obrigatório: o PDF expõe PII do paciente (nome, telefone,
        // prescritor, composição). Sem o filtro, enumerar orderId vazava documento alheio.
        var order = await _context.ManipulationOrders
            .Include(o => o.Establishment)
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .Include(o => o.Steps)
                .ThenInclude(s => s.PerformedByEmployee)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
            throw new InvalidOperationException("Ordem não encontrada");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, order));
                page.Content().Element(c => ComposeManipulationSheetContent(c, order));
                page.Footer().Element(c => ComposeFooter(c, order));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, ManipulationOrder order)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                // Establishment: usar NomeFantasia ou RazaoSocial
                col.Item().Text(order.Establishment?.NomeFantasia ?? order.Establishment?.RazaoSocial ?? "Farmácia")
                    .FontSize(16).Bold();
                col.Item().Text($"CNPJ: {order.Establishment?.Cnpj ?? "N/A"}");
            });

            row.ConstantItem(200).Column(col =>
            {
                col.Item().AlignRight().Text("FICHA DE MANIPULAÇÃO")
                    .FontSize(14).Bold().FontColor(Colors.Blue.Medium);
                col.Item().AlignRight().Text($"OM: {order.OrderNumber}").FontSize(12).Bold();
                col.Item().AlignRight().Text($"Data: {order.OrderDate:dd/MM/yyyy}");
            });
        });

        container.PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Medium);
    }

    private void ComposeManipulationSheetContent(IContainer container, ManipulationOrder order)
    {
        container.Column(col =>
        {
            // ═══ DADOS DO CLIENTE/PACIENTE ═══
            col.Item().PaddingTop(10).Text("DADOS DO CLIENTE/PACIENTE").Bold().FontSize(11);
            col.Item().Border(1).Padding(8).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    // ManipulationOrder tem CustomerName diretamente
                    c.Item().Text($"Nome: {order.CustomerName ?? "N/A"}");
                    c.Item().Text($"Telefone: {order.CustomerPhone ?? "N/A"}");
                });
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text($"Prioridade: {order.Priority}");
                    c.Item().Text($"Previsão: {order.ExpectedDate:dd/MM/yyyy}");
                });
            });

            // ═══ DADOS DA PRESCRIÇÃO ═══
            if (!string.IsNullOrEmpty(order.PrescriptionNumber))
            {
                col.Item().PaddingTop(10).Text("DADOS DA PRESCRIÇÃO").Bold().FontSize(11);
                col.Item().Border(1).Padding(8).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Prescritor: {order.PrescriberName ?? "N/A"}");
                        c.Item().Text($"Registro: {order.PrescriberRegistration ?? "N/A"}");
                    });
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Nº Receita: {order.PrescriptionNumber}");
                    });
                });
            }

            // ═══ DADOS DA FÓRMULA ═══
            col.Item().PaddingTop(10).Text("FÓRMULA").Bold().FontSize(11);
            col.Item().Border(1).Padding(8).Column(c =>
            {
                c.Item().Text($"Tipo: {order.Formula?.Name ?? "Fórmula Livre"}");
                c.Item().Text($"Quantidade: {order.QuantityToProduce} {order.Unit}");
                
                if (!string.IsNullOrEmpty(order.SpecialInstructions))
                    c.Item().PaddingTop(5).Text($"Instruções: {order.SpecialInstructions}");
            });

            // ═══ TABELA DE COMPONENTES ═══
            col.Item().PaddingTop(10).Text("COMPONENTES / MATÉRIAS-PRIMAS").Bold().FontSize(11);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Matéria-Prima").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Qtd Calc.").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Qtd Pesada").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Lote").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignCenter().Text("Conferência").Bold();
                });

                if (order.Formula?.Components != null)
                {
                    foreach (var comp in order.Formula.Components.OrderBy(c => c.OrderIndex))
                    {
                        var calculatedQty = comp.Quantity * (order.QuantityToProduce / 100m);
                        
                        table.Cell().Border(0.5f).Padding(4)
                            .Text($"{comp.RawMaterial?.Name ?? "N/A"} ({comp.ComponentType})");
                        table.Cell().Border(0.5f).Padding(4).AlignCenter()
                            .Text($"{calculatedQty:N4} {comp.Unit}");
                        table.Cell().Border(0.5f).Padding(4).AlignCenter().Text("________");
                        table.Cell().Border(0.5f).Padding(4).AlignCenter().Text("________");
                        table.Cell().Border(0.5f).Padding(4).AlignCenter().Text("________");
                    }
                }
            });

            // ═══ ETAPAS DE PRODUÇÃO ═══
            col.Item().PaddingTop(15).Text("ETAPAS DE PRODUÇÃO").Bold().FontSize(11);
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Etapa").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Data/Hora").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Responsável").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Assinatura").Bold();
                });

                var steps = new[] { "Separação", "Pesagem", "Mistura", "Envase", "Rotulagem", "Conferência", "Aprovação" };
                foreach (var stepName in steps)
                {
                    var executedStep = order.Steps?.FirstOrDefault(s => 
                        s.StepType.Equals(stepName.ToUpper().Replace("Ã", "A").Replace("Ç", "C"), StringComparison.OrdinalIgnoreCase));

                    table.Cell().Border(0.5f).Padding(4).Text(stepName);
                    table.Cell().Border(0.5f).Padding(4)
                        .Text(executedStep?.CompletedAt?.ToString("dd/MM HH:mm") ?? "____/____  ____:____");
                    table.Cell().Border(0.5f).Padding(4)
                        .Text(executedStep?.PerformedByEmployee?.FullName ?? "________________");
                    table.Cell().Border(0.5f).Padding(4).Text("________________");
                }
            });

            // ═══ OBSERVAÇÕES ═══
            col.Item().PaddingTop(15).Text("OBSERVAÇÕES").Bold().FontSize(11);
            col.Item().Border(1).Height(60).Padding(5)
                .Text(order.QualityNotes ?? "");

            // ═══ APROVAÇÃO FINAL ═══
            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("CONFERÊNCIA - FARMACÊUTICO").Bold();
                    c.Item().PaddingTop(30).Text("_________________________________");
                    c.Item().Text("Assinatura / Carimbo / CRF").FontSize(8);
                    c.Item().PaddingTop(5).Text("Data: ____/____/______");
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("LIBERAÇÃO FINAL").Bold();
                    c.Item().PaddingTop(30).Text("_________________________________");
                    c.Item().Text("Farmacêutico Responsável Técnico").FontSize(8);
                    c.Item().PaddingTop(5).Text("Data: ____/____/______");
                });
            });
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // CERTIFICADO DE MANIPULAÇÃO (Documento para cliente)
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gera o certificado de manipulação para entregar ao cliente
    /// </summary>
    public async Task<byte[]> GenerateCertificateAsync(Guid orderId, Guid establishmentId)
    {
        // Escopo de tenant obrigatório (PII do paciente) — ver GenerateManipulationSheetAsync.
        var order = await _context.ManipulationOrders
            .Include(o => o.Establishment)
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .Include(o => o.ApprovedByPharmacist)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
            throw new InvalidOperationException("Ordem não encontrada");

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeCertificateHeader(c, order));
                page.Content().Element(c => ComposeCertificateContent(c, order));
                page.Footer().Element(c => ComposeFooter(c, order));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeCertificateHeader(IContainer container, ManipulationOrder order)
    {
        container.Column(col =>
        {
            col.Item().AlignCenter().Text(order.Establishment?.NomeFantasia ?? order.Establishment?.RazaoSocial ?? "Farmácia de Manipulação")
                .FontSize(18).Bold();
            col.Item().AlignCenter().Text($"CNPJ: {order.Establishment?.Cnpj ?? "N/A"}")
                .FontSize(9);
            
            // Endereço do estabelecimento
            var address = BuildAddress(order.Establishment);
            if (!string.IsNullOrEmpty(address))
            {
                col.Item().AlignCenter().Text(address).FontSize(9);
            }
            
            col.Item().PaddingTop(15).AlignCenter()
                .Text("CERTIFICADO DE MANIPULAÇÃO")
                .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);
            
            col.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Medium);
        });
    }

    private void ComposeCertificateContent(IContainer container, ManipulationOrder order)
    {
        container.PaddingTop(20).Column(col =>
        {
            // Número da ordem
            col.Item().AlignCenter().Text($"Ordem de Manipulação Nº {order.OrderNumber}")
                .FontSize(12).Bold();
            col.Item().AlignCenter().Text($"Data de Manipulação: {order.CompletionDate?.ToString("dd/MM/yyyy") ?? order.OrderDate.ToString("dd/MM/yyyy")}")
                .FontSize(10);

            // Dados do paciente
            col.Item().PaddingTop(20).Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
            {
                c.Item().Text("DADOS DO PACIENTE").Bold().FontSize(11);
                c.Item().PaddingTop(5).Text($"Nome: {order.CustomerName ?? "N/A"}");
            });

            // Composição
            col.Item().PaddingTop(15).Text("COMPOSIÇÃO").Bold().FontSize(11);
            col.Item().Border(1).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(8).Text("Componente").Bold();
                    header.Cell().Background(Colors.Blue.Lighten4).Padding(8).AlignCenter().Text("Quantidade").Bold();
                });

                if (order.Formula?.Components != null)
                {
                    foreach (var comp in order.Formula.Components.OrderBy(c => c.OrderIndex))
                    {
                        var displayQty = comp.Quantity * (order.QuantityToProduce / 100m);
                        table.Cell().Border(0.5f).Padding(6).Text(comp.RawMaterial?.Name ?? "N/A");
                        table.Cell().Border(0.5f).Padding(6).AlignCenter().Text($"{displayQty:N2} {comp.Unit}");
                    }
                }
            });

            // Informações do produto
            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("FORMA FARMACÊUTICA").Bold().FontSize(9);
                    c.Item().Text(order.Formula?.Name ?? "N/E").FontSize(11);
                });
                row.ConstantItem(10);
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("QUANTIDADE").Bold().FontSize(9);
                    c.Item().Text($"{order.QuantityToProduce} {order.Unit}").FontSize(11);
                });
                row.ConstantItem(10);
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("VIA DE USO").Bold().FontSize(9);
                    c.Item().Text("Conforme prescrição").FontSize(11);
                });
            });

            // Posologia
            if (!string.IsNullOrEmpty(order.SpecialInstructions))
            {
                col.Item().PaddingTop(15).Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("POSOLOGIA / MODO DE USAR").Bold().FontSize(9);
                    c.Item().PaddingTop(5).Text(order.SpecialInstructions);
                });
            }

            // Validade e Lote
            col.Item().PaddingTop(15).Row(row =>
            {
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("LOTE").Bold().FontSize(9);
                    c.Item().Text(order.OrderNumber).FontSize(11);
                });
                row.ConstantItem(10);
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("DATA DE FABRICAÇÃO").Bold().FontSize(9);
                    c.Item().Text(order.CompletionDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(11);
                });
                row.ConstantItem(10);
                row.RelativeItem().Border(1).Padding(10).Column(c =>
                {
                    c.Item().Text("VALIDADE").Bold().FontSize(9);
                    // Usar ExpiryDate (não ExpirationDate)
                    c.Item().Text(order.ExpiryDate?.ToString("dd/MM/yyyy") ?? 
                        (order.CompletionDate?.AddMonths(6).ToString("dd/MM/yyyy") ?? "N/A")).FontSize(11);
                });
            });

            // Assinatura do farmacêutico
            col.Item().PaddingTop(30).AlignCenter().Column(c =>
            {
                c.Item().Text("_____________________________________________");
                c.Item().PaddingTop(5).Text(order.ApprovedByPharmacist?.FullName ?? "Farmacêutico Responsável").Bold();
                // Employee usa Crm + CrmState
                c.Item().Text($"CRF: {GetEmployeeCrf(order.ApprovedByPharmacist)}").FontSize(9);
            });

            // Aviso legal
            col.Item().PaddingTop(20).Background(Colors.Yellow.Lighten4).Padding(10).Column(c =>
            {
                c.Item().AlignCenter().Text("ATENÇÃO").Bold().FontSize(9);
                c.Item().AlignCenter().Text("Medicamento manipulado de uso exclusivo do paciente identificado.").FontSize(8);
                c.Item().AlignCenter().Text("Conservar em local fresco e seco, ao abrigo da luz e calor.").FontSize(8);
                c.Item().AlignCenter().Text("Manter fora do alcance de crianças.").FontSize(8);
            });
        });
    }

    private void ComposeFooter(IContainer container, ManipulationOrder order)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);
            col.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Impresso em: {Helpers.BrazilTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Página ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" de ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });
        });
    }

    // ════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ════════════════════════════════════════════════════════════════════════

    private string BuildAddress(Establishment? est)
    {
        if (est == null) return "";
        
        var parts = new List<string>();
        
        if (!string.IsNullOrEmpty(est.Street))
            parts.Add(est.Street);
        if (!string.IsNullOrEmpty(est.Number))
            parts.Add(est.Number);
        if (!string.IsNullOrEmpty(est.City))
            parts.Add(est.City);
        if (!string.IsNullOrEmpty(est.State))
            parts.Add(est.State);
            
        return string.Join(", ", parts);
    }

    private string GetEmployeeCrf(Employee? employee)
    {
        if (employee == null) return "N/A";
        
        // Employee usa Crm + CrmState
        if (!string.IsNullOrEmpty(employee.Crm))
        {
            return $"{employee.Crm}/{employee.CrmState ?? ""}";
        }
        return "N/A";
    }
}
