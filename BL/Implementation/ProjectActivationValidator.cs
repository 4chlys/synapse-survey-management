using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.BL.Implementation;

public class ProjectActivationValidator : IProjectActivationValidator
{
    public IReadOnlyList<ActivationProblem> Validate(Project project)
    {
        var problems = new List<ActivationProblem>();
        var supportedCodes = project.SupportedLanguages.Select(l => l.Code).ToList();

        foreach (var code in supportedCodes)
        {
            if (!project.Translations.Any(t => t.Language?.Code == code))
                problems.Add(new ActivationProblem("ProjectTranslation", null, code));

            if (project.Introduction is not null
                && !project.Introduction.Translations.Any(t => t.Language?.Code == code))
                problems.Add(new ActivationProblem("IntroductionTranslation", null, code));

            if (project.Questionnaire is not null)
            {
                foreach (var section in project.Questionnaire.Sections)
                {
                    if (!section.Translations.Any(t => t.Language?.Code == code))
                        problems.Add(new ActivationProblem("SectionTranslation", section.SectionId, code));
                }

                foreach (var question in project.Questionnaire.Questions)
                {
                    if (!question.Translations.Any(t => t.Language?.Code == code))
                        problems.Add(new ActivationProblem("QuestionTranslation", question.QuestionId, code));

                    foreach (var endingAnswer in question.EndingAnswers)
                    {
                        if (!endingAnswer.Translations.Any(t => t.Language?.Code == code))
                            problems.Add(new ActivationProblem("EndingAnswerTranslation", endingAnswer.EndingAnswerId, code));
                    }
                }
            }

            foreach (var category in project.Categories)
            {
                if (!category.Translations.Any(t => t.Language?.Code == code))
                    problems.Add(new ActivationProblem("CategoryTranslation", category.CategoryId, code));
            }
        }

        if (!project.Categories.Any())
            problems.Add(new ActivationProblem("NoCategory", null, null));

        return problems;
    }
}
