using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;
using SynapseEnqueteManagement.DAL.Interfaces;
using SynapseEnqueteManagement.BL.Interfaces;
using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Implementation;

public class QuestionnaireManager(
    IQuestionnaireRepository questionnaireRepository,
    IQuestionRepository questionRepository,
    ISectionRepository sectionRepository,
    ILanguageRepository languageRepository,
    IUnitOfWork uow) : IQuestionnaireManager
{
    public Task<Questionnaire> GetQuestionnaireByProjectIdWithQuestionsReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByProjectIdReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByQuestionIdWithQuestionsReadOnlyAsync(int questionId)
        => questionnaireRepository.ReadByQuestionIdWithQuestionsReadOnlyAsync(questionId);

    public Task<Questionnaire> GetQuestionnaireByProjectIdWithTranslationsReadOnlyAsync(int projectId)
        => questionnaireRepository.ReadByProjectIdWithQuestionsAndTranslationsReadOnlyAsync(projectId);

    public Task<Questionnaire> GetQuestionnaireByQuestionIdWithTranslationsReadOnlyAsync(int questionId)
        => questionnaireRepository.ReadByQuestionIdWithQuestionsAndTranslationsReadOnlyAsync(questionId);

    public Task<Question> GetQuestionByIdReadOnlyAsync(int questionId)
        => questionRepository.ReadByIdReadOnlyAsync(questionId);

    public Task<bool> QuestionBelongsToProjectAsync(int questionId, int projectId)
        => questionRepository.ExistsForProjectAsync(questionId, projectId);

    public async Task<Question> AddQuestionAsync(
        int projectId, int answerChoiceCount, QuestionFormat format, bool isMandatory,
        IEnumerable<QuestionTranslationInput> translations,
        int? sectionId = null,
        int? parentQuestionId = null, int? choiceIndex = null,
        IEnumerable<IEnumerable<EndingAnswerTranslationInput>> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null, bool mediaIsEmbed = false,
        CancellationToken ct = default)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var translationList = translations?.ToList() ?? new List<QuestionTranslationInput>();
        var languages = await ValidateQuestionTranslationInputsAsync(translationList, answerChoiceCount);

        var section = sectionId.HasValue
            ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
            : null;

        Question parent = null;
        if (parentQuestionId.HasValue)
        {
            parent = await questionRepository.ReadByIdWithSectionForUpdateAsync(parentQuestionId.Value);
            if (parent is null)
                throw new ValidationException($"Parent question {parentQuestionId} not found.");
            ValidateParentChoiceIndex(parent, choiceIndex);
        }
        else if (choiceIndex.HasValue)
        {
            throw new ValidationException("ChoiceIndex must be null when no parent is set.");
        }

        var siblings = (await questionRepository.ReadSiblingsForUpdateAsync(
            parentQuestionId, parent is not null ? choiceIndex : null, questionnaire.QuestionnaireId)).ToList();
        var nextPosition = siblings.Count == 0 ? 1 : siblings.Max(s => s.Position) + 1;

        var question = new Question
        {
            QuestionFormat = format,
            IsMandatory = isMandatory,
            AnswerChoiceCount = answerChoiceCount,
            Questionnaire = questionnaire,
            Section = section,
            ParentQuestion = parent,
            ChoiceIndex = parent is not null ? choiceIndex : null,
            Position = nextPosition,
            MediaUrl = mediaUrl,
            MediaType = mediaType,
            MediaIsEmbed = mediaIsEmbed
        };

        foreach (var input in translationList)
        {
            question.Translations.Add(new QuestionTranslation
            {
                Language = languages[input.LanguageCode],
                Text = input.Text,
                AnswerChoices = input.AnswerChoices ?? new List<string>(),
                MediaText = input.MediaText
            });
        }

        if (endingAnswers is not null)
        {
            foreach (var eaInputs in endingAnswers)
            {
                var eaInputList = eaInputs?.ToList() ?? new List<EndingAnswerTranslationInput>();
                var ea = new EndingAnswer { Question = question };
                foreach (var eaInput in eaInputList)
                {
                    EntityValidator.ValidateProperty(new EndingAnswerTranslation(), eaInput.Value, nameof(EndingAnswerTranslation.Value));
                    ea.Translations.Add(new EndingAnswerTranslation
                    {
                        Language = languages[eaInput.LanguageCode],
                        Value = eaInput.Value
                    });
                }
                question.EndingAnswers.Add(ea);
            }
        }

        EntityValidator.Validate(question);
        var created = await questionRepository.CreateAsync(question);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<IEnumerable<Question>> GetQuestionsForProjectAsync(int projectId)
        => await questionRepository.ReadByProjectIdWithTranslationsReadOnlyAsync(projectId);

    public async Task<bool> UpdateQuestionAsync(
        int questionId, int answerChoiceCount, QuestionFormat format, bool isMandatory,
        IEnumerable<QuestionTranslationInput> translations,
        int? sectionId = null,
        IEnumerable<IEnumerable<EndingAnswerTranslationInput>> endingAnswers = null,
        string mediaUrl = null, QuestionMediaType? mediaType = null, bool mediaIsEmbed = false,
        CancellationToken ct = default)
    {
        var question = await questionRepository.ReadByIdWithSectionForUpdateAsync(questionId);
        if (question is null) return false;

        var translationList = translations?.ToList() ?? new List<QuestionTranslationInput>();
        var languages = await ValidateQuestionTranslationInputsAsync(translationList, answerChoiceCount);

        // Guard: cannot remove choices if children reference those indices
        if (answerChoiceCount < question.AnswerChoiceCount)
        {
            var invalidChildren = question.Children.Where(c => c.ChoiceIndex >= answerChoiceCount).ToList();
            if (invalidChildren.Count > 0)
                throw new ValidationException(
                    "Cannot reduce answer choice count: child questions reference choices that would be removed.");
        }

        question.QuestionFormat = format;
        question.IsMandatory = isMandatory;
        question.AnswerChoiceCount = answerChoiceCount;

        TranslationUpserter.Upsert(
            question.Translations,
            translationList,
            inputLanguageCode: i => i.LanguageCode,
            existingLanguageCode: t => t.Language.Code,
            create: input => new QuestionTranslation
            {
                Language = languages[input.LanguageCode],
                Text = input.Text,
                AnswerChoices = input.AnswerChoices ?? new List<string>(),
                MediaText = input.MediaText
            },
            update: (existing, input) =>
            {
                existing.Text = input.Text;
                existing.AnswerChoices = input.AnswerChoices ?? new List<string>();
                existing.MediaText = input.MediaText;
            });

        question.Section = sectionId.HasValue
            ? await sectionRepository.ReadByIdForUpdateAsync(sectionId.Value)
            : null;

        if (endingAnswers is not null)
        {
            question.EndingAnswers.Clear();
            foreach (var eaInputs in endingAnswers)
            {
                var eaInputList = eaInputs?.ToList() ?? new List<EndingAnswerTranslationInput>();
                var ea = new EndingAnswer { Question = question };
                foreach (var eaInput in eaInputList)
                {
                    EntityValidator.ValidateProperty(new EndingAnswerTranslation(), eaInput.Value, nameof(EndingAnswerTranslation.Value));
                    ea.Translations.Add(new EndingAnswerTranslation
                    {
                        Language = languages[eaInput.LanguageCode],
                        Value = eaInput.Value
                    });
                }
                question.EndingAnswers.Add(ea);
            }
        }

        question.MediaUrl = mediaUrl;
        question.MediaType = mediaType;
        question.MediaIsEmbed = mediaIsEmbed;

        questionRepository.Update(question);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> DeleteQuestionAsync(int questionId, CancellationToken ct = default)
        => uow.ExecuteInTransactionAsync(_ => questionRepository.DeleteByIdAsync(questionId), ct);

    public async Task<Question> MoveQuestionAsync(int questionId, int? newParentQuestionId,
        int? newChoiceIndex, int newPosition, CancellationToken ct = default)
    {
        var question = await questionRepository.ReadByIdWithParentChainForUpdateAsync(questionId);
        if (question is null) return null;

        Question newParent = null;
        if (newParentQuestionId.HasValue)
        {
            if (newParentQuestionId.Value == questionId)
                throw new ValidationException("A question cannot be its own parent.");

            newParent = await questionRepository.ReadByIdWithParentChainForUpdateAsync(newParentQuestionId.Value);
            if (newParent is null)
                throw new ValidationException($"Parent question {newParentQuestionId} not found.");
            if (newParent.Questionnaire.QuestionnaireId != question.Questionnaire.QuestionnaireId)
                throw new ValidationException("Parent must belong to the same questionnaire.");

            ValidateParentChoiceIndex(newParent, newChoiceIndex);
            EnsureNoCycle(question, newParent);
        }
        else if (newChoiceIndex.HasValue)
        {
            throw new ValidationException("ChoiceIndex must be null when moving to top-level.");
        }

        var oldParentId = question.ParentQuestion?.QuestionId;
        var oldChoiceIndex = question.ChoiceIndex;
        var questionnaireId = question.Questionnaire.QuestionnaireId;

        question.ParentQuestion = newParent;
        question.ChoiceIndex = newParent is null ? null : newChoiceIndex;
        questionRepository.Update(question);

        await RepackScopeAsync(questionnaireId, oldParentId, oldChoiceIndex, excludeId: questionId);
        await RepackScopeAsync(questionnaireId, newParentQuestionId, newParent is null ? null : newChoiceIndex,
            insertId: questionId, insertAt: newPosition);

        await uow.SaveChangesAsync(ct);
        return question;
    }

    public async Task<Question> GetFirstQuestionAsync(int projectId)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);
        if (questionnaire is null) return null;

        return questionnaire.Questions
            .Where(q => q.ParentQuestion is null)
            .OrderBy(q => q.Position)
            .FirstOrDefault();
    }

    public async Task<int> GetQuestionCountAsync(int projectId)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdWithQuestionsReadOnlyAsync(projectId);
        return questionnaire?.Questions.Count ?? 0;
    }

    public void ValidateMandatoryAnswers(Questionnaire questionnaire,
        IEnumerable<(int QuestionId, string Value)> answers,
        HashSet<int> visibleQuestionIds = null)
    {
        var answeredIds = answers
            .Where(a => !string.IsNullOrWhiteSpace(a.Value))
            .Select(a => a.QuestionId)
            .ToHashSet();

        var questionsToValidate = questionnaire.Questions
            .Where(q => q.IsMandatory);

        if (visibleQuestionIds is not null)
            questionsToValidate = questionsToValidate.Where(q => visibleQuestionIds.Contains(q.QuestionId));

        var missing = questionsToValidate
            .Where(q => !answeredIds.Contains(q.QuestionId))
            .Select(q => q.QuestionId)
            .ToList();

        if (missing.Count > 0)
            throw new ValidationException($"Missing answers for mandatory questions: {string.Join(", ", missing)}");
    }

    public Task<IEnumerable<Section>> GetSectionsReadOnlyAsync(int projectId)
        => sectionRepository.ReadByProjectIdReadOnlyAsync(projectId);

    public async Task<Section> CreateSectionAsync(int projectId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default)
    {
        var questionnaire = await questionnaireRepository.ReadByProjectIdForUpdateAsync(projectId);
        if (questionnaire is null) return null;

        var translationList = translations?.ToList() ?? new List<SectionTranslationInput>();
        var languages = await ValidateSectionTranslationInputsAsync(translationList);

        var section = new Section { Questionnaire = questionnaire };
        foreach (var input in translationList)
        {
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Name, nameof(SectionTranslation.Name));
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Description, nameof(SectionTranslation.Description));
            section.Translations.Add(new SectionTranslation
            {
                Language = languages[input.LanguageCode],
                Name = input.Name,
                Description = input.Description
            });
        }

        EntityValidator.Validate(section);
        var created = await sectionRepository.CreateAsync(section);
        await uow.SaveChangesAsync(ct);
        return created;
    }

    public async Task<bool> UpdateSectionAsync(int sectionId, IEnumerable<SectionTranslationInput> translations, CancellationToken ct = default)
    {
        var section = await sectionRepository.ReadByIdForUpdateAsync(sectionId);
        if (section is null) return false;

        var translationList = translations?.ToList() ?? new List<SectionTranslationInput>();
        var languages = await ValidateSectionTranslationInputsAsync(translationList);

        void Validate(SectionTranslationInput input)
        {
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Name, nameof(SectionTranslation.Name));
            EntityValidator.ValidateProperty(new SectionTranslation(), input.Description, nameof(SectionTranslation.Description));
        }

        TranslationUpserter.Upsert(
            section.Translations,
            translationList,
            inputLanguageCode: i => i.LanguageCode,
            existingLanguageCode: t => t.Language.Code,
            create: input =>
            {
                Validate(input);
                return new SectionTranslation
                {
                    Language = languages[input.LanguageCode],
                    Name = input.Name,
                    Description = input.Description
                };
            },
            update: (existing, input) =>
            {
                Validate(input);
                existing.Name = input.Name;
                existing.Description = input.Description;
            });

        sectionRepository.Update(section);
        await uow.SaveChangesAsync(ct);
        return true;
    }

    public Task<bool> DeleteSectionAsync(int sectionId, CancellationToken ct = default)
        => uow.ExecuteInTransactionAsync(_ => sectionRepository.DeleteByIdAsync(sectionId), ct);

    private static void ValidateParentChoiceIndex(Question parent, int? choiceIndex)
    {
        if (parent.QuestionFormat == QuestionFormat.Open)
            throw new ValidationException("Open questions cannot have sub-questions.");
        if (!choiceIndex.HasValue)
            throw new ValidationException("ChoiceIndex is required when nesting under a parent.");

        if (parent.QuestionFormat == QuestionFormat.SingleChoice
            || parent.QuestionFormat == QuestionFormat.MultipleChoice)
        {
            if (choiceIndex < 0 || choiceIndex >= parent.AnswerChoiceCount)
                throw new ValidationException(
                    $"ChoiceIndex {choiceIndex} is out of range for the parent (0..{parent.AnswerChoiceCount - 1}).");
        }
        // Likert: accept any non-negative int — the UI presents 1..N and bounds are not stored on Question.
    }

    private static void EnsureNoCycle(Question moving, Question newParent)
    {
        var cursor = newParent;
        while (cursor is not null)
        {
            if (cursor.QuestionId == moving.QuestionId)
                throw new ValidationException("Move would create a cycle.");
            cursor = cursor.ParentQuestion;
        }
    }

    private async Task RepackScopeAsync(int questionnaireId, int? parentQuestionId, int? choiceIndex,
        int? excludeId = null, int? insertId = null, int? insertAt = null)
    {
        var siblings = (await questionRepository.ReadSiblingsForUpdateAsync(
            parentQuestionId, choiceIndex, questionnaireId)).ToList();

        if (excludeId.HasValue)
            siblings.RemoveAll(s => s.QuestionId == excludeId.Value);

        if (insertId.HasValue)
        {
            siblings.RemoveAll(s => s.QuestionId == insertId.Value);
            var target = await questionRepository.ReadByIdWithSectionForUpdateAsync(insertId.Value);
            if (target is not null)
            {
                var clamped = Math.Clamp(insertAt ?? siblings.Count, 0, siblings.Count);
                siblings.Insert(clamped, target);
            }
        }

        for (var i = 0; i < siblings.Count; i++)
        {
            var desired = i + 1;
            if (siblings[i].Position != desired)
            {
                siblings[i].Position = desired;
                questionRepository.Update(siblings[i]);
            }
        }
    }

    private async Task<Dictionary<string, Language>> ValidateQuestionTranslationInputsAsync(
        ICollection<QuestionTranslationInput> translations, int answerChoiceCount)
    {
        if (translations.Count == 0)
            throw new ValidationException("At least one translation is required.");

        var codes = translations.Select(t => t.LanguageCode).ToList();
        if (codes.Distinct().Count() != codes.Count)
            throw new ValidationException("Translations must contain a single row per language.");

        foreach (var t in translations)
        {
            var count = t.AnswerChoices?.Count ?? 0;
            if (count != answerChoiceCount)
                throw new ValidationException(
                    $"Translation '{t.LanguageCode}' has {count} answer choices but AnswerChoiceCount is {answerChoiceCount}.");
        }

        var resolved = (await languageRepository.ReadByCodesForUpdateAsync(codes)).ToList();
        var byCode = resolved.ToDictionary(l => l.Code);
        var missing = codes.Where(c => !byCode.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new ValidationException($"Unknown language code(s): {string.Join(", ", missing)}.");

        return byCode;
    }

    private async Task<Dictionary<string, Language>> ValidateSectionTranslationInputsAsync(
        ICollection<SectionTranslationInput> translations)
    {
        if (translations.Count == 0)
            throw new ValidationException("At least one translation is required.");

        var codes = translations.Select(t => t.LanguageCode).ToList();
        if (codes.Distinct().Count() != codes.Count)
            throw new ValidationException("Translations must contain a single row per language.");

        var resolved = (await languageRepository.ReadByCodesForUpdateAsync(codes)).ToList();
        var byCode = resolved.ToDictionary(l => l.Code);
        var missing = codes.Where(c => !byCode.ContainsKey(c)).ToList();
        if (missing.Count > 0)
            throw new ValidationException($"Unknown language code(s): {string.Join(", ", missing)}.");

        return byCode;
    }
}
