using System.Net.Http.Json;
using OrderImport.Domain;

namespace OrderImport.Notifications;

// Client typé : HttpClient fourni par IHttpClientFactory, configuré dans Program (adresse, timeout, clé)
public sealed class HttpSupplierNotifier : ISupplierNotifier
{
    private readonly HttpClient _httpClient;

    public HttpSupplierNotifier(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken)
    {
        // Corps JSON : les valeurs sont sérialisées, plus de concaténation dans la query string
        var payload = new { supplier = order.SupplierId, reference = order.Reference };

        using var response = await _httpClient.PostAsJsonAsync("send", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
