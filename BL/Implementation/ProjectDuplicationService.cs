using SynapseEnqueteManagement.BL.Domain.I18n.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

// Owns the single responsibility of deep-copying a project's object graph. Reads emojis through
// IEmojiManager so the links attach as tracked entities in the caller's unit of work.
public class ProjectDuplicationService(IEmojiManager emojiManager) : IProjectDuplicationService
{
    public async Task<Project> BuildCloneAsync(
        Project src,
        IReadOnlyList<ProjectTranslationInput> translations,
        IReadOnlyDictionary<string, Language> languages,
        string shortForm,
        int platformId)
    {
        var supportedCodes = src.SupportedLanguages.Select(l => l.Code).ToList();
        var defaultCode = src.DefaultLanguage?.Code
            ?? throw new InvalidOperationException("Source project has no default language.");

        var copy = new Project
        {
            ShortForm = shortForm,
            ImageUrl = src.ImageUrl,
            FormType = src.FormType,
            MinAge = src.MinAge,
            MaxAge = src.MaxAge,
            RequestContactInfo = src.RequestContactInfo,
            AllowCrossLanguageIdeas = src.AllowCrossLanguageIdeas,
            IdeaPageSize = src.IdeaPageSize,
            ExtraIdeasReplaceExisting = src.ExtraIdeasReplaceExisting,
            MaxExtraIdeaRequests = src.MaxExtraIdeaRequests,
            CommentPageSize = src.CommentPageSize,
            ProjectPlatformId = platformId,
            ProjectState = ProjectState.Draft,
            CreatedAt = DateTime.UtcNow,
            ViewCount = 0,
            DefaultLanguage = languages[defaultCode],
            Theme = src.Theme,
            PrimaryColor = src.PrimaryColor,
            CustomCss = src.CustomCss,
        };

        foreach (var languageCode in supportedCodes)
            copy.SupportedLanguages.Add(languages[languageCode]);

        foreach (var t in translations)
            copy.Translations.Add(new ProjectTranslation
            {
                Language = languages[t.LanguageCode],
                Name = t.Name,
                Topic = t.Topic,
            });

        if (src.Introduction is not null && src.Introduction.Translations.Count > 0)
        {
            var introCopy = new Introduction();
            foreach (var t in src.Introduction.Translations)
                introCopy.Translations.Add(new IntroductionTranslation { Language = languages[t.Language.Code], Text = t.Text });
            copy.Introduction = introCopy;
        }

        copy.Questionnaire = new Questionnaire();

        if (src.Questionnaire is not null)
        {
            var sectionMap = new Dictionary<int, Section>();
            var questionMap = new Dictionary<int, Question>();

            foreach (var srcSection in src.Questionnaire.Sections)
            {
                var newSection = new Section();
                foreach (var t in srcSection.Translations)
                    newSection.Translations.Add(new SectionTranslation { Language = languages[t.Language.Code], Name = t.Name, Description = t.Description });
                sectionMap[srcSection.SectionId] = newSection;
                copy.Questionnaire.Sections.Add(newSection);
            }

            foreach (var srcQ in src.Questionnaire.Questions)
            {
                var newQ = new Question
                {
                    QuestionFormat = srcQ.QuestionFormat,
                    IsMandatory = srcQ.IsMandatory,
                    AnswerChoiceCount = srcQ.AnswerChoiceCount,
                    ChoiceIndex = srcQ.ChoiceIndex,
                    Position = srcQ.Position,
                    Section = srcQ.Section is not null ? sectionMap[srcQ.Section.SectionId] : null,
                };
                foreach (var t in srcQ.Translations)
                    newQ.Translations.Add(new QuestionTranslation { Language = languages[t.Language.Code], Text = t.Text, AnswerChoices = t.AnswerChoices.ToList(), MediaText = t.MediaText });
                foreach (var ea in srcQ.EndingAnswers)
                {
                    var newEa = new EndingAnswer { Question = newQ };
                    foreach (var t in ea.Translations)
                        newEa.Translations.Add(new EndingAnswerTranslation { Language = languages[t.Language.Code], Value = t.Value });
                    newQ.EndingAnswers.Add(newEa);
                }
                questionMap[srcQ.QuestionId] = newQ;
                copy.Questionnaire.Questions.Add(newQ);
            }

            foreach (var srcQ in src.Questionnaire.Questions)
            {
                if (srcQ.ParentQuestion is not null && questionMap.TryGetValue(srcQ.ParentQuestion.QuestionId, out var newParent))
                    questionMap[srcQ.QuestionId].ParentQuestion = newParent;
            }
        }

        foreach (var srcCat in src.Categories)
        {
            var catCopy = new Category();
            foreach (var t in srcCat.Translations)
                catCopy.Translations.Add(new CategoryTranslation { Language = languages[t.Language.Code], Name = t.Name, Description = t.Description });
            copy.Categories.Add(catCopy);
        }

        foreach (var srcEmoji in src.Emojis)
        {
            var tracked = await emojiManager.GetByIdForUpdateAsync(srcEmoji.EmojiId);
            if (tracked is not null)
                copy.Emojis.Add(tracked);
        }

        return copy;
    }
}
