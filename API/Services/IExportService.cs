namespace SynapseEnqueteManagement.API.Services;

public interface IExportService
{
    byte[] BuildCsv(string[] headers, IEnumerable<string[]> rows);
    byte[] BuildTxt(IEnumerable<string> lines);
    byte[] BuildXlsx(string[] headers, IEnumerable<string[]> rows, string worksheetName = "Sheet1");
    byte[] BuildXlsx(IEnumerable<(string SheetName, string[] Headers, IEnumerable<string[]> Rows)> sheets);
}
