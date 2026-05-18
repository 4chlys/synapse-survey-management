using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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
    private static readonly HashSet<string> AllTables =
        ["vragen", "antwoorden", "categorieen", "ideeen", "comments", "emojireacties"];

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
            ? AllTables
            : tables.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .Where(AllTables.Contains)
                .ToHashSet();

        if (selectedTables.Count == 0)
            selectedTables = AllTables;

        var questions = (await questionnaireManager.GetQuestionsForProjectAsync(projectId)).ToList();
        var responses = (await responseManager.GetCommittedResponsesForProjectReadOnlyAsync(projectId)).ToList();
        var categories = (await categoryManager.GetCategoriesWithIdeasForProjectReadOnlyAsync(projectId)).ToList();

        var projectName = project.Translations.FirstOrDefault()?.Name ?? project.ShortForm ?? $"project-{projectId}";
        var safeName = SanitizeFileName(projectName);
        var sections = new List<(string Key, string Title, string[] Headers, IEnumerable<string[]> Rows)>();

        if (selectedTables.Contains("vragen"))
        {
            sections.Add(("vragen", "Vragen",
                ["VraagId", "Tekst", "Type", "Verplicht", "Sectie", "Antwoordkeuzes"],
                questions.Select(q =>
                {
                    var qt = q.Translations?.FirstOrDefault();
                    var st = q.Section?.Translations?.FirstOrDefault();
                    return new[]
                    {
                        q.QuestionId.ToString(),
                        qt?.Text ?? "",
                        q.QuestionFormat.ToString(),
                        q.IsMandatory ? "Ja" : "Nee",
                        st?.Name ?? "",
                        qt?.AnswerChoices?.Count > 0 ? string.Join(" | ", qt.AnswerChoices) : ""
                    };
                })));
        }

        if (selectedTables.Contains("antwoorden"))
        {
            sections.Add(("antwoorden", "Antwoorden",
                ["AntwoordId", "VraagId", "GebruikerId", "Antwoord", "Tijdstip"],
                responses.Select(r => new[]
                {
                    r.ResponseId.ToString(),
                    r.Question.QuestionId.ToString(),
                    r.Participant?.ParticipantId.ToString() ?? "",
                    r.Value ?? "",
                    r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                })));
        }

        if (selectedTables.Contains("categorieen"))
        {
            sections.Add(("categorieen", "Categorieën",
                ["CategorieId", "Naam", "Beschrijving", "AantalIdeeën"],
                categories.Select(c =>
                {
                    var ct = c.Translations?.FirstOrDefault();
                    return new[]
                    {
                        c.CategoryId.ToString(),
                        ct?.Name ?? "",
                        ct?.Description ?? "",
                        c.Ideas.Count.ToString()
                    };
                })));
        }

        if (selectedTables.Contains("ideeen"))
        {
            sections.Add(("ideeen", "Ideeën",
                ["IdeeId", "Beschrijving", "Categorie", "Status", "AantalReacties", "AantalEmojis", "EmojiDetails", "Tijdstip"],
                categories.SelectMany(c => c.Ideas.Select(i =>
                {
                    var catName = c.Translations?.FirstOrDefault()?.Name ?? "";
                    var emojiReactions = i.Comments.OfType<IconComment>()
                        .Where(ic => ic.TextComment == null)
                        .ToList();
                    var emojiDetails = string.Join(" | ", emojiReactions
                        .GroupBy(ic => ic.Emoji?.Symbol ?? "?")
                        .Select(g => $"{g.Key} x{g.Count()}"));
                    return new[]
                    {
                        i.IdeaId.ToString(),
                        i.Description,
                        catName,
                        i.Status.ToString(),
                        i.Comments.OfType<TextComment>().Count().ToString(),
                        emojiReactions.Count.ToString(),
                        emojiDetails,
                        i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }))));
        }

        if (selectedTables.Contains("comments"))
        {
            sections.Add(("comments", "Comments",
                ["CommentId", "IdeeId", "GebruikerId", "Tekst", "Status", "EmojiReacties", "Tijdstip"],
                categories.SelectMany(c => c.Ideas.SelectMany(i =>
                    i.Comments.OfType<TextComment>().Select(tc =>
                    {
                        var emojiDetails = string.Join(" | ", tc.EmojiReactions
                            .GroupBy(ir => ir.Emoji?.Symbol ?? "?")
                            .Select(g => $"{g.Key} x{g.Count()}"));
                        return new[]
                        {
                            tc.CommentId.ToString(),
                            i.IdeaId.ToString(),
                            tc.Participant?.ParticipantId.ToString() ?? "",
                            tc.Text,
                            tc.ModerationStatus.ToString(),
                            emojiDetails,
                            tc.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                    })))));
        }

        if (selectedTables.Contains("emojireacties"))
        {
            sections.Add(("emojireacties", "EmojiReacties",
                ["ReactieId", "IdeeId", "CommentId", "GebruikerId", "Emoji", "EmojiNaam", "Tijdstip"],
                categories.SelectMany(c => c.Ideas.SelectMany(i =>
                    i.Comments.OfType<IconComment>().Select(ic => new[]
                    {
                        ic.CommentId.ToString(),
                        i.IdeaId.ToString(),
                        ic.TextComment?.CommentId.ToString() ?? "",
                        ic.Participant?.ParticipantId.ToString() ?? "",
                        ic.Emoji?.Symbol ?? "",
                        ic.Emoji?.Name ?? "",
                        ic.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    })))));
        }

        if (normalized == "xlsx")
        {
            var sheets = sections.Select(s => (s.Title, s.Headers, s.Rows));
            var bytes = exportService.BuildXlsx(sheets);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{safeName}-rawdata.xlsx");
        }
        else
        {
            if (sections.Count == 1)
            {
                var s = sections[0];
                var bytes = exportService.BuildCsv(s.Headers, s.Rows);
                return File(bytes, "text/csv", $"{safeName}-{s.Key}.csv");
            }

            var zipFiles = sections.Select(s => ($"{safeName}-{s.Key}.csv", s.Headers, s.Rows));
            var zipBytes = exportService.BuildZip(zipFiles);
            return File(zipBytes, "application/zip", $"{safeName}-rawdata.zip");
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        return sanitized.Trim().Replace(' ', '-').ToLowerInvariant();
    }
}
