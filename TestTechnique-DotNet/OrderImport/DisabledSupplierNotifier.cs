namespace OrderImport;

// Utilisé quand aucune clé API n'est configurée : l'import fonctionne, sans appel externe
public sealed class DisabledSupplierNotifier : ISupplierNotifier
{
    public Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Notification désactivée : commande {order.Reference} non notifiée.");
        return Task.CompletedTask;
    }
}
