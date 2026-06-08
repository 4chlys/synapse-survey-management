using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.API.Services.Export;

public static class RawDataExportSheetBuilder
{
    public static readonly IReadOnlySet<string> AllTables =
        new HashSet<string> { "vragen", "antwoorden", "categorieen", "ideeen", "comments", "emojireacties" };

    public static IReadOnlyList<ExportSheet> Build(
        IReadOnlyList<Question> questions,
        IReadOnlyList<Response> responses,
        IReadOnlyList<Category> categories,
        IReadOnlySet<string> tables,
        string defaultLanguageCode)
    {
        var sheets = new List<ExportSheet>();

        if (tables.Contains("vragen"))
            sheets.Add(new ExportSheet("vragen", "Questions",
                ["QuestionId", "Text", "Type", "Mandatory", "Section", "AnswerChoices"],
                questions.Select(q =>
                {
                    var qt = TranslationPicker.Pick(q.Translations, defaultLanguageCode);
                    var st = TranslationPicker.Pick(q.Section?.Translations, defaultLanguageCode);
                    return new[]
                    {
                        q.QuestionId.ToString(),
                        qt?.Text ?? "",
                        q.QuestionFormat.ToString(),
                        q.IsMandatory ? "Yes" : "No",
                        st?.Name ?? "",
                        qt?.AnswerChoices?.Count > 0 ? string.Join(" | ", qt.AnswerChoices) : ""
                    };
                })));

        if (tables.Contains("antwoorden"))
            sheets.Add(new ExportSheet("antwoorden", "Answers",
                ["AnswerId", "QuestionId", "UserId", "Answer", "Timestamp"],
                responses.Select(r => new[]
                {
                    r.ResponseId.ToString(),
                    r.Question.QuestionId.ToString(),
                    r.Participant?.ParticipantId.ToString() ?? "",
                    r.Value ?? "",
                    r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                })));

        if (tables.Contains("categorieen"))
            sheets.Add(new ExportSheet("categorieen", "Categories",
                ["CategoryId", "Name", "Description", "IdeaCount"],
                categories.Select(c =>
                {
                    var ct = TranslationPicker.Pick(c.Translations, defaultLanguageCode);
                    return new[]
                    {
                        c.CategoryId.ToString(),
                        ct?.Name ?? "",
                        ct?.Description ?? "",
                        c.Ideas.Count.ToString()
                    };
                })));

        if (tables.Contains("ideeen"))
            sheets.Add(new ExportSheet("ideeen", "Ideas",
                ["IdeaId", "Description", "Category", "Status", "Email", "CommentCount", "EmojiCount", "EmojiDetails", "Timestamp"],
                categories.SelectMany(c => c.Ideas.Select(i =>
                {
                    var catName = TranslationPicker.Pick(c.Translations, defaultLanguageCode)?.Name ?? "";
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
                        i.Participant?.Email ?? "",
                        i.Comments.OfType<TextComment>().Count().ToString(),
                        emojiReactions.Count.ToString(),
                        emojiDetails,
                        i.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }))));

        if (tables.Contains("comments"))
            sheets.Add(new ExportSheet("comments", "Comments",
                ["CommentId", "IdeaId", "UserId", "Text", "Status", "EmojiReactions", "Timestamp"],
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

        if (tables.Contains("emojireacties"))
            sheets.Add(new ExportSheet("emojireacties", "EmojiReactions",
                ["ReactionId", "IdeaId", "CommentId", "UserId", "Emoji", "EmojiName", "Timestamp"],
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

        return sheets;
    }
}
