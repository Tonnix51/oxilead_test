using System;
using System.IO;

namespace OrderImport;

class Program
{
    static void Main(string[] args)
    {
        var filePath = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "sample-orders.csv");

        var service = new OrderImportService();
        service.Import(filePath);
    }
}
