namespace KHDMA.Application.Interfaces
{
    public interface IExportService
    {
        byte[] ExportToCsv<T>(IEnumerable<T> data);
        byte[] ExportToExcel<T>(IEnumerable<T> data);
    }
}
