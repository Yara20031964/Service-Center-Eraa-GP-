using CsvHelper;
using ClosedXML.Excel;
using KHDMA.Application.Interfaces;
using System.Globalization;

namespace KHDMA.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        public byte[] ExportToCsv<T>(IEnumerable<T> data)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
            writer.Flush();
            return stream.ToArray();
        }

        public byte[] ExportToExcel<T>(IEnumerable<T> data)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");
            worksheet.Cell(1, 1).InsertTable(data);
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
