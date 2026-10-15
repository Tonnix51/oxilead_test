namespace OrderImport;

public interface ISupplierNotifier
{
    Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken);
}
