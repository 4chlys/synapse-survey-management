using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.API.Services.Export;
using SynapseEnqueteManagement.BL.Helpers;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/projects/{projectId:int}/rawdata")]
public class AdminRawDataController(
    IProjectManager projectManager,
    IQuestionnaireManager questionnaireManager,
    IResponseManager responseManager,
    ICategoryManager categoryManager,
    IExportService exportService) : ControllerBase
{
    [HttpGet("export")]
    public async Task<IActionResult> ExportRawData(
        int projectId,
        [FromQuery] string format = "xlsx",
        [FromQuery] string tables = "")
    {
        var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
        if (project is null)
            return NotFound();

        var normalized = (format ?? "xlsx").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "xlsx"))
            return BadRequest(new { error = $"Ongeldig formaat '{format}'." });

        var selectedTables = string.IsNullOrWhiteSpace(tables)
            ? RawDataExportSheetBuilder.AllTables
            : tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .Where(RawDataExportSheetBuilder.AllTables.Contains)
                .ToHashSet();

        if (selectedTables.Count == 0)
            selectedTables = RawDataExportSheetBuilder.AllTables;

        var questions = (await questionnaireManager.GetQuestionsForProjectAsync(projectId)).ToList();
        var responses = (await responseManager.GetCommittedResponsesForProjectReadOnlyAsync(projectId)).ToList();
        var categories = (await categoryManager.GetCategoriesWithIdeasAndParticipantsForProjectReadOnlyAsync(projectId)).ToList();

        // Export labels land on the project's default language so a download is reproducible —
        // otherwise FirstOrDefault() picked whichever translation EF happened to load first.
        var exportLanguage = project.DefaultLanguage?.Code;
        var projectName = TranslationPicker.Pick(project.Translations, exportLanguage)?.Name
                          ?? project.ShortForm ?? $"project-{projectId}";
        var safeName = FileNameSanitizer.Sanitize(projectName);
        var sheets = RawDataExportSheetBuilder.Build(questions, responses, categories, selectedTables, exportLanguage);

        if (normalized == "xlsx")
        {
            var bytes = exportService.BuildXlsx(sheets.Select(s => (s.Name, s.Headers, s.Rows)));
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{safeName}-rawdata.xlsx");
        }

        if (sheets.Count == 1)
        {
            var s = sheets[0];
            var bytes = exportService.BuildCsv(s.Headers, s.Rows);
            return File(bytes, "text/csv", $"{safeName}-{s.Key}.csv");
        }

        var zipFiles = sheets.Select(s => ($"{safeName}-{s.Key}.csv", s.Headers, s.Rows));
        var zipBytes = exportService.BuildZip(zipFiles);
        return File(zipBytes, "application/zip", $"{safeName}-rawdata.zip");
    }
}
