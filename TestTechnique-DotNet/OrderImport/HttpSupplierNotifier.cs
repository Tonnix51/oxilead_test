using System.Net.Http.Json;

namespace OrderImport;

public sealed class HttpSupplierNotifier : ISupplierNotifier
{
    private readonly HttpClient _httpClient;

    public HttpSupplierNotifier(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        // Clé dans un en-tête et non dans l'URL : les URL finissent dans les logs des proxys et serveurs
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    public async Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken)
    {
        // Corps JSON : les valeurs sont sérialisées, plus de concaténation dans la query string
        var payload = new { supplier = order.SupplierId, reference = order.Reference };

        using var response = await _httpClient.PostAsJsonAsync("send", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
