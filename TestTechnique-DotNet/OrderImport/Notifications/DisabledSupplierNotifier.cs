using Microsoft.Extensions.Logging;
using OrderImport.Domain;

namespace OrderImport.Notifications;

// Utilisé quand aucune clé API n'est configurée : l'import fonctionne, sans appel externe
public sealed class DisabledSupplierNotifier : ISupplierNotifier
{
    private readonly ILogger<DisabledSupplierNotifier> _logger;

    public DisabledSupplierNotifier(ILogger<DisabledSupplierNotifier> logger)
    {
        _logger = logger;
    }

    public Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken)
    {
        _logger.LogWarning("Notifications désactivées : commande {Reference} non notifiée.", order.Reference);
        return Task.CompletedTask;
    }
}
