namespace OrderImport;

public enum OrderStatus
{
    Standard,
    Urgent
}

public sealed record Order(string SupplierId, string Reference, decimal Amount, OrderStatus Status);
