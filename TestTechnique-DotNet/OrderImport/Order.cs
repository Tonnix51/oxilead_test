namespace OrderImport;

public class Order
{
    public string SupplierId { get; set; }
    public string Reference { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
}
