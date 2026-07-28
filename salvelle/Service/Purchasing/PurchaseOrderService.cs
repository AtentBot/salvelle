using Microsoft.EntityFrameworkCore;
using Data;
using DTOs.Purchasing;
using Models.Purchasing;
using Models.Employees;
using Models.Pharmacy;

namespace Service.Purchasing;

public class PurchaseOrderService
{
    private readonly AppDbContext _context;

    public PurchaseOrderService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, PurchaseOrder? Order)> CreatePurchaseOrderAsync(
        CreatePurchaseOrderDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Id == dto.SupplierId && s.EstablishmentId == establishmentId);

            if (supplier == null)
                return (false, "Fornecedor não encontrado", null);

            if (supplier.Status?.ToUpper() != "ATIVO")
                return (false, "Fornecedor não está ativo", null);

            var orderNumber = await GenerateOrderNumberAsync(establishmentId);

            decimal totalValue = 0;
            var items = new List<PurchaseOrderItem>();

            foreach (var itemDto in dto.Items)
            {
                var rawMaterial = await _context.RawMaterials
                    .FirstOrDefaultAsync(r => r.Id == itemDto.RawMaterialId && r.EstablishmentId == establishmentId);

                if (rawMaterial == null)
                    return (false, $"Matéria-prima ID {itemDto.RawMaterialId} não encontrada", null);

                var itemTotal = itemDto.QuantityOrdered * itemDto.UnitPrice;
                var discountAmount = itemTotal * (itemDto.DiscountPercentage / 100);
                var finalItemPrice = itemTotal - discountAmount;

                var item = new PurchaseOrderItem
                {
                    RawMaterialId = itemDto.RawMaterialId,
                    QuantityOrdered = itemDto.QuantityOrdered,
                    QuantityReceived = 0,
                    Unit = itemDto.Unit,
                    UnitPrice = itemDto.UnitPrice,
                    DiscountPercentage = itemDto.DiscountPercentage,
                    TotalPrice = finalItemPrice,
                    Notes = itemDto.Notes,
                    Status = "PENDENTE"
                };

                items.Add(item);
                totalValue += finalItemPrice;
            }

            var finalValue = totalValue - dto.DiscountValue + dto.ShippingValue;

            var order = new PurchaseOrder
            {
                OrderNumber = orderNumber,
                SupplierId = dto.SupplierId,
                EstablishmentId = establishmentId,
                OrderDate = DateTime.UtcNow,
                ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
                Status = "RASCUNHO",
                TotalValue = totalValue,
                DiscountValue = dto.DiscountValue,
                ShippingValue = dto.ShippingValue,
                FinalValue = finalValue,
                Notes = dto.Notes,
                CreatedByEmployeeId = employeeId,
                CreatedAt = DateTime.UtcNow,
                Items = items
            };

            _context.PurchaseOrders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Pedido criado com sucesso", order);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao criar pedido: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> ApprovePurchaseOrderAsync(
        int orderId,
        Guid establishmentId,
        Guid employeeId)
    {
        var order = await _context.PurchaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
            return (false, "Pedido não encontrado");

        if (order.Status?.ToUpper() != "RASCUNHO")
            return (false, $"Pedido não pode ser aprovado no status {order.Status}");

        order.Status = "APROVADO";
        order.ApprovedByEmployeeId = employeeId;
        order.ApprovedAt = DateTime.UtcNow;
        order.UpdatedByEmployeeId = employeeId;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Pedido aprovado com sucesso");
    }

    public async Task<(bool Success, string Message)> SendPurchaseOrderAsync(
        int orderId,
        Guid establishmentId,
        Guid employeeId)
    {
        var order = await _context.PurchaseOrders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
            return (false, "Pedido não encontrado");

        if (order.Status?.ToUpper() != "APROVADO")
            return (false, $"Apenas pedidos aprovados podem ser enviados");

        order.Status = "ENVIADO";
        order.UpdatedByEmployeeId = employeeId;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return (true, "Pedido enviado ao fornecedor");
    }

    public async Task<(bool Success, string Message)> ReceivePurchaseOrderAsync(
        int orderId,
        ReceivePurchaseOrderDto dto,
        Guid establishmentId,
        Guid employeeId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = await _context.PurchaseOrders
                .Include(o => o.Items)
                .ThenInclude(i => i.RawMaterial)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

            if (order == null)
                return (false, "Pedido não encontrado");

            if (order.Status?.ToUpper() != "ENVIADO" && order.Status?.ToUpper() != "RECEBIDO_PARCIAL")
                return (false, $"Pedido não pode ser recebido no status {order.Status}");

            foreach (var itemDto in dto.Items)
            {
                var orderItem = order.Items.FirstOrDefault(i => i.Id == itemDto.PurchaseOrderItemId);
                if (orderItem == null)
                    return (false, $"Item do pedido {itemDto.PurchaseOrderItemId} não encontrado");

                if (orderItem.QuantityReceived + itemDto.QuantityReceived > orderItem.QuantityOrdered)
                    return (false, $"Quantidade recebida excede quantidade pedida para {orderItem.RawMaterial?.Name}");

                var batch = new Batch
                {
                    RawMaterialId = orderItem.RawMaterialId,
                    SupplierId = order.SupplierId,
                    BatchNumber = itemDto.BatchNumber,
                    InvoiceNumber = dto.SupplierInvoiceNumber ?? "", 
                    ReceivedQuantity = itemDto.QuantityReceived,  
                    CurrentQuantity = itemDto.QuantityReceived,
                    UnitCost = orderItem.UnitPrice, 
                    ReceivedDate = dto.ActualDeliveryDate,
                    ExpiryDate = itemDto.ExpiryDate,
                    ManufactureDate = itemDto.ManufactureDate,
                    Status = "QUARENTENA",
                    CertificateNumber = itemDto.CertificateOfAnalysis, 
                    CreatedByEmployeeId = employeeId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Batches.Add(batch);
                await _context.SaveChangesAsync();

                var batchReceiving = new BatchReceiving
                {
                    PurchaseOrderItemId = orderItem.Id,
                    BatchId = batch.Id,
                    QuantityReceived = itemDto.QuantityReceived,
                    ReceivedDate = dto.ActualDeliveryDate,
                    ReceivedByEmployeeId = employeeId,
                    Notes = itemDto.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.BatchReceivings.Add(batchReceiving);

                orderItem.QuantityReceived += itemDto.QuantityReceived;
                orderItem.Status = orderItem.QuantityReceived >= orderItem.QuantityOrdered
                    ? "RECEBIDO_TOTAL"
                    : "RECEBIDO_PARCIAL";
                orderItem.UpdatedAt = DateTime.UtcNow;

                // ✅ CALCULA ESTOQUE ATUAL
                var currentStock = await _context.StockMovements
                    .Where(sm => sm.RawMaterialId == orderItem.RawMaterialId &&
                                 sm.EstablishmentId == establishmentId)
                    .OrderByDescending(sm => sm.MovementDate)
                    .Select(sm => sm.StockAfter)
                    .FirstOrDefaultAsync();

                var stockMovement = new StockMovement
                {
                    BatchId = batch.Id,
                    EstablishmentId = establishmentId,
                    RawMaterialId = orderItem.RawMaterialId,
                    MovementType = "ENTRADA",
                    Quantity = itemDto.QuantityReceived,
                    StockBefore = currentStock,
                    StockAfter = currentStock + itemDto.QuantityReceived,
                    MovementDate = dto.ActualDeliveryDate,
                    Reason = $"Recebimento OC {order.OrderNumber}",
                    SupplierId = order.SupplierId,
                    PerformedByEmployeeId = employeeId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockMovements.Add(stockMovement);
            }

            bool allItemsFullyReceived = order.Items.All(i => i.Status?.ToUpper() == "RECEBIDO_TOTAL");
            bool anyItemReceived = order.Items.Any(i => i.QuantityReceived > 0);

            order.Status = allItemsFullyReceived ? "RECEBIDO_TOTAL" : "RECEBIDO_PARCIAL";
            order.ActualDeliveryDate = dto.ActualDeliveryDate;
            order.SupplierInvoiceNumber = dto.SupplierInvoiceNumber;
            order.UpdatedByEmployeeId = employeeId;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, allItemsFullyReceived
                ? "Pedido recebido totalmente"
                : "Pedido recebido parcialmente");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Erro ao receber pedido: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> CancelPurchaseOrderAsync(
        int orderId,
        Guid establishmentId,
        Guid employeeId)
    {
        var order = await _context.PurchaseOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.EstablishmentId == establishmentId);

        if (order == null)
            return (false, "Pedido não encontrado");

        if (order.Status?.ToUpper() == "RECEBIDO_TOTAL")
            return (false, "Pedido já recebido não pode ser cancelado");

        if (order.Items.Any(i => i.QuantityReceived > 0))
            return (false, "Pedido com itens já recebidos não pode ser cancelado");

        order.Status = "CANCELADO";
        order.UpdatedByEmployeeId = employeeId;
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var item in order.Items)
        {
            item.Status = "CANCELADO";
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return (true, "Pedido cancelado com sucesso");
    }

    private async Task<string> GenerateOrderNumberAsync(Guid establishmentId)  // ✅ MUDOU DE int PARA Guid
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"OC{year}";

        var lastOrder = await _context.PurchaseOrders
            .Where(o => o.EstablishmentId == establishmentId && o.OrderNumber.StartsWith(prefix))
            .OrderByDescending(o => o.OrderNumber)
            .Select(o => o.OrderNumber)
            .FirstOrDefaultAsync();

        int nextNumber = 1;
        if (lastOrder != null && lastOrder.Length > prefix.Length)
        {
            var numberPart = lastOrder.Substring(prefix.Length);
            if (int.TryParse(numberPart, out int lastNumber))
            {
                nextNumber = lastNumber + 1;
            }
        }

        return $"{prefix}{nextNumber:D6}";
    }
}