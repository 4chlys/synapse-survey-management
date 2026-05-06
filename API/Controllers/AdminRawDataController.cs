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
    [HttpGet("export")]
    public async Task<IActionResult> ExportRawData(int projectId, [FromQuery] string format = "xlsx")
    {
        if (await projectManager.GetProjectByIdReadOnlyAsync(projectId) is null)
            return NotFound();

        var normalized = (format ?? "xlsx").Trim().ToLowerInvariant();
        if (normalized is not ("csv" or "xlsx"))
            return BadRequest(new { error = $"Ongeldig formaat '{format}'." });

        var questionsTask = questionnaireManager.GetQuestionsForProjectAsync(projectId);
        var responsesTask = responseManager.GetCommittedResponsesForProjectReadOnlyAsync(projectId);
        var categoriesTask = categoryManager.GetCategoriesWithIdeasForProjectReadOnlyAsync(projectId);

        await Task.WhenAll(questionsTask, responsesTask, categoriesTask);

        var questions = (await questionsTask).ToList();
        var responses = (await responsesTask).ToList();
        var categories = (await categoriesTask).ToList();

        var questionHeaders = new[] { "VraagId", "Tekst", "Type", "Verplicht", "Sectie", "Antwoordkeuzes" };
        var questionRows = questions.Select(q => new[]
        {
            q.QuestionId.ToString(),
            q.Text,
            q.QuestionFormat.ToString(),
            q.IsMandatory ? "Ja" : "Nee",
            q.Section?.Name ?? "",
            q.AnswerChoices?.Count > 0 ? string.Join(" | ", q.AnswerChoices) : ""
        });

        var responseHeaders = new[] { "AntwoordId", "VraagId", "Antwoord", "Tijdstip" };
        var responseRows = responses.Select(r => new[]
        {
            r.ResponseId.ToString(),
            r.Question.QuestionId.ToString(),
            r.Value ?? "",
            r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        });

        var categoryHeaders = new[] { "CategorieId", "Naam", "Beschrijving", "AantalIdeeën" };
        var categoryRows = categories.Select(c => new[]
        {
            c.CategoryId.ToString(),
            c.Name,
            c.Description ?? "",
            c.Ideas.Count.ToString()
        });

        var ideaHeaders = new[] { "IdeeId", "Beschrijving", "Categorie", "Status", "AantalReacties", "AantalEmojis", "Tijdstip" };
        var ideaRows = categories.SelectMany(c => c.Ideas.Select(i => new[]
        {
            i.IdeaId.ToString(),
            i.Description,
            c.Name,
            i.Status.ToString(),
            i.Comments.OfType<TextComment>().Count().ToString(),
            i.Comments.OfType<IconComment>().Count(ic => ic.TextComment == null).ToString(),
            i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
        }));

        var commentHeaders = new[] { "CommentId", "IdeeId", "Tekst", "Status", "Tijdstip" };
        var commentRows = categories.SelectMany(c => c.Ideas.SelectMany(i =>
            i.Comments.OfType<TextComment>().Select(tc => new[]
            {
                tc.CommentId.ToString(),
                i.IdeaId.ToString(),
                tc.Text,
                tc.ModerationStatus.ToString(),
                tc.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            })));

        if (normalized == "xlsx")
        {
            var sheets = new (string, string[], IEnumerable<string[]>)[]
            {
                ("Vragen", questionHeaders, questionRows),
                ("Antwoorden", responseHeaders, responseRows),
                ("Categorieën", categoryHeaders, categoryRows),
                ("Ideeën", ideaHeaders, ideaRows),
                ("Comments", commentHeaders, commentRows)
            };
            var bytes = exportService.BuildXlsx(sheets);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"rawdata-{projectId}.xlsx");
        }
        else
        {
            var sections = new (string Title, string[] Headers, IEnumerable<string[]> Rows)[]
            {
                ("Vragen", questionHeaders, questionRows),
                ("Antwoorden", responseHeaders, responseRows),
                ("Categorieën", categoryHeaders, categoryRows),
                ("Ideeën", ideaHeaders, ideaRows),
                ("Comments", commentHeaders, commentRows)
            };

            var sb = new System.Text.StringBuilder();
            foreach (var (title, headers, rows) in sections)
            {
                sb.AppendLine(string.Join(";", headers));
                foreach (var row in rows)
                    sb.AppendLine(string.Join(";", row.Select(EscapeCsvField)));
                sb.AppendLine();
            }

            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var body = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var bytes = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, bytes, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, bytes, preamble.Length, body.Length);
            return File(bytes, "text/csv", $"rawdata-{projectId}.csv");
        }
    }

    private static string EscapeCsvField(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.IndexOfAny([';', '"', '\r', '\n']) < 0) return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    } 
}

