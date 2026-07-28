using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Data;
using DTOs;
using DTOs.Sales;
using Models;
using Models.Pharmacy;
using Service;

namespace Controllers;

[ApiController]
[Route("api/[controller]")]
public class PDVController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SaleService _saleService;
    private readonly CashRegisterService _cashService;
    private readonly StockService _stockService;

    public PDVController(AppDbContext context)
    {
        _context = context;
        _saleService = new SaleService(context);
        _cashService = new CashRegisterService(context);
        _stockService = new StockService(context);
    }

    private Guid GetEstablishmentId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee != null) return employee.EstablishmentId;

        var claim = User.FindFirst("EstablishmentId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    private Guid GetEmployeeId()
    {
        var employee = HttpContext.Items["Employee"] as Models.Employees.Employee;
        if (employee != null) return employee.Id;

        var claim = User.FindFirst("EmployeeId");
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }

    // ================================================================
    // STATUS DO CAIXA
    // ================================================================

    [HttpGet("cash-register/status")]
    public async Task<ActionResult<ApiResponse<CashRegisterStatusDto>>> GetCashRegisterStatus()
    {
        var establishmentId = GetEstablishmentId();

        var cashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);

        if (cashRegister == null)
        {
            return Ok(ApiResponse<CashRegisterStatusDto>.SuccessResponse(
                new CashRegisterStatusDto { IsOpen = false },
                "Nenhum caixa aberto"));
        }

        var operatorName = await _context.Employees
            .Where(e => e.Id == cashRegister.OpenedByEmployeeId)
            .Select(e => e.FullName)
            .FirstOrDefaultAsync();

        var status = new CashRegisterStatusDto
        {
            IsOpen = true,
            Id = cashRegister.Id,
            Code = cashRegister.Code,
            OpeningDate = cashRegister.OpeningDate,
            OpeningBalance = cashRegister.OpeningBalance,
            TotalCash = cashRegister.TotalCash,
            TotalCard = cashRegister.TotalCard,
            TotalPix = cashRegister.TotalPix,
            TotalSales = cashRegister.TotalSales,
            SalesCount = cashRegister.SalesCount,
            OperatorName = operatorName ?? "Operador"
        };

        return Ok(ApiResponse<CashRegisterStatusDto>.SuccessResponse(status, "Caixa aberto"));
    }

    // ================================================================
    // QUICK SALE (VENDA RÁPIDA LEGADO)
    // ================================================================

    [HttpPost("quick-sale")]
    public async Task<ActionResult<ApiResponse<SaleReceiptDto>>> QuickSale([FromBody] QuickSaleDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister == null)
            return BadRequest(ApiResponse<SaleReceiptDto>.ErrorResponse("Nenhum caixa aberto. Abra o caixa antes de realizar vendas."));

        var saleDto = new CreateSaleDto
        {
            CustomerId = dto.CustomerId,
            Items = dto.Items.Select(i => new SaleItemDto
            {
                ManipulationOrderId = i.ManipulationOrderId,
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountPercentage = i.DiscountPercentage
            }).ToList(),
            SaleDate = DateTime.UtcNow,
            PaymentMethod = dto.PaymentMethod,
            PaidAmount = dto.PaidAmount,
            DiscountPercentage = dto.DiscountPercentage,
            Observations = dto.Observations
        };

        var result = await _saleService.CreateSaleAsync(saleDto, establishmentId, employeeId);
        var success = result.Item1;
        var message = result.Item2;
        var sale = result.Item3;

        if (!success || sale == null)
            return BadRequest(ApiResponse<SaleReceiptDto>.ErrorResponse(message));

        var stockResult = await _stockService.DeductStockForSaleAsync(sale.Id, establishmentId, employeeId);

        if (!stockResult.Success)
            Console.WriteLine($"Aviso de estoque: {stockResult.Message}");

        await _cashService.RegisterSaleInCashRegisterAsync(
            openCashRegister.Id, sale.Id, sale.TotalAmount, sale.PaymentMethod, employeeId);

        var receipt = await GenerateReceipt(sale.Id);

        var responseMessage = "Venda realizada com sucesso!";
        if (!stockResult.Success)
            responseMessage += $" ATENÇÃO: {stockResult.Message}";

        return Ok(ApiResponse<SaleReceiptDto>.SuccessResponse(receipt, responseMessage));
    }

    // ================================================================
    // ESTOQUE
    // ================================================================

    [HttpGet("low-stock-alerts")]
    public async Task<ActionResult<ApiResponse<List<LowStockAlert>>>> GetLowStockAlerts()
    {
        var establishmentId = GetEstablishmentId();
        var lowStockItems = await _stockService.GetLowStockItemsAsync(establishmentId);

        return Ok(ApiResponse<List<LowStockAlert>>.SuccessResponse(
            lowStockItems, $"{lowStockItems.Count} item(ns) com estoque baixo"));
    }

    [HttpGet("check-stock/{rawMaterialId}")]
    public async Task<ActionResult<ApiResponse<object>>> CheckStock(Guid rawMaterialId, [FromQuery] Guid? batchId = null)
    {
        var establishmentId = GetEstablishmentId();
        var currentStock = await _stockService.GetCurrentStockAsync(rawMaterialId, batchId, establishmentId);

        var rawMaterial = await _context.RawMaterials.FirstOrDefaultAsync(r => r.Id == rawMaterialId);
        if (rawMaterial == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Matéria-prima não encontrada"));

        var stockInfo = new
        {
            RawMaterialId = rawMaterialId,
            Name = rawMaterial.Name,
            CurrentStock = currentStock,
            Unit = rawMaterial.Unit,
            MinimumStock = rawMaterial.MinimumStock,
            IsLowStock = currentStock <= rawMaterial.MinimumStock,
            IsOutOfStock = currentStock <= 0,
            Status = currentStock <= 0 ? "SEM_ESTOQUE" :
                     currentStock <= rawMaterial.MinimumStock ? "ESTOQUE_BAIXO" : "OK"
        };

        return Ok(ApiResponse<object>.SuccessResponse(stockInfo));
    }

    // ================================================================
    // RECIBO E VENDAS DIÁRIAS
    // ================================================================

    [HttpGet("receipt/{saleId}")]
    public async Task<ActionResult<ApiResponse<SaleReceiptDto>>> GetReceipt(Guid saleId)
    {
        var receipt = await GenerateReceipt(saleId);
        if (receipt == null)
            return NotFound(ApiResponse<SaleReceiptDto>.ErrorResponse("Venda não encontrada"));

        return Ok(ApiResponse<SaleReceiptDto>.SuccessResponse(receipt));
    }

    [HttpGet("daily-sales")]
    public async Task<ActionResult<ApiResponse<DailySalesDto>>> GetDailySales([FromQuery] DateTime? date)
    {
        var establishmentId = GetEstablishmentId();
        var targetDate = date ?? DateTime.Today;

        var sales = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId &&
                       s.SaleDate.Date == targetDate.Date &&
                       s.Status == "FINALIZADA")
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        var result = new DailySalesDto
        {
            Date = targetDate,
            TotalSales = sales.Count,
            TotalAmount = sales.Sum(s => s.TotalAmount),
            TotalCash = sales.Where(s => s.PaymentMethod == "DINHEIRO").Sum(s => s.TotalAmount),
            TotalCard = sales.Where(s => s.PaymentMethod != null && s.PaymentMethod.Contains("CARTAO")).Sum(s => s.TotalAmount),
            TotalPix = sales.Where(s => s.PaymentMethod == "PIX").Sum(s => s.TotalAmount),
            SalesByPaymentMethod = sales.Where(s => s.PaymentMethod != null).GroupBy(s => s.PaymentMethod!).ToDictionary(g => g.Key, g => g.Count()),
            Sales = sales.Select(s => new DTOs.Sales.SaleListDto
            {
                Id = s.Id,
                Code = s.Code,
                CustomerName = s.CustomerId.HasValue ?
                    _context.Customers.Where(c => c.Id == s.CustomerId).Select(c => c.FullName).FirstOrDefault() :
                    "CLIENTE NÃO IDENTIFICADO",
                SaleDate = s.SaleDate,
                TotalAmount = s.TotalAmount,
                PaymentMethod = s.PaymentMethod ?? "N/A",
                Status = s.Status
            }).ToList()
        };

        return Ok(ApiResponse<DailySalesDto>.SuccessResponse(result));
    }

    // ================================================================
    // MANIPULAÇÕES FINALIZADAS
    // ================================================================

    [HttpGet("pending-orders")]
    public async Task<ActionResult<ApiResponse<List<PendingManipulationDto>>>> GetPendingOrders()
    {
        var establishmentId = GetEstablishmentId();

        var orders = await _context.ManipulationOrders
            .Where(o => o.EstablishmentId == establishmentId &&
                       o.Status == "FINALIZADO" &&
                       !_context.SaleItems.Any(si => si.ManipulationOrderId == o.Id))
            .Include(o => o.Formula)
            .Select(o => new PendingManipulationDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.CustomerName,
                FormulaName = o.Formula != null ? o.Formula.Name : "N/A",
                QuantityToProduce = o.QuantityToProduce,
                Unit = o.Unit,
                CompletionDate = o.CompletionDate,
                SuggestedPrice = _context.PrescriptionQuotes
                    .Where(q => q.ManipulationOrderId == o.Id && (q.Status == "APROVADO" || q.Status == "CONVERTIDO"))
                    .Select(q => q.FinalPrice)
                    .FirstOrDefault()
            })
            .OrderBy(o => o.CompletionDate)
            .ToListAsync();

        return Ok(ApiResponse<List<PendingManipulationDto>>.SuccessResponse(orders,
            $"{orders.Count} manipulação(ões) aguardando pagamento"));
    }

    /// <summary>
    /// Processa pagamento de uma manipulação finalizada
    /// </summary>
    [HttpPost("process-manipulation/{id}")]
    public async Task<ActionResult<ApiResponse<ProcessOnlineOrderResultDto>>> ProcessManipulation(
        Guid id,
        [FromBody] ProcessOnlineOrderDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister == null)
            return BadRequest(ApiResponse<ProcessOnlineOrderResultDto>.ErrorResponse("Nenhum caixa aberto"));

        var manipulation = await _context.ManipulationOrders
            .Include(m => m.Formula)
            .FirstOrDefaultAsync(m => m.Id == id && m.EstablishmentId == establishmentId);

        if (manipulation == null)
            return NotFound(ApiResponse<ProcessOnlineOrderResultDto>.ErrorResponse("Manipulação não encontrada"));

        if (manipulation.Status != "FINALIZADO")
            return BadRequest(ApiResponse<ProcessOnlineOrderResultDto>.ErrorResponse("Manipulação não está finalizada"));

        // Verificar se já existe venda para esta manipulação
        var existingSale = await _context.SaleItems
            .Include(si => si.Sale)
            .Where(si => si.ManipulationOrderId == id)
            .Select(si => si.Sale)
            .FirstOrDefaultAsync();

        if (existingSale != null)
        {
            return Ok(ApiResponse<ProcessOnlineOrderResultDto>.SuccessResponse(
                new ProcessOnlineOrderResultDto
                {
                    SaleId = existingSale.Id,
                    SaleCode = existingSale.Code,
                    OrderNumber = manipulation.OrderNumber,
                    Total = existingSale.TotalAmount,
                    PaidAmount = existingSale.PaidAmount ?? 0
                }, "Venda já existe para esta manipulação"));
        }

        // Buscar preço do orçamento aprovado
        var quote = await _context.PrescriptionQuotes
            .Where(q => q.ManipulationOrderId == id && (q.Status == "APROVADO" || q.Status == "CONVERTIDO"))
            .FirstOrDefaultAsync();

        var totalPrice = quote?.FinalPrice ?? dto.PaidAmount;

        // Gerar código da venda
        var today = DateTime.UtcNow;
        var countToday = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate.Date == today.Date)
            .CountAsync();
        var saleCode = $"V{today:yyyyMMdd}-{(countToday + 1):D4}";

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            Code = saleCode,
            SaleDate = DateTime.UtcNow,
            Subtotal = totalPrice,
            DiscountAmount = 0,
            TotalAmount = totalPrice,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = "PAGO",
            PaidAmount = dto.PaidAmount,
            ChangeAmount = dto.ChangeAmount,
            PaymentDate = DateTime.UtcNow,
            Status = "FINALIZADA",
            Observations = $"Manipulação: {manipulation.OrderNumber}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId
        };

        _context.Sales.Add(sale);

        var formulaName = manipulation.Formula?.Name ?? "Fórmula Magistral";
        var saleItem = new SaleItem
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            ManipulationOrderId = manipulation.Id,
            Description = $"{formulaName} - {manipulation.QuantityToProduce} {manipulation.Unit}",
            Quantity = 1,
            UnitPrice = totalPrice,
            TotalPrice = totalPrice,
            DiscountAmount = 0
        };

        _context.SaleItems.Add(saleItem);

        // Atualizar status do orçamento
        if (quote != null)
        {
            quote.Status = "CONVERTIDO";
            quote.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _cashService.RegisterSaleInCashRegisterAsync(
            openCashRegister.Id, sale.Id, sale.TotalAmount, sale.PaymentMethod ?? dto.PaymentMethod, employeeId);

        return Ok(ApiResponse<ProcessOnlineOrderResultDto>.SuccessResponse(
            new ProcessOnlineOrderResultDto
            {
                SaleId = sale.Id,
                SaleCode = sale.Code,
                OrderNumber = manipulation.OrderNumber,
                Total = sale.TotalAmount,
                PaidAmount = dto.PaidAmount,
                ChangeAmount = dto.ChangeAmount
            }, "Pagamento processado com sucesso!"));
    }

    // ================================================================
    // PEDIDOS ONLINE
    // ================================================================

    /// <summary>
    /// Retorna pedidos online prontos para pagamento (alias para compatibilidade com View)
    /// </summary>
    [HttpGet("online-orders-ready")]
    public async Task<ActionResult<ApiResponse<List<OnlineOrderPdvDto>>>> GetOnlineOrdersReady()
    {
        return await GetOnlineOrders("READY");
    }

    [HttpGet("online-orders")]
    public async Task<ActionResult<ApiResponse<List<OnlineOrderPdvDto>>>> GetOnlineOrders([FromQuery] string? status = null)
    {
        var establishmentId = GetEstablishmentId();

        var query = _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.EstablishmentId == establishmentId);

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(o => o.Status == status);
        }
        else
        {
            // Por padrão, mostrar apenas pedidos prontos para pagamento
            query = query.Where(o => o.Status == "READY" || o.Status == "CONFIRMED");
        }

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .Select(o => new OnlineOrderPdvDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer != null ? o.Customer.FullName : "Cliente",
                CustomerPhone = o.Customer != null ? o.Customer.Phone : null,
                CustomerId = o.CustomerId,
                Total = o.Total,
                Subtotal = o.Subtotal,
                Discount = o.Discount,
                Status = o.Status,
                StatusDisplay = o.Status == "READY" ? "Pronto" :
                               o.Status == "CONFIRMED" ? "Confirmado" :
                               o.Status == "PREPARING" ? "Preparando" :
                               o.Status == "DELIVERED" ? "Entregue" : o.Status,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt,
                ItemsCount = o.Items!.Count,
                Items = o.Items!.Select(i => new OnlineOrderItemPdvDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .ToListAsync();

        return Ok(ApiResponse<List<OnlineOrderPdvDto>>.SuccessResponse(orders, $"{orders.Count} pedido(s) encontrado(s)"));
    }

    [HttpPost("process-online-order/{orderId}")]
    public async Task<ActionResult<ApiResponse<ProcessOnlineOrderResultDto>>> ProcessOnlineOrder(
        Guid orderId,
        [FromBody] ProcessOnlineOrderDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister == null)
            return BadRequest(ApiResponse<ProcessOnlineOrderResultDto>.ErrorResponse("Nenhum caixa aberto"));

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
            return NotFound(ApiResponse<ProcessOnlineOrderResultDto>.ErrorResponse("Pedido não encontrado"));

        if (order.PaymentStatus == "PAID")
            return BadRequest(ApiResponse<ProcessOnlineOrderResultDto>.ErrorResponse("Pedido já foi pago"));

        // Gerar código da venda
        var today = DateTime.UtcNow;
        var countToday = await _context.Sales
            .Where(s => s.EstablishmentId == establishmentId && s.SaleDate.Date == today.Date)
            .CountAsync();
        var saleCode = $"V{today:yyyyMMdd}-{(countToday + 1):D4}";

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            EstablishmentId = establishmentId,
            CustomerId = order.CustomerId,
            Code = saleCode,
            SaleDate = DateTime.UtcNow,
            Subtotal = order.Subtotal,
            DiscountAmount = order.Discount,
            TotalAmount = order.Total,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = "PAGO",
            PaidAmount = dto.PaidAmount,
            ChangeAmount = dto.ChangeAmount,
            PaymentDate = DateTime.UtcNow,
            Status = "FINALIZADA",
            Observations = $"Pedido Online: {order.OrderNumber}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByEmployeeId = employeeId
        };

        _context.Sales.Add(sale);

        // Criar itens da venda
        foreach (var item in order.Items!)
        {
            var saleItem = new SaleItem
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                Description = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                DiscountAmount = 0
            };
            _context.SaleItems.Add(saleItem);
        }

        // Criar registro de pagamento
        var payment = new SalePayment
        {
            Id = Guid.NewGuid(),
            SaleId = sale.Id,
            PaymentMethod = dto.PaymentMethod,
            Amount = dto.PaidAmount,
            CashReceived = dto.CashReceived,
            ChangeAmount = dto.ChangeAmount,
            CardBrand = dto.CardBrand,
            CardLastDigits = dto.CardLastDigits,
            Installments = dto.Installments ?? 1,
            Nsu = dto.Nsu,
            AuthorizationCode = dto.AuthorizationCode,
            PixTransactionId = dto.PixTransactionId,
            PaymentStatus = "APPROVED",
            PaymentDate = DateTime.UtcNow,
            ProcessedByEmployeeId = employeeId,
            CreatedAt = DateTime.UtcNow
        };
        _context.SalePayments.Add(payment);

        order.SaleId = sale.Id;
        order.PaymentStatus = "PAID";
        order.PaymentMethod = dto.PaymentMethod;
        order.Status = "DELIVERED";
        order.DeliveredAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _cashService.RegisterSaleInCashRegisterAsync(
            openCashRegister.Id, sale.Id, sale.TotalAmount, sale.PaymentMethod ?? dto.PaymentMethod, employeeId);

        return Ok(ApiResponse<ProcessOnlineOrderResultDto>.SuccessResponse(
            new ProcessOnlineOrderResultDto
            {
                SaleId = sale.Id,
                SaleCode = sale.Code,
                OrderNumber = order.OrderNumber,
                Total = sale.TotalAmount,
                PaidAmount = dto.PaidAmount,
                ChangeAmount = dto.ChangeAmount
            }, "Pagamento processado com sucesso!"));
    }

    [HttpGet("online-order/{orderId}")]
    public async Task<ActionResult<ApiResponse<OnlineOrderPdvDto>>> GetOnlineOrder(Guid orderId)
    {
        var establishmentId = GetEstablishmentId();

        var order = await _context.Set<OnlineOrder>()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Where(o => o.Id == orderId && o.EstablishmentId == establishmentId)
            .Select(o => new OnlineOrderPdvDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer != null ? o.Customer.FullName : "Cliente",
                CustomerPhone = o.Customer != null ? o.Customer.Phone : null,
                CustomerId = o.CustomerId,
                Total = o.Total,
                Subtotal = o.Subtotal,
                Discount = o.Discount,
                Status = o.Status,
                StatusDisplay = o.Status == "READY" ? "Pronto" :
                               o.Status == "CONFIRMED" ? "Confirmado" :
                               o.Status == "DELIVERED" ? "Entregue" : o.Status,
                PaymentStatus = o.PaymentStatus,
                CreatedAt = o.CreatedAt,
                ItemsCount = o.Items!.Count,
                Items = o.Items!.Select(i => new OnlineOrderItemPdvDto
                {
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (order == null)
            return NotFound(ApiResponse<OnlineOrderPdvDto>.ErrorResponse("Pedido não encontrado"));

        return Ok(ApiResponse<OnlineOrderPdvDto>.SuccessResponse(order));
    }

    // ================================================================
    // PRODUTOS DO CATÁLOGO + MATÉRIAS-PRIMAS (VENDA BALCÃO) - ATUALIZADO!
    // ================================================================

    /// <summary>
    /// Busca produtos no catálogo E matérias-primas para venda no balcão
    /// Retorna resultados combinados de CatalogProducts e RawMaterials
    /// </summary>
    [HttpGet("products/search")]
    public async Task<ActionResult<ApiResponse<List<ProductSearchDto>>>> SearchProducts([FromQuery] string q)
    {
        var establishmentId = GetEstablishmentId();

        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return BadRequest(ApiResponse<List<ProductSearchDto>>.ErrorResponse("Digite pelo menos 2 caracteres"));

        var searchTerm = q.ToLower();
        var results = new List<ProductSearchDto>();

        // 1. Buscar em CatalogProducts (produtos de prateleira)
        var catalogProducts = await _context.CatalogProducts
            .Include(p => p.Category)
            .Where(p => p.EstablishmentId == establishmentId && p.IsActive)
            .Where(p => p.Name.ToLower().Contains(searchTerm) ||
                        (p.Code != null && p.Code.ToLower().Contains(searchTerm)))
            .OrderBy(p => p.Name)
            .Take(10)
            .Select(p => new ProductSearchDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Price = p.CurrentPrice,
                OriginalPrice = p.Price,
                IsOnPromotion = p.IsOnPromotion,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit,
                CategoryName = p.Category != null ? p.Category.Name : null,
                Source = "CATALOGO",
                ControlType = null,
                IsControlled = false
            })
            .ToListAsync();

        results.AddRange(catalogProducts);

        // 2. Buscar em RawMaterials (matérias-primas vendáveis)
        var rawMaterialsQuery = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .Where(r => r.Name.ToLower().Contains(searchTerm) ||
                        (r.DcbCode != null && r.DcbCode.ToLower().Contains(searchTerm)) ||
                        (r.CasNumber != null && r.CasNumber.ToLower().Contains(searchTerm)))
            .OrderBy(r => r.Name)
            .Take(10)
            .ToListAsync();

        foreach (var r in rawMaterialsQuery)
        {
            // Calcular preço médio dos lotes aprovados
            var batchCosts = await _context.Batches
                .Where(b => b.RawMaterialId == r.Id && b.Status.ToUpper() == "APROVADO")
                .Select(b => b.UnitCost)
                .ToListAsync();

            var avgCost = batchCosts.Any() ? batchCosts.Average() : 0m;

            // Markup de 100% para venda no balcão (ajustar conforme necessário)
            var salePrice = avgCost > 0 ? avgCost * 2m : 0;

            results.Add(new ProductSearchDto
            {
                Id = r.Id,
                Name = r.Name,
                Code = r.DcbCode ?? r.CasNumber,
                Price = salePrice,
                OriginalPrice = salePrice,
                IsOnPromotion = false,
                StockQuantity = (int)r.CurrentStock,
                Unit = r.Unit,
                CategoryName = r.ControlType != "COMUM" ? $"Controlado - {r.ControlType}" : "Matéria-Prima",
                Source = "MATERIA_PRIMA",
                ControlType = r.ControlType,
                IsControlled = r.ControlType != null && r.ControlType != "COMUM"
            });
        }

        // Ordenar por nome e limitar a 20 resultados
        results = results.OrderBy(r => r.Name).Take(20).ToList();

        return Ok(ApiResponse<List<ProductSearchDto>>.SuccessResponse(results, $"{results.Count} item(ns) encontrado(s)"));
    }

    [HttpGet("products/code/{code}")]
    public async Task<ActionResult<ApiResponse<ProductSearchDto>>> GetProductByCode(string code)
    {
        var establishmentId = GetEstablishmentId();

        // Primeiro buscar em CatalogProducts
        var product = await _context.CatalogProducts
            .Include(p => p.Category)
            .Where(p => p.EstablishmentId == establishmentId && p.IsActive && p.Code == code)
            .Select(p => new ProductSearchDto
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                Price = p.CurrentPrice,
                OriginalPrice = p.Price,
                IsOnPromotion = p.IsOnPromotion,
                StockQuantity = p.StockQuantity,
                Unit = p.Unit,
                CategoryName = p.Category != null ? p.Category.Name : null,
                Source = "CATALOGO",
                ControlType = null,
                IsControlled = false
            })
            .FirstOrDefaultAsync();

        if (product != null)
            return Ok(ApiResponse<ProductSearchDto>.SuccessResponse(product));

        // Se não encontrar, buscar em RawMaterials
        var rawMaterial = await _context.RawMaterials
            .Where(r => r.EstablishmentId == establishmentId && r.IsActive)
            .Where(r => r.DcbCode == code || r.CasNumber == code)
            .FirstOrDefaultAsync();

        if (rawMaterial == null)
            return NotFound(ApiResponse<ProductSearchDto>.ErrorResponse("Produto não encontrado"));

        // Calcular preço médio dos lotes aprovados
        var batchCosts = await _context.Batches
            .Where(b => b.RawMaterialId == rawMaterial.Id && b.Status.ToUpper() == "APROVADO")
            .Select(b => b.UnitCost)
            .ToListAsync();

        var avgCost = batchCosts.Any() ? batchCosts.Average() : 0m;
        var salePrice = avgCost > 0 ? avgCost * 2m : 0;

        var result = new ProductSearchDto
        {
            Id = rawMaterial.Id,
            Name = rawMaterial.Name,
            Code = rawMaterial.DcbCode ?? rawMaterial.CasNumber,
            Price = salePrice,
            OriginalPrice = salePrice,
            IsOnPromotion = false,
            StockQuantity = (int)rawMaterial.CurrentStock,
            Unit = rawMaterial.Unit,
            CategoryName = rawMaterial.ControlType != "COMUM" ? $"Controlado - {rawMaterial.ControlType}" : "Matéria-Prima",
            Source = "MATERIA_PRIMA",
            ControlType = rawMaterial.ControlType,
            IsControlled = rawMaterial.ControlType != null && rawMaterial.ControlType != "COMUM"
        };

        return Ok(ApiResponse<ProductSearchDto>.SuccessResponse(result));
    }

    // ================================================================
    // VENDA UNIFICADA (PRODUTOS + MANIPULAÇÕES + ONLINE + MATÉRIAS-PRIMAS)
    // ================================================================

    [HttpPost("unified-sale")]
    public async Task<ActionResult<ApiResponse<UnifiedSaleResultDto>>> CreateUnifiedSale([FromBody] CreateUnifiedSaleDto dto)
    {
        var establishmentId = GetEstablishmentId();
        var employeeId = GetEmployeeId();

        var openCashRegister = await _cashService.GetOpenCashRegisterAsync(establishmentId);
        if (openCashRegister == null)
            return BadRequest(ApiResponse<UnifiedSaleResultDto>.ErrorResponse("Nenhum caixa aberto"));

        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(ApiResponse<UnifiedSaleResultDto>.ErrorResponse("Nenhum item na venda"));

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validar itens controlados
            foreach (var item in dto.Items.Where(i => i.Tipo == "MATERIA_PRIMA"))
            {
                if (item.ReferenciaId.HasValue)
                {
                    var rawMaterial = await _context.RawMaterials.FindAsync(item.ReferenciaId.Value);
                    if (rawMaterial != null && rawMaterial.ControlType != null && rawMaterial.ControlType != "COMUM")
                    {
                        // É um controlado - validar se tem receita (implementação futura)
                        // Por enquanto, apenas alertar
                        Console.WriteLine($"ATENÇÃO: Venda de controlado {rawMaterial.Name} ({rawMaterial.ControlType})");
                    }
                }
            }

            // Gerar código da venda
            var today = DateTime.UtcNow;
            var countToday = await _context.Sales
                .Where(s => s.EstablishmentId == establishmentId && s.SaleDate.Date == today.Date)
                .CountAsync();
            var saleCode = $"V{today:yyyyMMdd}-{(countToday + 1):D4}";

            // Calcular totais
            var subtotal = dto.Items.Sum(i => i.Total);
            var discount = dto.Discount ?? 0;
            var total = subtotal - discount;

            var sale = new Sale
            {
                Id = Guid.NewGuid(),
                EstablishmentId = establishmentId,
                CustomerId = dto.CustomerId,
                Code = saleCode,
                SaleDate = DateTime.UtcNow,
                Subtotal = subtotal,
                DiscountAmount = discount,
                TotalAmount = total,
                PaymentMethod = dto.Payment.Method,
                PaymentStatus = "PAGO",
                PaidAmount = dto.Payment.Amount,
                ChangeAmount = dto.Payment.ChangeAmount,
                PaymentDate = DateTime.UtcNow,
                Status = "FINALIZADA",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedByEmployeeId = employeeId
            };

            _context.Sales.Add(sale);

            // Criar itens da venda
            foreach (var item in dto.Items)
            {
                var saleItem = new SaleItem
                {
                    Id = Guid.NewGuid(),
                    SaleId = sale.Id,
                    Description = item.Descricao,
                    Quantity = (int)item.Quantidade,
                    UnitPrice = item.PrecoUnitario,
                    TotalPrice = item.Total,
                    DiscountAmount = 0
                };

                // Vincular referências
                if (item.ReferenciaId.HasValue)
                {
                    switch (item.Tipo.ToUpper())
                    {
                        case "MANIPULACAO":
                            saleItem.ManipulationOrderId = item.ReferenciaId;
                            break;
                        case "PRODUTO":
                        case "CATALOGO":
                            saleItem.CatalogProductId = item.ReferenciaId;
                            break;
                        case "MATERIA_PRIMA":
                            saleItem.RawMaterialId = item.ReferenciaId;
                            // Buscar info do controlado se necessário
                            if (item.ReferenciaId.HasValue)
                            {
                                var rm = await _context.RawMaterials.FindAsync(item.ReferenciaId.Value);
                                if (rm != null && rm.ControlType != null && rm.ControlType != "COMUM")
                                {
                                    saleItem.ControlType = rm.ControlType;
                                }
                            }
                            break;
                    }
                }

                _context.SaleItems.Add(saleItem);
            }

            // Criar registro de pagamento
            var payment = new SalePayment
            {
                Id = Guid.NewGuid(),
                SaleId = sale.Id,
                PaymentMethod = dto.Payment.Method,
                Amount = dto.Payment.Amount,
                CashReceived = dto.Payment.CashReceived,
                ChangeAmount = dto.Payment.ChangeAmount,
                CardBrand = dto.Payment.CardBrand,
                CardLastDigits = dto.Payment.CardLastDigits,
                Installments = dto.Payment.Installments ?? 1,
                Nsu = dto.Payment.Nsu,
                PixTransactionId = dto.Payment.PixTransactionId,
                PaymentStatus = "APPROVED",
                PaymentDate = DateTime.UtcNow,
                ProcessedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow
            };
            _context.SalePayments.Add(payment);

            await _context.SaveChangesAsync();

            // Baixar estoque
            foreach (var item in dto.Items)
            {
                var qty = (int)item.Quantidade;

                if (item.Tipo.ToUpper() == "PRODUTO" || item.Tipo.ToUpper() == "CATALOGO")
                {
                    // Baixar estoque de CatalogProduct
                    if (item.ReferenciaId.HasValue)
                    {
                        var product = await _context.CatalogProducts.FindAsync(item.ReferenciaId.Value);
                        if (product != null)
                        {
                            product.StockQuantity -= qty;
                            if (product.StockQuantity < 0) product.StockQuantity = 0;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
                else if (item.Tipo.ToUpper() == "MATERIA_PRIMA")
                {
                    // Baixar estoque de RawMaterial
                    if (item.ReferenciaId.HasValue)
                    {
                        var rawMaterial = await _context.RawMaterials.FindAsync(item.ReferenciaId.Value);
                        if (rawMaterial != null)
                        {
                            rawMaterial.CurrentStock -= item.Quantidade;
                            if (rawMaterial.CurrentStock < 0) rawMaterial.CurrentStock = 0;
                            rawMaterial.UpdatedAt = DateTime.UtcNow;

                            // Criar movimento de estoque
                            var stockMovement = new StockMovement
                            {
                                Id = Guid.NewGuid(),
                                EstablishmentId = establishmentId,
                                RawMaterialId = rawMaterial.Id,
                                MovementType = "VENDA",
                                Quantity = item.Quantidade,
                                StockBefore = rawMaterial.CurrentStock + item.Quantidade,
                                StockAfter = rawMaterial.CurrentStock,
                                MovementDate = DateTime.UtcNow,
                                PerformedByEmployeeId = employeeId,
                                DocumentNumber = sale.Code,
                                Reason = $"Venda PDV - {sale.Code}",
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.StockMovements.Add(stockMovement);

                            // Se for controlado, registrar movimentação SNGPC
                            if (rawMaterial.ControlType != null && rawMaterial.ControlType != "COMUM")
                            {
                                var controlledMovement = new ControlledSubstanceMovement
                                {
                                    Id = Guid.NewGuid(),
                                    EstablishmentId = establishmentId,
                                    RawMaterialId = rawMaterial.Id,
                                    MovementDate = DateTime.UtcNow,
                                    MovementType = "SAIDA",
                                    ControlledList = rawMaterial.ControlType,
                                    SubstanceDcbCode = rawMaterial.DcbCode ?? "",
                                    SubstanceName = rawMaterial.Name,
                                    Quantity = item.Quantidade,
                                    Unit = rawMaterial.Unit,
                                    BalanceBefore = rawMaterial.CurrentStock + item.Quantidade,
                                    BalanceAfter = rawMaterial.CurrentStock,
                                    SaleId = sale.Id,
                                    SngpcSent = false,
                                    SngpcStatus = "PENDENTE",
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedByEmployeeId = employeeId
                                };
                                _context.ControlledSubstanceMovements.Add(controlledMovement);
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            await _cashService.RegisterSaleInCashRegisterAsync(
                openCashRegister.Id, sale.Id, total, dto.Payment.Method, employeeId);

            await transaction.CommitAsync();

            return Ok(ApiResponse<UnifiedSaleResultDto>.SuccessResponse(new UnifiedSaleResultDto
            {
                SaleId = sale.Id,
                SaleCode = sale.Code,
                Total = total,
                PaidAmount = dto.Payment.Amount,
                ChangeAmount = dto.Payment.ChangeAmount ?? 0,
                PaymentMethod = dto.Payment.Method,
                ItemsCount = dto.Items.Count
            }, "Venda finalizada com sucesso!"));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(ApiResponse<UnifiedSaleResultDto>.ErrorResponse("Erro ao processar venda: " + ex.Message));
        }
    }

    // ================================================================
    // GERADOR DE RECIBO
    // ================================================================

    private async Task<SaleReceiptDto?> GenerateReceipt(Guid saleId)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == saleId);

        if (sale == null)
            return null;

        var establishment = await _context.Establishments
            .FirstOrDefaultAsync(e => e.Id == sale.EstablishmentId);

        var customer = sale.CustomerId.HasValue ?
            await _context.Customers.FirstOrDefaultAsync(c => c.Id == sale.CustomerId) : null;

        return new SaleReceiptDto
        {
            Code = sale.Code,
            SaleDate = sale.SaleDate,
            CustomerName = customer?.FullName,
            Items = sale.Items.Select(i => new ReceiptItemDto
            {
                Description = i.Description,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),
            Subtotal = sale.Subtotal,
            DiscountAmount = sale.DiscountAmount,
            TotalAmount = sale.TotalAmount,
            PaymentMethod = sale.PaymentMethod,
            PaidAmount = sale.PaidAmount ?? 0,
            ChangeAmount = sale.ChangeAmount ?? 0,
            EstablishmentName = establishment?.NomeFantasia ?? "Salvelle",
            EstablishmentAddress = establishment != null ?
                $"{establishment.Street}, {establishment.Number} - {establishment.Neighborhood}, {establishment.City}/{establishment.State}" : "",
            EstablishmentCnpj = establishment?.Cnpj ?? ""
        };
    }
}

// ================================================================
// DTOs DO PDV
// ================================================================

public class CashRegisterStatusDto
{
    public bool IsOpen { get; set; }
    public Guid? Id { get; set; }
    public string? Code { get; set; }
    public DateTime? OpeningDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal TotalCash { get; set; }
    public decimal TotalCard { get; set; }
    public decimal TotalPix { get; set; }
    public decimal TotalSales { get; set; }
    public int SalesCount { get; set; }
    public string? OperatorName { get; set; }
}

public class PendingManipulationDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string FormulaName { get; set; } = "";
    public decimal QuantityToProduce { get; set; }
    public string Unit { get; set; } = "";
    public DateTime? CompletionDate { get; set; }
    public decimal SuggestedPrice { get; set; }
}

public class OnlineOrderPdvDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
    public string? PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemsCount { get; set; }
    public List<OnlineOrderItemPdvDto> Items { get; set; } = new();
}

public class OnlineOrderItemPdvDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class ProcessOnlineOrderDto
{
    public string PaymentMethod { get; set; } = "DINHEIRO";
    public decimal PaidAmount { get; set; }
    public decimal? CashReceived { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastDigits { get; set; }
    public int? Installments { get; set; }
    public string? Nsu { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? PixTransactionId { get; set; }
}

public class ProcessOnlineOrderResultDto
{
    public Guid SaleId { get; set; }
    public string SaleCode { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal? ChangeAmount { get; set; }
}

public class ProductSearchDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Code { get; set; }
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public bool IsOnPromotion { get; set; }
    public int StockQuantity { get; set; }
    public string Unit { get; set; } = "UN";
    public string? CategoryName { get; set; }

    // Novos campos para diferenciar fonte
    public string Source { get; set; } = "CATALOGO"; // CATALOGO ou MATERIA_PRIMA
    public string? ControlType { get; set; } // COMUM, LISTA_A, LISTA_B, etc.
    public bool IsControlled { get; set; }
}

public class CreateUnifiedSaleDto
{
    public Guid? CustomerId { get; set; }
    public List<UnifiedSaleItemDto> Items { get; set; } = new();
    public decimal? Discount { get; set; }
    public UnifiedPaymentDto Payment { get; set; } = new();
}

public class UnifiedSaleItemDto
{
    public string Tipo { get; set; } = ""; // PRODUTO, MANIPULACAO, MATERIA_PRIMA
    public Guid? ReferenciaId { get; set; }
    public string Descricao { get; set; } = "";
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
}

public class UnifiedPaymentDto
{
    public string Method { get; set; } = "DINHEIRO";
    public decimal Amount { get; set; }
    public decimal? CashReceived { get; set; }
    public decimal? ChangeAmount { get; set; }
    public string? CardBrand { get; set; }
    public string? CardLastDigits { get; set; }
    public int? Installments { get; set; }
    public string? Nsu { get; set; }
    public string? PixTransactionId { get; set; }
}

public class UnifiedSaleResultDto
{
    public Guid SaleId { get; set; }
    public string SaleCode { get; set; } = "";
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string PaymentMethod { get; set; } = "";
    public int ItemsCount { get; set; }
}