using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Models;
using Models.Pharmacy;
using Models.Employees;
using Data;
using DTOs.Prescriptions;
using DTOs;

namespace Service.Prescriptions;

/// <summary>
/// Serviço responsável pelo workflow de conversão de orçamento em venda
/// Fluxo: Orçamento Aprovado → Ordem de Manipulação → Venda → Pagamento → Caixa
/// </summary>
public class PrescriptionWorkflowService
{
    private readonly AppDbContext _context;
    private readonly ILogger<PrescriptionWorkflowService> _logger;
    private readonly OpenAIPrescriptionParserService _ocrService;
    private readonly IngredientMatcherService _matcherService;

    public PrescriptionWorkflowService(
        AppDbContext context,
        ILogger<PrescriptionWorkflowService> logger,
        OpenAIPrescriptionParserService ocrService,
        IngredientMatcherService matcherService)
    {
        _context = context;
        _logger = logger;
        _ocrService = ocrService;
        _matcherService = matcherService;
    }

    /// <summary>
    /// Processamento rápido de prescrição: Upload → OCR → Match → Retorna para revisão
    /// </summary>
    public async Task<QuickProcessResultDto> ProcessPrescriptionAsync(
        QuickProcessPrescriptionDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        var result = new QuickProcessResultDto
        {
            Success = false,
            Warnings = new List<string>(),
            UnmatchedIngredients = new List<string>()
        };

        try
        {
            _logger.LogInformation("Iniciando processamento rápido de receita para establishment {EstablishmentId}", establishmentId);

            // Validar entrada
            if (string.IsNullOrEmpty(dto.ImageBase64))
            {
                result.Message = "Imagem não fornecida";
                return result;
            }

            // ===== ETAPA 1: OCR com OpenAI Vision =====
            _logger.LogInformation("Etapa 1: Processando OCR...");
            OcrPrescriptionResultDto ocrResult;

            try
            {
                // Chama o serviço de OCR existente (OpenAIPrescriptionParserService)
                ocrResult = await _ocrService.ParsePrescriptionAsync(dto.ImageBase64, dto.ImageType);
                result.OcrResult = ocrResult;

                _logger.LogInformation("OCR concluído. Itens extraídos: {Count}", ocrResult?.Items?.Count ?? 0);

                if (ocrResult == null)
                {
                    result.Message = "Não foi possível processar a imagem.";
                    result.RequiresManualReview = true;
                    result.Success = true;
                    return result;
                }

                if (ocrResult.Items == null || ocrResult.Items.Count == 0)
                {
                    result.Message = "Não foi possível extrair ingredientes da receita. Verifique a qualidade da imagem.";
                    result.RequiresManualReview = true;
                    result.Success = true; // Sucesso parcial - permite revisão manual
                    return result;
                }
            }
            catch (Exception ocrEx)
            {
                _logger.LogError(ocrEx, "Erro no OCR");
                result.Message = $"Erro ao processar imagem: {ocrEx.Message}";
                return result;
            }

            // ===== ETAPA 2: Matching de Ingredientes =====
            _logger.LogInformation("Etapa 2: Matching de ingredientes ({Count} itens)...", ocrResult.Items.Count);

            try
            {
                // Converter OcrIngredientDto (do OCR) para OcrItemDto (do matcher)
                // OcrIngredientDto vem do OpenAIPrescriptionParserService (namespace DTOs.Prescriptions)
                // OcrItemDto é esperado pelo IngredientMatcherService (namespace DTOs)
                var ocrItems = ocrResult.Items.Select(i => new OcrItemDto
                {
                    Name = i.Name,           // Usa o setter que define Component
                    Quantity = i.Quantity,
                    Unit = i.Unit,
                    RawText = i.RawText,
                    Confidence = (decimal)i.Confidence
                }).ToList();

                // Chama o serviço de matching existente (IngredientMatcherService)
                // Retorna IngredientMatchResponseDto com List<IngredientMatchDto>
                var matchResponse = await _matcherService.FindMatchesAsync(ocrItems);

                // Converter IngredientMatchDto → IngredientMatchResultDto
                // IngredientMatchDto (namespace DTOs) → IngredientMatchResultDto (namespace DTOs.Prescriptions)
                if (matchResponse?.Matches != null)
                {
                    result.IngredientMatches = matchResponse.Matches.Select(m => new IngredientMatchResultDto
                    {
                        OcrText = m.OcrText,
                        OcrQuantity = m.Quantity ?? "",
                        OcrUnit = m.Unit ?? "",
                        // Converter RawMaterialSuggestionDto → OcrRawMaterialMatchDto
                        Suggestions = m.Suggestions?.Select(s => new OcrRawMaterialMatchDto
                        {
                            RawMaterialId = s.RawMaterialId,
                            Name = s.Name,
                            DcbCode = s.DciName,
                            Confidence = (double)s.Confidence,
                            InStock = s.InStock,
                            AvailableQuantity = s.AvailableQuantity,
                            Unit = s.Unit ?? ""
                        }).ToList() ?? new List<OcrRawMaterialMatchDto>(),
                        // BestMatch = primeira sugestão com maior confiança
                        BestMatch = m.Suggestions?.OrderByDescending(s => s.Confidence)
                            .Select(s => new OcrRawMaterialMatchDto
                            {
                                RawMaterialId = s.RawMaterialId,
                                Name = s.Name,
                                DcbCode = s.DciName,
                                Confidence = (double)s.Confidence,
                                InStock = s.InStock,
                                AvailableQuantity = s.AvailableQuantity,
                                Unit = s.Unit ?? ""
                            }).FirstOrDefault()
                    }).ToList();

                    // Identificar ingredientes não encontrados
                    foreach (var match in result.IngredientMatches)
                    {
                        if (match.BestMatch == null)
                        {
                            result.UnmatchedIngredients.Add(match.OcrText ?? "Desconhecido");
                            result.Warnings.Add($"Ingrediente '{match.OcrText}' não encontrado no estoque");
                        }
                        else if (match.BestMatch.Confidence < 0.7) // 70%
                        {
                            result.Warnings.Add($"Ingrediente '{match.OcrText}' com baixa confiança ({match.BestMatch.Confidence * 100:F0}%)");
                        }
                    }

                    if (result.UnmatchedIngredients.Count > 0)
                    {
                        result.RequiresManualReview = true;
                    }

                    _logger.LogInformation("Matching concluído. Encontrados: {Matched}, Não encontrados: {Unmatched}",
                        result.IngredientMatches.Count - result.UnmatchedIngredients.Count,
                        result.UnmatchedIngredients.Count);
                }
            }
            catch (Exception matchEx)
            {
                _logger.LogError(matchEx, "Erro no matching de ingredientes");
                result.Warnings.Add($"Erro ao buscar ingredientes no estoque: {matchEx.Message}");
                result.RequiresManualReview = true;
                // Não falha - permite continuar com revisão manual
            }

            // ===== ETAPA 3: Armazenar imagem no resultado =====
            // O PrescriptionFile será criado quando gerar o orçamento/prescrição definitiva
            // Isso evita o erro de FK (prescription_id = Guid.Empty)
            result.ImageBase64 = dto.ImageBase64;
            result.ImageType = dto.ImageType;
            _logger.LogInformation("Imagem armazenada no resultado para processamento posterior");

            result.Success = true;
            result.Message = result.RequiresManualReview
                ? "Receita processada. Alguns itens requerem revisão manual."
                : "Receita processada com sucesso!";

            _logger.LogInformation("Processamento concluído. Success: {Success}, RequiresManualReview: {ManualReview}",
                result.Success, result.RequiresManualReview);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro no processamento rápido de receita");
            result.Message = $"Erro ao processar receita: {ex.Message}";
            return result;
        }
    }

