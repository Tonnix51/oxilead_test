using OrderImport.Domain;

namespace OrderImport.Notifications;

public interface ISupplierNotifier
{
    Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken);
}
