namespace OrderImport;

public sealed record RejectedLine(int LineNumber, string Reason);

public sealed class ImportResult
{
    public List<Order> Imported { get; } = new();
    public List<RejectedLine> Rejected { get; } = new();
    public List<string> FailedNotifications { get; } = new();
}
