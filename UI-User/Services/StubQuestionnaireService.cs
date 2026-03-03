using SynapseEnqueteManagement.UI.User.Models;

namespace SynapseEnqueteManagement.UI.User.Services;

public class StubQuestionnaireService : IQuestionnaireService
{
    public SurveyViewModel GetSurvey()
    {
        var questions = new List<QuestionViewModel>
        {
            new QuestionViewModel
            {
                QuestionId = 1,
                Text = "Welke factoren zorgen bij jou de meeste stress?",
                Format = QuestionFormat.MultipleChoice,
                AnswerChoices = new List<string> { "Examens", "Weinig vrije tijd", "Deadlines", "Sociale druk", "Thuissituatie" },
                QuestionNumber = 1
            },
            new QuestionViewModel
            {
                QuestionId = 2,
                Text = "Hoe ervaar jij je mentaal welzijn op school de afgelopen maand?",
                Format = QuestionFormat.SingleChoice,
                AnswerChoices = new List<string> { "Goed", "Neutraal", "Slecht" },
                QuestionNumber = 2
            },
            new QuestionViewModel
            {
                QuestionId = 3,
                Text = "Op een schaal van 1 tot 10, hoeveel schoolstress ervaar jij?",
                Format = QuestionFormat.Likert,
                AnswerChoices = new List<string>(),
                QuestionNumber = 3
            },
            new QuestionViewModel
            {
                QuestionId = 4,
                Text = "Wat zou de school volgens jou concreet kunnen doen om het mentaal welzijn van leerlingen te verbeteren?",
                Format = QuestionFormat.Open,
                AnswerChoices = new List<string>(),
                QuestionNumber = 4
            }
        };

        int total = questions.Count;
        foreach (var q in questions)
            q.TotalQuestions = total;

        return new SurveyViewModel
        {
            Title = "Mentaal welzijn",
            Questions = questions
        };
    }
}
