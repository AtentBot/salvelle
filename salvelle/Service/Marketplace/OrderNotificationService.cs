using Data;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Service.Marketplace;

/// <summary>
/// Serviço de notificações de pedidos do marketplace.
/// Envia notificações por push (FCM) e email conforme eventos de pedidos.
/// </summary>
public class OrderNotificationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderNotificationService> _logger;

    public OrderNotificationService(AppDbContext db, ILogger<OrderNotificationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Notificar farmácia sobre novo pedido
    /// </summary>
    public async Task NotifyPharmacyNewOrder(OnlineOrder order)
    {
        var pharmacy = await _db.Establishments.FindAsync(order.EstablishmentId);
        if (pharmacy == null) return;

        _logger.LogInformation(
            "Novo pedido {OrderNumber} para farmácia {Pharmacy} — R$ {Total:F2}",
            order.OrderNumber, pharmacy.NomeFantasia, order.Total);

        // TODO: Enviar push notification para app da farmácia
        // TODO: Enviar email/WhatsApp se configurado
        // await SendPushToPharmacy(order.EstablishmentId, title, body);
    }

    /// <summary>
    /// Notificar cliente sobre atualização de status do pedido
    /// </summary>
    public async Task NotifyCustomerOrderUpdate(Guid orderId, string newStatus)
    {
        var order = await _db.OnlineOrders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order?.Customer == null) return;

        var (title, body) = newStatus switch
        {
            "CONFIRMED" => ("Pedido Confirmado! ✅", $"Seu pedido {order.OrderNumber} foi aceito pela farmácia."),
            "PREPARING" => ("Em Preparação 🔬", $"Seu pedido {order.OrderNumber} está sendo preparado."),
            "READY" => ("Pedido Pronto! 📦", $"Seu pedido {order.OrderNumber} está pronto para retirada/entrega."),
            "DELIVERED" => ("Pedido Entregue! 🎉", $"Seu pedido {order.OrderNumber} foi entregue. Avalie sua experiência!"),
            "CANCELLED" => ("Pedido Cancelado ❌", $"Seu pedido {order.OrderNumber} foi cancelado."),
            _ => ($"Atualização do pedido", $"Seu pedido {order.OrderNumber} foi atualizado para: {newStatus}")
        };

        _logger.LogInformation("Notificação para cliente {CustomerId}: {Title}", order.CustomerId, title);

        // Buscar FCM token do cliente
        var device = await _db.CustomerDevices
            .Where(d => d.CustomerId == order.CustomerId && d.IsActive)
            .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
            .FirstOrDefaultAsync();

        if (device != null)
        {
            await SendFcmNotification(device.DeviceToken, title, body, new Dictionary<string, string>
            {
                { "type", "ORDER_UPDATE" },
                { "orderId", orderId.ToString() },
                { "status", newStatus }
            });
        }
    }

    /// <summary>
    /// Enviar push notification via Firebase Cloud Messaging
    /// </summary>
    private async Task SendFcmNotification(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        // TODO: Implementar integração real com Firebase Admin SDK
        // Por enquanto, apenas loga a intenção
        _logger.LogInformation("FCM Push → Token: {Token}... | {Title}: {Body}",
            fcmToken[..Math.Min(20, fcmToken.Length)], title, body);

        await Task.CompletedTask;
    }
}
