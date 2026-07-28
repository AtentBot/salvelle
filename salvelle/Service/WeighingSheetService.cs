using Data;
using Microsoft.EntityFrameworkCore;
using Models.Pharmacy;
using System.Text;

namespace Service;

/// <summary>
/// Serviço para geração de Fichas de Pesagem conforme RDC 67/2007
/// </summary>
public class WeighingSheetService
{
    private readonly AppDbContext _context;

    public WeighingSheetService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gera HTML da Ficha de Pesagem para impressão
    /// </summary>
    public async Task<string> GenerateWeighingSheetHtml(Guid orderId)
    {
        var order = await _context.ManipulationOrders
            .Include(o => o.Formula)
                .ThenInclude(f => f!.Components)
                    .ThenInclude(c => c.RawMaterial)
            .Include(o => o.RequestedByEmployee)
            .Include(o => o.Establishment)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new Exception("Ordem de manipulação não encontrada");

        var sb = new StringBuilder();

        // Header HTML
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang='pt-BR'>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset='UTF-8'>");
        sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        sb.AppendLine("    <title>Ficha de Pesagem - " + order.OrderNumber + "</title>");
        sb.AppendLine(GetStyles());
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Cabeçalho da Farmácia
        sb.AppendLine("<div class='header'>");
        sb.AppendLine($"    <h1>{order.Establishment?.NomeFantasia ?? "Farmácia de Manipulação"}</h1>");
        sb.AppendLine($"    <p>CNPJ: {order.Establishment?.Cnpj ?? "N/A"}</p>");
        sb.AppendLine($"    <p>{order.Establishment?.Street}, {order.Establishment?.Number} - {order.Establishment?.City}/{order.Establishment?.State}</p>");
        sb.AppendLine("</div>");

        // Título
        sb.AppendLine("<div class='title'>");
        sb.AppendLine("    <h2>FICHA DE PESAGEM E MANIPULAÇÃO</h2>");
        sb.AppendLine($"    <p>Conforme RDC 67/2007 - ANVISA</p>");
        sb.AppendLine("</div>");

        // Informações da Ordem
        sb.AppendLine("<div class='order-info'>");
        sb.AppendLine("    <table class='info-table'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine($"            <td><strong>Ordem Nº:</strong> {order.OrderNumber}</td>");
        sb.AppendLine($"            <td><strong>Data:</strong> {order.OrderDate:dd/MM/yyyy HH:mm}</td>");
        sb.AppendLine($"            <td><strong>Previsão:</strong> {order.ExpectedDate:dd/MM/yyyy}</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine($"            <td colspan='2'><strong>Cliente:</strong> {order.CustomerName}</td>");
        sb.AppendLine($"            <td><strong>Telefone:</strong> {order.CustomerPhone ?? "N/A"}</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Informações da Prescrição (se houver)
        if (!string.IsNullOrEmpty(order.PrescriptionNumber))
        {
            sb.AppendLine("<div class='prescription-info'>");
            sb.AppendLine("    <table class='info-table'>");
            sb.AppendLine("        <tr>");
            sb.AppendLine($"            <td><strong>Prescrição Nº:</strong> {order.PrescriptionNumber}</td>");
            sb.AppendLine($"            <td><strong>Prescritor:</strong> {order.PrescriberName ?? "N/A"}</td>");
            sb.AppendLine($"            <td><strong>CRM/CRO:</strong> {order.PrescriberRegistration ?? "N/A"}</td>");
            sb.AppendLine("        </tr>");
            sb.AppendLine("    </table>");
            sb.AppendLine("</div>");
        }

        // Informações da Fórmula
        sb.AppendLine("<div class='formula-info'>");
        sb.AppendLine("    <table class='info-table'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine($"            <td><strong>Fórmula:</strong> {order.Formula?.Name ?? "Fórmula Livre"}</td>");
        sb.AppendLine($"            <td><strong>Código:</strong> {order.Formula?.Code ?? "N/A"}</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine($"            <td><strong>Forma Farmacêutica:</strong> {order.Formula?.PharmaceuticalForm ?? "N/A"}</td>");
        sb.AppendLine($"            <td><strong>Quantidade:</strong> {order.QuantityToProduce} {order.Unit}</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Tabela de Componentes para Pesagem
        sb.AppendLine("<div class='components-section'>");
        sb.AppendLine("    <h3>COMPONENTES PARA PESAGEM</h3>");
        sb.AppendLine("    <table class='components-table'>");
        sb.AppendLine("        <thead>");
        sb.AppendLine("            <tr>");
        sb.AppendLine("                <th style='width: 5%'>Item</th>");
        sb.AppendLine("                <th style='width: 25%'>Matéria-Prima</th>");
        sb.AppendLine("                <th style='width: 10%'>Qtd Unit.</th>");
        sb.AppendLine("                <th style='width: 10%'>Qtd Total</th>");
        sb.AppendLine("                <th style='width: 10%'>Lote MP</th>");
        sb.AppendLine("                <th style='width: 10%'>Peso Real</th>");
        sb.AppendLine("                <th style='width: 10%'>Desvio</th>");
        sb.AppendLine("                <th style='width: 10%'>Rubrica</th>");
        sb.AppendLine("                <th style='width: 10%'>Conferência</th>");
        sb.AppendLine("            </tr>");
        sb.AppendLine("        </thead>");
        sb.AppendLine("        <tbody>");

        int itemNumber = 1;
        if (order.Formula?.Components != null)
        {
            foreach (var component in order.Formula.Components.OrderBy(c => c.OrderIndex))
            {
                var quantityTotal = component.Quantity * order.QuantityToProduce;

                // Buscar lotes disponíveis
                var availableBatches = await _context.Batches
                    .Where(b => b.RawMaterialId == component.RawMaterialId &&
                               b.Status.ToUpper() == "APROVADO" &&
                               b.CurrentQuantity > 0 &&
                               b.ExpiryDate > DateTime.UtcNow)
                    .OrderBy(b => b.ExpiryDate)
                    .Select(b => b.BatchNumber)
                    .Take(3)
                    .ToListAsync();

                var batchSuggestion = availableBatches.Any() ? string.Join(", ", availableBatches) : "N/A";

                sb.AppendLine("            <tr>");
                sb.AppendLine($"                <td class='center'>{itemNumber++}</td>");
                sb.AppendLine($"                <td>{component.RawMaterial?.Name ?? "N/A"}<br/><small class='dcb'>DCB: {component.RawMaterial?.DcbCode ?? "-"}</small></td>");
                sb.AppendLine($"                <td class='center'>{component.Quantity:F4} {component.Unit}</td>");
                sb.AppendLine($"                <td class='center'><strong>{quantityTotal:F4} {component.Unit}</strong></td>");
                sb.AppendLine($"                <td class='input-cell'><small>{batchSuggestion}</small></td>");
                sb.AppendLine($"                <td class='input-cell'></td>");
                sb.AppendLine($"                <td class='input-cell'></td>");
                sb.AppendLine($"                <td class='input-cell'></td>");
                sb.AppendLine($"                <td class='input-cell'></td>");
                sb.AppendLine("            </tr>");
            }
        }

        // Linhas extras para componentes adicionais
        for (int i = 0; i < 3; i++)
        {
            sb.AppendLine("            <tr class='extra-row'>");
            sb.AppendLine($"                <td class='center'>{itemNumber++}</td>");
            sb.AppendLine("                <td></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("                <td class='input-cell'></td>");
            sb.AppendLine("            </tr>");
        }

        sb.AppendLine("        </tbody>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Instruções de Preparo
        sb.AppendLine("<div class='instructions-section'>");
        sb.AppendLine("    <h3>INSTRUÇÕES DE PREPARO</h3>");
        sb.AppendLine("    <div class='instructions-box'>");
        sb.AppendLine($"        {order.Formula?.PreparationInstructions ?? order.SpecialInstructions ?? "Seguir procedimento padrão da forma farmacêutica."}");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        // Registro de Perdas e Sobras
        sb.AppendLine("<div class='losses-section'>");
        sb.AppendLine("    <h3>REGISTRO DE PERDAS E SOBRAS</h3>");
        sb.AppendLine("    <table class='losses-table'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td style='width: 50%'>");
        sb.AppendLine("                <strong>Quantidade Produzida:</strong><br/>");
        sb.AppendLine("                <div class='input-line'></div>");
        sb.AppendLine("            </td>");
        sb.AppendLine("            <td style='width: 50%'>");
        sb.AppendLine("                <strong>Rendimento (%):</strong><br/>");
        sb.AppendLine("                <div class='input-line'></div>");
        sb.AppendLine("            </td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>");
        sb.AppendLine("                <strong>Perdas (quantidade/motivo):</strong><br/>");
        sb.AppendLine("                <div class='input-line'></div>");
        sb.AppendLine("            </td>");
        sb.AppendLine("            <td>");
        sb.AppendLine("                <strong>Sobras (quantidade/destino):</strong><br/>");
        sb.AppendLine("                <div class='input-line'></div>");
        sb.AppendLine("            </td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Controle de Qualidade
        sb.AppendLine("<div class='quality-section'>");
        sb.AppendLine("    <h3>CONTROLE DE QUALIDADE</h3>");
        sb.AppendLine("    <table class='quality-table'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <th>Parâmetro</th>");
        sb.AppendLine("            <th>Especificação</th>");
        sb.AppendLine("            <th>Resultado</th>");
        sb.AppendLine("            <th>Conforme</th>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>Aspecto</td>");
        sb.AppendLine("            <td>Característico da forma</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell center'>☐ S ☐ N</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>Cor</td>");
        sb.AppendLine("            <td>Característico</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell center'>☐ S ☐ N</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>Odor</td>");
        sb.AppendLine("            <td>Característico</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell center'>☐ S ☐ N</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>Peso/Volume Final</td>");
        sb.AppendLine($"            <td>{order.QuantityToProduce} {order.Unit} ± 5%</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell center'>☐ S ☐ N</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>pH (se aplicável)</td>");
        sb.AppendLine("            <td>Conforme forma</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell center'>☐ S ☐ N ☐ N/A</td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Assinaturas
        sb.AppendLine("<div class='signatures-section'>");
        sb.AppendLine("    <h3>CONFERÊNCIA DUPLA E ASSINATURAS</h3>");
        sb.AppendLine("    <table class='signatures-table'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <th style='width: 25%'>Função</th>");
        sb.AppendLine("            <th style='width: 25%'>Nome</th>");
        sb.AppendLine("            <th style='width: 25%'>Assinatura</th>");
        sb.AppendLine("            <th style='width: 15%'>Data/Hora</th>");
        sb.AppendLine("            <th style='width: 10%'>CRF</th>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>Manipulador</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td>Conferente</td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td><strong>Farmacêutico RT</strong></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("            <td class='input-cell'></td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Rotulagem
        sb.AppendLine("<div class='label-section'>");
        sb.AppendLine("    <h3>INFORMAÇÕES PARA ROTULAGEM</h3>");
        sb.AppendLine("    <table class='label-table'>");
        sb.AppendLine("        <tr>");
        sb.AppendLine($"            <td><strong>Lote:</strong></td>");
        sb.AppendLine($"            <td class='input-cell' style='width: 30%'></td>");
        sb.AppendLine($"            <td><strong>Validade:</strong></td>");
        sb.AppendLine($"            <td class='input-cell' style='width: 30%'></td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td colspan='4'>");
        sb.AppendLine($"                <strong>Armazenamento:</strong> {order.Formula?.StorageInstructions ?? "Conservar em local fresco e seco, ao abrigo da luz."}");
        sb.AppendLine("            </td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("        <tr>");
        sb.AppendLine("            <td colspan='4'>");
        sb.AppendLine($"                <strong>Posologia:</strong> {order.Formula?.UsageInstructions ?? "Conforme prescrição médica."}");
        sb.AppendLine("            </td>");
        sb.AppendLine("        </tr>");
        sb.AppendLine("    </table>");
        sb.AppendLine("</div>");

        // Observações
        sb.AppendLine("<div class='observations-section'>");
        sb.AppendLine("    <h3>OBSERVAÇÕES</h3>");
        sb.AppendLine("    <div class='observations-box'>");
        if (!string.IsNullOrEmpty(order.SpecialInstructions))
        {
            sb.AppendLine($"        <p>{order.SpecialInstructions}</p>");
        }
        sb.AppendLine("        <div class='observation-lines'>");
        sb.AppendLine("            <div class='input-line'></div>");
        sb.AppendLine("            <div class='input-line'></div>");
        sb.AppendLine("            <div class='input-line'></div>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</div>");

        // Rodapé
        sb.AppendLine("<div class='footer'>");
        sb.AppendLine($"    <p>Documento gerado em: {Helpers.BrazilTime.Now:dd/MM/yyyy HH:mm:ss}</p>");
        sb.AppendLine($"    <p>Sistema Salvelle - Gestão de Farmácia de Manipulação</p>");
        sb.AppendLine("</div>");

        // Script para impressão automática
        sb.AppendLine("<script>");
        sb.AppendLine("    window.onload = function() {");
        sb.AppendLine("        // window.print();");
        sb.AppendLine("    };");
        sb.AppendLine("</script>");

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private string GetStyles()
    {
        return @"
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        
        body {
            font-family: 'Segoe UI', Arial, sans-serif;
            font-size: 11px;
            line-height: 1.4;
            padding: 15px;
            max-width: 210mm;
            margin: 0 auto;
        }
        
        .header {
            text-align: center;
            border-bottom: 2px solid #333;
            padding-bottom: 10px;
            margin-bottom: 10px;
        }
        
        .header h1 {
            font-size: 18px;
            color: #1a5f7a;
            margin-bottom: 5px;
        }
        
        .header p {
            font-size: 10px;
            color: #666;
        }
        
        .title {
            text-align: center;
            background: #1a5f7a;
            color: white;
            padding: 8px;
            margin-bottom: 15px;
        }
        
        .title h2 {
            font-size: 14px;
            margin-bottom: 2px;
        }
        
        .title p {
            font-size: 9px;
        }
        
        h3 {
            font-size: 12px;
            color: #1a5f7a;
            border-bottom: 1px solid #1a5f7a;
            padding-bottom: 3px;
            margin-bottom: 8px;
        }
        
        .info-table {
            width: 100%;
            margin-bottom: 10px;
        }
        
        .info-table td {
            padding: 4px 8px;
            border: 1px solid #ddd;
            background: #f9f9f9;
        }
        
        .components-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .components-table th {
            background: #1a5f7a;
            color: white;
            padding: 6px 4px;
            font-size: 10px;
            text-align: center;
        }
        
        .components-table td {
            border: 1px solid #ccc;
            padding: 6px 4px;
            font-size: 10px;
        }
        
        .components-table .center {
            text-align: center;
        }
        
        .components-table .input-cell {
            background: #fffef0;
            min-height: 25px;
        }
        
        .components-table .dcb {
            color: #888;
            font-size: 8px;
        }
        
        .extra-row td {
            height: 30px;
        }
        
        .instructions-box {
            border: 1px solid #ddd;
            padding: 10px;
            background: #f9f9f9;
            margin-bottom: 15px;
            min-height: 60px;
        }
        
        .losses-table {
            width: 100%;
            margin-bottom: 15px;
        }
        
        .losses-table td {
            padding: 8px;
            border: 1px solid #ddd;
            vertical-align: top;
        }
        
        .input-line {
            border-bottom: 1px solid #333;
            height: 25px;
            margin: 5px 0;
        }
        
        .quality-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .quality-table th, .quality-table td {
            border: 1px solid #ccc;
            padding: 5px;
            text-align: center;
            font-size: 10px;
        }
        
        .quality-table th {
            background: #e0e0e0;
        }
        
        .quality-table .input-cell {
            background: #fffef0;
            min-height: 20px;
        }
        
        .signatures-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .signatures-table th, .signatures-table td {
            border: 1px solid #ccc;
            padding: 8px;
            text-align: center;
            font-size: 10px;
        }
        
        .signatures-table th {
            background: #1a5f7a;
            color: white;
        }
        
        .signatures-table .input-cell {
            background: #fffef0;
            height: 40px;
        }
        
        .label-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 15px;
        }
        
        .label-table td {
            border: 1px solid #ddd;
            padding: 6px;
        }
        
        .label-table .input-cell {
            background: #fffef0;
        }
        
        .observations-box {
            border: 1px solid #ddd;
            padding: 10px;
            min-height: 60px;
        }
        
        .footer {
            text-align: center;
            margin-top: 20px;
            padding-top: 10px;
            border-top: 1px solid #ddd;
            font-size: 9px;
            color: #888;
        }
        
        @media print {
            body {
                padding: 10px;
            }
            
            .no-print {
                display: none;
            }
            
            @page {
                size: A4;
                margin: 10mm;
            }
        }
    </style>";
    }
}