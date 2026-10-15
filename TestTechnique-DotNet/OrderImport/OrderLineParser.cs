using System.Globalization;
using System.Text.RegularExpressions;

namespace OrderImport;

public sealed record ParseResult(Order? Order, string? Error)
{
    public static ParseResult Success(Order order) => new(order, null);
    public static ParseResult Failure(string error) => new(null, error);
}

public static class OrderLineParser
{
    public const string ExpectedHeader = "SupplierId,Reference,Amount,Status";
    private const int ColumnCount = 4;

    // Format strict sur les identifiants : ils partent vers l'API de notification et dans les logs
    private static readonly Regex IdentifierPattern = new("^[A-Za-z0-9-]{1,32}$", RegexOptions.Compiled);

    public static ParseResult Parse(string line)
    {
        var parts = line.Split(',');
        if (parts.Length != ColumnCount)
            return ParseResult.Failure($"{parts.Length} colonne(s) au lieu de {ColumnCount}");

        var supplierId = parts[0].Trim();
        var reference = parts[1].Trim();

        if (!IdentifierPattern.IsMatch(supplierId))
            return ParseResult.Failure("SupplierId invalide");

        if (!IdentifierPattern.IsMatch(reference))
            return ParseResult.Failure("Reference invalide");

        // Culture invariante : le fichier utilise le point décimal, quelle que soit la langue du poste
        if (!decimal.TryParse(parts[2].Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
            || amount <= 0)
            return ParseResult.Failure("montant invalide");

        if (!TryParseStatus(parts[3].Trim(), out var status))
            return ParseResult.Failure("statut inconnu");

        return ParseResult.Success(new Order(supplierId, reference, amount, status));
    }

    private static bool TryParseStatus(string value, out OrderStatus status)
    {
        // Pas d'Enum.TryParse : il accepte aussi les valeurs numériques ("1", "42")
        switch (value.ToUpperInvariant())
        {
            case "STANDARD":
                status = OrderStatus.Standard;
                return true;
            case "URGENT":
                status = OrderStatus.Urgent;
                return true;
            default:
                status = default;
                return false;
        }
    }
}
