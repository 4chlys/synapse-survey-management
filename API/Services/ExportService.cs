using System.IO.Compression;
using System.Text;
using ClosedXML.Excel;

namespace SynapseEnqueteManagement.API.Services;

public class ExportService : IExportService
{
    public byte[] BuildCsv(string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(",", headers.Select(EscapeCsv))).Append("\r\n");
        foreach (var row in rows)
            sb.Append(string.Join(",", row.Select(EscapeCsv))).Append("\r\n");

        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var output = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, output, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, output, preamble.Length, body.Length);
        return output;
    }

    public byte[] BuildTxt(IEnumerable<string> lines)
        => Encoding.UTF8.GetBytes(string.Join("\r\n", lines));

    public byte[] BuildXlsx(string[] headers, IEnumerable<string[]> rows, string worksheetName = "Sheet1")
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(worksheetName);

        for (var col = 0; col < headers.Length; col++)
        {
            ws.Cell(1, col + 1).Value = headers[col];
            ws.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var rowIndex = 2;
        foreach (var row in rows)
        {
            for (var col = 0; col < row.Length; col++)
                ws.Cell(rowIndex, col + 1).Value = row[col];
            rowIndex++;
        }

        for (var col = 1; col <= headers.Length; col++)
            ws.Column(col).AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] BuildXlsx(IEnumerable<(string SheetName, string[] Headers, IEnumerable<string[]> Rows)> sheets)
    {
        using var wb = new XLWorkbook();

        foreach (var (sheetName, headers, rows) in sheets)
        {
            var ws = wb.Worksheets.Add(sheetName);

            for (var col = 0; col < headers.Length; col++)
            {
                ws.Cell(1, col + 1).Value = headers[col];
                ws.Cell(1, col + 1).Style.Font.Bold = true;
            }

            var rowIndex = 2;
            foreach (var row in rows)
            {
                for (var col = 0; col < row.Length; col++)
                    ws.Cell(rowIndex, col + 1).Value = row[col];
                rowIndex++;
            }

            for (var col = 1; col <= headers.Length; col++)
                ws.Column(col).AdjustToContents();
        }

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] BuildZip(IEnumerable<(string FileName, string[] Headers, IEnumerable<string[]> Rows)> files)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            foreach (var (fileName, headers, rows) in files)
            {
                var entry = archive.CreateEntry(fileName, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                var csvBytes = BuildCsv(headers, rows);
                entryStream.Write(csvBytes, 0, csvBytes.Length);
            }
        }
        return ms.ToArray();
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
