using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace OrderImport;

public class OrderImportService
{
    private static readonly List<Order> _importedOrders = new List<Order>();
    private const string NotificationApiKey = "<cle-retiree>";

    public void Import(string filePath)
    {
        Console.WriteLine($"Début de l'import : {filePath}");

        var lines = File.ReadAllLines(filePath);

        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            string supplierId = parts[0];
            string reference = parts[1];
            decimal amount = decimal.Parse(parts[2]);
            string status = parts[3];

            _importedOrders.Add(new Order
            {
                SupplierId = supplierId,
                Reference = reference,
                Amount = amount,
                Status = status
            });

            Console.WriteLine($"Commande {reference} ajoutée pour {supplierId}.");

            if (status == "URGENT")
            {
                Console.WriteLine($"Commande {reference} marquée URGENT, notification en cours...");
                NotifySupplier(supplierId, reference);
            }
        }

        Console.WriteLine($"{_importedOrders.Count} commande(s) importée(s).");
    }

    private void NotifySupplier(string supplierId, string reference)
    {
        var client = new HttpClient();
        var url = "https://api.notif-fournisseurs.fr/send?key=" + NotificationApiKey
            + "&supplier=" + supplierId + "&ref=" + reference;

        client.GetAsync(url);
    }
}
