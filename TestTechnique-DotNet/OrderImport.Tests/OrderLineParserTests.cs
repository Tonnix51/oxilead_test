using System.Globalization;
using OrderImport.Domain;
using OrderImport.Parsing;

namespace OrderImport.Tests;

public class OrderLineParserTests
{
    [Fact]
    public void Parse_LigneValide_RetourneLaCommande()
    {
        var result = OrderLineParser.Parse("SUP-001,CMD-1001,150.00,STANDARD");

        Assert.Null(result.Error);
        Assert.Equal(new Order("SUP-001", "CMD-1001", 150.00m, OrderStatus.Standard), result.Order);
    }

    [Fact]
    public void Parse_MontantAvecPoint_FonctionneSurUnPosteEnFrancais()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        try
        {
            var result = OrderLineParser.Parse("SUP-002,CMD-1002,89.90,URGENT");

            Assert.Equal(89.90m, result.Order?.Amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("")]
    [InlineData("-5")]
    [InlineData("0")]
    [InlineData("1,5")]
    [InlineData("1e3")]
    public void Parse_MontantInvalide_EstRejete(string amount)
    {
        var result = OrderLineParser.Parse($"SUP-001,CMD-1001,{amount},STANDARD");

        Assert.Null(result.Order);
        Assert.NotNull(result.Error);
    }

    [Theory]
    [InlineData("SUP-001,CMD-1001")]
    [InlineData("SUP-001,CMD-1001,10.00,STANDARD,EN-TROP")]
    [InlineData("")]
    public void Parse_NombreDeColonnesIncorrect_EstRejete(string line)
    {
        var result = OrderLineParser.Parse(line);

        Assert.Null(result.Order);
        Assert.Contains("colonne", result.Error);
    }

    [Theory]
    [InlineData("SUP-001,CMD-1&ref=autre,10.00,STANDARD")]
    [InlineData("SUP 001,CMD-1001,10.00,STANDARD")]
    [InlineData(",CMD-1001,10.00,STANDARD")]
    public void Parse_IdentifiantInvalide_EstRejete(string line)
    {
        var result = OrderLineParser.Parse(line);

        Assert.Null(result.Order);
        Assert.Contains("invalide", result.Error);
    }

    [Theory]
    [InlineData("PRIORITAIRE")]
    [InlineData("1")]
    [InlineData("")]
    public void Parse_StatutInconnu_EstRejete(string status)
    {
        var result = OrderLineParser.Parse($"SUP-001,CMD-1001,10.00,{status}");

        Assert.Null(result.Order);
        Assert.Equal("statut inconnu", result.Error);
    }

    [Fact]
    public void Parse_StatutEnMinuscules_EstAccepte()
    {
        var result = OrderLineParser.Parse("SUP-001,CMD-1001,10.00,urgent");

        Assert.Equal(OrderStatus.Urgent, result.Order?.Status);
    }
}
