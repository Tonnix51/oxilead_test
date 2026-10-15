namespace OrderImport;

internal static class Program
{
    private const string ApiKeyVariable = "NOTIF_API_KEY";
    private static readonly Uri NotificationApiUrl = new("https://api.notif-fournisseurs.fr/");

    private static async Task<int> Main(string[] args)
    {
        var filePath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "sample-orders.csv");
        if (!File.Exists(filePath))
        {
            Console.Error.WriteLine($"Fichier introuvable : {filePath}");
            return 1;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using var httpClient = new HttpClient { BaseAddress = NotificationApiUrl, Timeout = TimeSpan.FromSeconds(10) };
        var service = new OrderImportService(CreateNotifier(httpClient));

        Console.WriteLine($"Début de l'import : {filePath}");
        try
        {
            using var reader = new StreamReader(filePath);
            var result = await service.ImportAsync(reader, cts.Token);
            PrintSummary(result);
            return result.Rejected.Count == 0 && result.FailedNotifications.Count == 0 ? 0 : 2;
        }
        catch (InvalidDataException ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Import annulé.");
            return 1;
        }
    }

    private static ISupplierNotifier CreateNotifier(HttpClient httpClient)
    {
        // La clé n'est plus dans le code : elle vient de l'environnement (ou d'un coffre de secrets en prod)
        var apiKey = Environment.GetEnvironmentVariable(ApiKeyVariable);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Console.WriteLine($"{ApiKeyVariable} non définie : notifications désactivées.");
            return new DisabledSupplierNotifier();
        }

        return new HttpSupplierNotifier(httpClient, apiKey);
    }

    private static void PrintSummary(ImportResult result)
    {
        foreach (var order in result.Imported)
            Console.WriteLine($"Commande {order.Reference} ajoutée pour {order.SupplierId}.");

        foreach (var rejected in result.Rejected)
            Console.WriteLine($"Ligne {rejected.LineNumber} rejetée : {rejected.Reason}.");

        foreach (var failure in result.FailedNotifications)
            Console.WriteLine($"Notification en échec : {failure}.");

        Console.WriteLine($"{result.Imported.Count} commande(s) importée(s), {result.Rejected.Count} ligne(s) rejetée(s).");
    }
}
