using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderImport.Importing;
using OrderImport.Notifications;

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

        using var host = BuildHost();
        var service = host.Services.GetRequiredService<OrderImportService>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

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

    private static IHost BuildHost()
    {
        // Pas d'args transmis au builder : le chemin du fichier n'est pas une clé de configuration
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options => options.SingleLine = true);

        // La clé vient de la configuration (variables d'environnement, ou coffre de secrets en prod)
        var apiKey = builder.Configuration[ApiKeyVariable];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            builder.Services.AddSingleton<ISupplierNotifier, DisabledSupplierNotifier>();
        }
        else
        {
            builder.Services.AddHttpClient<ISupplierNotifier, HttpSupplierNotifier>(client =>
            {
                client.BaseAddress = NotificationApiUrl;
                client.Timeout = TimeSpan.FromSeconds(10);
                // Clé dans un en-tête et non dans l'URL : les URL finissent dans les logs des proxys et serveurs
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
            });
        }

        builder.Services.AddTransient<OrderImportService>();

        return builder.Build();
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