    private string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType?.ToLower() switch
        {
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/png" => "png",
            "application/pdf" => "pdf",
            _ => "jpg"
        };
    }

    /// <summary>
    /// Converte um orçamento aprovado em venda completa
    /// </summary>
    public async Task<(bool Success, string Message, Guid? SaleId, Guid? ManipulationOrderId)> ConvertQuoteToSaleAsync(
        Guid quoteId,
        string paymentMethod,
        decimal amountPaid,
        int installments,
        decimal discountAmount,
        string? discountReason,
        Guid establishmentId,
        Guid employeeId,
        string? cardBrand = null,
        string? cardLastDigits = null,
        string? nsu = null,
        string? authorizationCode = null,
        string? pixTransactionId = null,
        string? observations = null)
    {
        try
        {
            // 1. Buscar e validar orçamento
            var quote = await _context.PrescriptionQuotes
                .FirstOrDefaultAsync(q => q.Id == quoteId && q.EstablishmentId == establishmentId);

            if (quote == null)
                return (false, "Orçamento não encontrado", null, null);

            if (quote.Status != "APROVADO" && quote.Status != "PENDENTE")
                return (false, $"Orçamento não pode ser convertido. Status atual: {quote.Status}", null, null);

            // Verificar se já foi convertido
            if (quote.SaleId.HasValue)
                return (false, "Este orçamento já foi convertido em venda.", null, null);

            // 2. Verificar se há caixa aberto
            var openCashRegister = await _context.CashRegisters
                .FirstOrDefaultAsync(c => c.EstablishmentId == establishmentId &&
                                         c.Status == "ABERTO" &&
                                         c.ClosingDate == null);

            if (openCashRegister == null)
                return (false, "Não há caixa aberto. Abra um caixa antes de realizar vendas.", null, null);

            // 3. Calcular valores finais
            var finalPrice = quote.FinalPrice - discountAmount;
            if (finalPrice < 0) finalPrice = 0;

            var changeAmount = 0m;
            if (paymentMethod.ToUpper() == "DINHEIRO" && amountPaid > finalPrice)
            {
                changeAmount = amountPaid - finalPrice;
            }

            // 4. Criar Ordem de Manipulação (apenas adiciona ao context, não salva)
            var order = await CreateManipulationOrderAsync(quote, employeeId, establishmentId);

            // 5. Criar Venda
            var sale = await CreateSaleAsync(quote, order, employeeId, establishmentId,
                finalPrice, discountAmount, discountReason, paymentMethod,
                amountPaid, changeAmount, observations);

            // 6. Criar Item da Venda
            await CreateSaleItemAsync(sale, order, quote);

            // 7. Criar Pagamento
            await CreateSalePaymentAsync(sale, paymentMethod, amountPaid, changeAmount,
                installments, cardBrand, cardLastDigits, nsu, authorizationCode, pixTransactionId, employeeId);

            // 8. Registrar no Caixa
            var cashMovement = new CashMovement
            {
                Id = Guid.NewGuid(),
                CashRegisterId = openCashRegister.Id,
                SaleId = sale.Id,
                MovementType = "ENTRADA",
                PaymentMethod = paymentMethod.ToUpper(),
                Amount = paymentMethod.ToUpper() == "DINHEIRO" ? (amountPaid - changeAmount) : amountPaid,
                Description = $"Venda {sale.Code} - Orçamento {quote.Code}",
                EmployeeId = employeeId,
                MovementDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.CashMovements.Add(cashMovement);

            // Atualizar totais do caixa
            UpdateCashRegisterTotals(openCashRegister, paymentMethod, amountPaid, changeAmount, finalPrice);

            // 9. Atualizar status do orçamento
            quote.Status = "CONVERTIDO";
            quote.SaleId = sale.Id;
            quote.ManipulationOrderId = order.Id;
            quote.UpdatedAt = DateTime.UtcNow;

            // 10. Atualizar status da prescrição (se existir)
            if (quote.PrescriptionId.HasValue)
            {
                var prescription = await _context.Prescriptions.FindAsync(quote.PrescriptionId.Value);
                if (prescription != null)
                {
                    prescription.Status = "EM_MANIPULACAO";
                    prescription.UpdatedAt = DateTime.UtcNow;
                }
            }

            // 11. Salvar TUDO de uma vez (atômico)
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Orçamento {QuoteId} convertido em Venda {SaleId} e OM {OrderId}",
                quoteId, sale.Id, order.Id);

            return (true, "Venda realizada com sucesso!", sale.Id, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao converter orçamento {QuoteId} em venda", quoteId);
            return (false, $"Erro ao processar venda: {ex.Message}", null, null);
        }
    }

    /// <summary>
    /// Cria a ordem de manipulação a partir do orçamento
    /// </summary>
    private async Task<ManipulationOrder> CreateManipulationOrderAsync(
        PrescriptionQuote quote,
        Guid employeeId,
        Guid establishmentId)
    {
        var orderCode = await GenerateManipulationOrderCodeAsync(establishmentId);

        // Extrair quantidade
        decimal quantity = 1;
        if (!string.IsNullOrEmpty(quote.TotalQuantity))
        {
            var numericPart = new string(quote.TotalQuantity.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray());
            if (!string.IsNullOrEmpty(numericPart))
            {
                decimal.TryParse(numericPart.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out quantity);
            }
            if (quantity <= 0) quantity = 1;
        }

        // Buscar código da prescrição se existir
        string? prescriptionCode = null;
        if (quote.PrescriptionId.HasValue)
        {
            var prescription = await _context.Prescriptions.FindAsync(quote.PrescriptionId.Value);
            prescriptionCode = prescription?.Code;
        }

        var order = new ManipulationOrder
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            OrderNumber = orderCode,
            Code = orderCode,
            PrescriptionNumber = prescriptionCode,
            PrescriberName = quote.DoctorName,
            PrescriberRegistration = quote.DoctorCrm,
            CustomerName = quote.CustomerName ?? "Cliente",
            CustomerPhone = quote.CustomerPhone,
            QuantityToProduce = quantity,
            Unit = quote.TotalQuantityUnit ?? "un",
            SpecialInstructions = quote.Instructions,
            Status = "AGUARDANDO_PRODUCAO",
            OrderDate = DateTime.UtcNow,
            ExpectedDate = DateTime.UtcNow.AddDays(quote.EstimatedDays),
            RequestedByEmployeeId = employeeId,
            PassedQualityControl = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ManipulationOrders.Add(order);
        // NÃO fazer SaveChangesAsync aqui - será feito no método principal

        // Criar componentes da ordem a partir dos componentes do orçamento
        if (!string.IsNullOrEmpty(quote.ComponentsJson))
        {
            try
            {
                var components = System.Text.Json.JsonSerializer.Deserialize<List<QuoteComponentData>>(
                    quote.ComponentsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (components != null)
                {
                    int index = 0;
                    foreach (var comp in components)
                    {
                        var orderComponent = new ManipulationOrderComponent
                        {
                            Id = Guid.NewGuid(),
                            ManipulationOrderId = order.Id,
                            RawMaterialId = comp.RawMaterialId,
                            RequiredQuantity = comp.Quantity,
                            Unit = comp.Unit ?? "g",
                            UnitCost = comp.UnitCost,
                            TotalCost = comp.TotalCost,
                            OrderIndex = index++,
                            Status = "PENDENTE",
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        _context.ManipulationOrderComponents.Add(orderComponent);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao parsear componentes do orçamento {QuoteId}", quote.Id);
            }
        }

        return order;
    }

    /// <summary>
    /// Cria a venda vinculada ao orçamento e ordem de manipulação
    /// </summary>
    private async Task<Sale> CreateSaleAsync(
        PrescriptionQuote quote,
        ManipulationOrder order,
        Guid employeeId,
        Guid establishmentId,
        decimal finalPrice,
        decimal discountAmount,
        string? discountReason,
        string paymentMethod,
        decimal amountPaid,
        decimal changeAmount,
        string? observations)
    {
        var saleCode = await GenerateSaleCodeAsync(establishmentId);

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = saleCode,
            CustomerId = quote.CustomerId,
            SaleDate = DateTime.UtcNow,
            Subtotal = quote.FinalPrice,
            DiscountAmount = discountAmount,
            TotalAmount = finalPrice,
            PaymentMethod = paymentMethod.ToUpper(),
            PaymentStatus = amountPaid >= finalPrice ? "PAGO" : "PAGAMENTO_PARCIAL",
            PaidAmount = amountPaid,
            ChangeAmount = changeAmount,
            Status = "FINALIZADA",
            Observations = string.IsNullOrEmpty(observations)
                ? $"Venda gerada do orçamento {quote.Code} - OM: {order.OrderNumber}"
                : $"{observations} | Orçamento: {quote.Code}",
            CreatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Sales.Add(sale);
        return sale;
    }

    /// <summary>
    /// Cria o item da venda
    /// </summary>
    private async Task CreateSaleItemAsync(Sale sale, ManipulationOrder order, PrescriptionQuote quote)
    {
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ManipulationOrderId = order.Id,
            Description = $"{quote.PharmaceuticalForm ?? "Manipulado"} {quote.TotalQuantity} - {quote.CustomerName}",
            Quantity = 1,
            UnitPrice = quote.FinalPrice,
            TotalPrice = quote.FinalPrice,
            CostPrice = quote.MaterialsCost + quote.LaborCost + quote.PackagingCost,
            ProfitMargin = quote.FinalPrice > 0
                ? ((quote.FinalPrice - (quote.MaterialsCost + quote.LaborCost + quote.PackagingCost)) / quote.FinalPrice) * 100
                : 0
        };

        _context.SaleItems.Add(saleItem);
    }

    /// <summary>
    /// Cria o registro de pagamento
    /// </summary>
    private async Task CreateSalePaymentAsync(
        Sale sale,
        string paymentMethod,
        decimal amountPaid,
        decimal changeAmount,
        int installments,
        string? cardBrand,
        string? cardLastDigits,
        string? nsu,
        string? authorizationCode,
        string? pixTransactionId,
        Guid employeeId)
    {
        var payment = new SalePayment
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            PaymentMethod = paymentMethod.ToUpper(),
            Amount = amountPaid,
            PaymentStatus = "APPROVED",
            PaymentDate = DateTime.UtcNow,
            ProcessedByEmployeeId = employeeId,
            CreatedAt = DateTime.UtcNow
        };

        // Campos específicos por método de pagamento
        switch (paymentMethod.ToUpper())
        {
            case "DINHEIRO":
                payment.CashReceived = amountPaid;
                payment.ChangeAmount = changeAmount;
                break;

            case "CARTAO_CREDITO":
            case "CARTAO_DEBITO":
                payment.CardBrand = cardBrand;
                payment.CardLastDigits = cardLastDigits;
                payment.Nsu = nsu;
                payment.AuthorizationCode = authorizationCode;
                payment.Installments = installments;
                break;

            case "PIX":
                payment.PixTransactionId = pixTransactionId;
                break;
        }

        _context.SalePayments.Add(payment);
    }

    /// <summary>
    /// Atualiza os totais do caixa
    /// </summary>
    private void UpdateCashRegisterTotals(
        CashRegister cashRegister,
        string paymentMethod,
        decimal amountPaid,
        decimal changeAmount,
        decimal saleTotal)
    {
        switch (paymentMethod.ToUpper())
        {
            case "DINHEIRO":
                cashRegister.TotalCash += amountPaid - changeAmount;
                break;
            case "CARTAO_CREDITO":
                cashRegister.TotalCredit += amountPaid;
                cashRegister.TotalCard += amountPaid;
                break;
            case "CARTAO_DEBITO":
                cashRegister.TotalDebit += amountPaid;
                cashRegister.TotalCard += amountPaid;
                break;
            case "PIX":
                cashRegister.TotalPix += amountPaid;
                break;
            case "BOLETO":
                cashRegister.TotalBoleto += amountPaid;
                break;
            default:
                cashRegister.TotalOther += amountPaid;
                break;
        }

        cashRegister.TotalSales += saleTotal;
        cashRegister.SalesCount++;
        cashRegister.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gera código único para ordem de manipulação
    /// </summary>
    private async Task<string> GenerateManipulationOrderCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var dateStr = today.ToString("yyyyMMdd");

        var count = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.CreatedAt.Date == today.Date)
            .CountAsync();

        return $"OM{dateStr}{(count + 1):D4}";
    }

    /// <summary>
    /// Gera código único para venda
    /// </summary>
    private async Task<string> GenerateSaleCodeAsync(Guid establishmentId)
    {
        var today = DateTime.UtcNow;
        var monthStr = today.ToString("yyyyMM");

        var count = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId &&
                       s.CreatedAt.Year == today.Year &&
                       s.CreatedAt.Month == today.Month)
            .CountAsync();

        return $"VD{monthStr}{(count + 1):D4}";
    }
}

/// <summary>
/// Classe auxiliar para deserializar componentes do JSON
/// </summary>
internal class QuoteComponentData
{
    public Guid RawMaterialId { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}