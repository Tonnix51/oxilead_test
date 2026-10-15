namespace OrderImport;

public class OrderImportService
{
    private readonly ISupplierNotifier _notifier;

    public OrderImportService(ISupplierNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task<ImportResult> ImportAsync(TextReader reader, CancellationToken cancellationToken = default)
    {
        var header = await reader.ReadLineAsync(cancellationToken);
        if (!string.Equals(header?.Trim(), OrderLineParser.ExpectedHeader, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"En-tête CSV inattendu, attendu : {OrderLineParser.ExpectedHeader}");

        var result = new ImportResult();
        var importedReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lineNumber = 1;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Une ligne en échec est rejetée, le reste du fichier continue d'être traité
            try
            {
                await ImportLineAsync(line, lineNumber, result, importedReferences, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                result.Rejected.Add(new RejectedLine(lineNumber, $"erreur inattendue ({ex.GetType().Name})"));
            }
        }

        return result;
    }

    private async Task ImportLineAsync(string line, int lineNumber, ImportResult result,
        HashSet<string> importedReferences, CancellationToken cancellationToken)
    {
        var parsed = OrderLineParser.Parse(line);
        if (parsed.Order is null)
        {
            result.Rejected.Add(new RejectedLine(lineNumber, parsed.Error!));
            return;
        }

        var order = parsed.Order;
        if (!importedReferences.Add(order.Reference))
        {
            result.Rejected.Add(new RejectedLine(lineNumber, $"référence {order.Reference} déjà importée"));
            return;
        }

        result.Imported.Add(order);

        if (order.Status == OrderStatus.Urgent)
            await NotifyAsync(order, result, cancellationToken);
    }

    private async Task NotifyAsync(Order order, ImportResult result, CancellationToken cancellationToken)
    {
        // La commande reste importée même si la notification échoue : on la signale pour relance
        try
        {
            await _notifier.NotifyUrgentOrderAsync(order, cancellationToken);
        }
        // Le timeout HttpClient lève aussi une TaskCanceledException, d'où le filtre sur le jeton
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            result.FailedNotifications.Add($"{order.Reference} ({ex.GetType().Name})");
        }
    }
}
