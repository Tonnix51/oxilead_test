namespace OrderImport.Tests;

public class OrderImportServiceTests
{
    private const string Header = "SupplierId,Reference,Amount,Status";

    private static Task<ImportResult> ImportAsync(ISupplierNotifier notifier, params string[] lines)
    {
        var csv = string.Join(Environment.NewLine, new[] { Header }.Concat(lines));
        return new OrderImportService(notifier).ImportAsync(new StringReader(csv));
    }

    [Fact]
    public async Task Import_FichierExemple_ImporteLesLignesValidesEtRejetteLesAutres()
    {
        var result = await ImportAsync(new RecordingNotifier(),
            "SUP-001,CMD-1001,150.00,STANDARD",
            "SUP-002,CMD-1002,89.90,URGENT",
            "SUP-001,CMD-1001,150.00,STANDARD",
            "SUP-003,CMD-1003,ABC,STANDARD",
            "SUP-004,CMD-1004,42.50,PRIORITAIRE");

        Assert.Equal(new[] { "CMD-1001", "CMD-1002" }, result.Imported.Select(o => o.Reference));
        Assert.Equal(new[] { 4, 5, 6 }, result.Rejected.Select(r => r.LineNumber));
    }

    [Fact]
    public async Task Import_ReferenceEnDouble_EstRejetee()
    {
        var result = await ImportAsync(new RecordingNotifier(),
            "SUP-001,CMD-1001,150.00,STANDARD",
            "SUP-002,cmd-1001,20.00,STANDARD");

        Assert.Single(result.Imported);
        var rejected = Assert.Single(result.Rejected);
        Assert.Equal(3, rejected.LineNumber);
        Assert.Contains("déjà importée", rejected.Reason);
    }

    [Fact]
    public async Task Import_CommandeUrgente_EstNotifiee_PasLesStandards()
    {
        var notifier = new RecordingNotifier();

        await ImportAsync(notifier,
            "SUP-001,CMD-1001,150.00,STANDARD",
            "SUP-002,CMD-1002,89.90,URGENT");

        Assert.Equal(new[] { "CMD-1002" }, notifier.NotifiedReferences);
    }

    [Fact]
    public async Task Import_NotificationEnEchec_NInterromptPasLImport()
    {
        var result = await ImportAsync(new FailingNotifier(),
            "SUP-002,CMD-1002,89.90,URGENT",
            "SUP-003,CMD-1003,10.00,STANDARD");

        Assert.Equal(2, result.Imported.Count);
        Assert.Empty(result.Rejected);
        var failure = Assert.Single(result.FailedNotifications);
        Assert.Contains("CMD-1002", failure);
    }

    [Fact]
    public async Task Import_LignesVides_SontIgnorees()
    {
        var result = await ImportAsync(new RecordingNotifier(),
            "",
            "SUP-001,CMD-1001,150.00,STANDARD",
            "   ");

        Assert.Single(result.Imported);
        Assert.Empty(result.Rejected);
    }

    [Fact]
    public async Task Import_EnTeteInattendu_LeveUneErreur()
    {
        var service = new OrderImportService(new RecordingNotifier());

        await Assert.ThrowsAsync<InvalidDataException>(
            () => service.ImportAsync(new StringReader("Foo,Bar\nSUP-001,CMD-1001,10.00,STANDARD")));
    }

    [Fact]
    public async Task Import_FichierVide_LeveUneErreur()
    {
        var service = new OrderImportService(new RecordingNotifier());

        await Assert.ThrowsAsync<InvalidDataException>(() => service.ImportAsync(new StringReader("")));
    }

    [Fact]
    public async Task Import_Annule_PropageLAnnulation()
    {
        var service = new OrderImportService(new RecordingNotifier());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new StringReader(Header), cts.Token));
    }

    private sealed class RecordingNotifier : ISupplierNotifier
    {
        public List<string> NotifiedReferences { get; } = new();

        public Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken)
        {
            NotifiedReferences.Add(order.Reference);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingNotifier : ISupplierNotifier
    {
        public Task NotifyUrgentOrderAsync(Order order, CancellationToken cancellationToken) =>
            throw new HttpRequestException("API indisponible");
    }
}
