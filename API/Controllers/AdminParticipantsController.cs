using System.Text;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/participants")]
public class AdminParticipantsController(
    IParticipantManager participantManager,
    IProjectManager projectManager) : ControllerBase
{
    [HttpGet("emails/export")]
    public async Task<IActionResult> ExportEmails(int projectId, [FromQuery] string format = "csv")
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var normalized = (format ?? "csv").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "txt" or "xlsx"))
            return BadRequest(new { error = $"Ongeldig formaat '{format}'." });

        var emails = await participantManager.GetParticipantEmailsByProjectAsync(projectId);

        var (bytes, contentType, fileName) = normalized switch
        {
            "csv"  => (BuildCsv(emails),  "text/csv", $"participants-{projectId}.csv"),
            "txt"  => (BuildTxt(emails),  "text/plain", $"participants-{projectId}.txt"),
            "xlsx" => (BuildXlsx(emails), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"participants-{projectId}.xlsx"),
            _ => throw new InvalidOperationException()
        };

        return File(bytes, contentType, fileName);
    }

    private static byte[] BuildCsv(IEnumerable<string> emails)
    {
        var sb = new StringBuilder();
        sb.Append("Email\r\n");
        foreach (var e in emails) sb.Append(EscapeCsv(e)).Append("\r\n");
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(sb.ToString());
        var output = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, output, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, output, preamble.Length, body.Length);
        return output;
    }

    private static string EscapeCsv(string value)
    {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static byte[] BuildTxt(IEnumerable<string> emails)
        => Encoding.UTF8.GetBytes(string.Join("\r\n", emails));

    private static byte[] BuildXlsx(IEnumerable<string> emails)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Participants");
        ws.Cell(1, 1).Value = "Email";
        ws.Cell(1, 1).Style.Font.Bold = true;
        var row = 2;
        foreach (var e in emails) ws.Cell(row++, 1).Value = e;
        ws.Column(1).AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
