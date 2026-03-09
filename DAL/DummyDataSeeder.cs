using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL;

public class DummyDataSeeder
{
    public static void Seed(SynapseEnqueteContext context)
    {
        if (context.Projects.Any()) return;

        var questionnaire = new Questionnaire();

        var question1 = new Question
        {
            QuestionFormat = QuestionFormat.MultipleChoice,
            Questionnaire = questionnaire,
            Text = "Welke factoren zorgen bij jou de meeste stress?",
            AnswerChoices = new List<string> { "Examens", "Weinig vrije tijd", "Deadlines", "Sociale druk", "Thuissituatie" },
            IsMandatory = true
        };

        var question2 = new Question
        {
            QuestionFormat = QuestionFormat.SingleChoice,
            Questionnaire = questionnaire,
            Text = "Hoe ervaar jij je mentaal welzijn op school de afgelopen maand?",
            AnswerChoices = new List<string> { "Goed", "Neutraal", "Slecht" },
            IsMandatory = true
        };

        var question3 = new Question
        {
            QuestionFormat = QuestionFormat.Likert,
            Questionnaire = questionnaire,
            Text = "Op een schaal van 1 tot 10, hoeveel schoolstress ervaar jij?",
            IsMandatory = true
        };

        var question4 = new Question
        {
            QuestionFormat = QuestionFormat.Open,
            Questionnaire = questionnaire,
            Text = "Wat zou de school volgens jou concreet kunnen doen om het mentaal welzijn van leerlingen te verbeteren?",
            IsMandatory = true
        };

        questionnaire.Questions.Add(question1);
        questionnaire.Questions.Add(question2);
        questionnaire.Questions.Add(question3);
        questionnaire.Questions.Add(question4);

        var project = new Project
        {
            Questionnaire = questionnaire
        };

        context.Projects.Add(project);

        context.SaveChanges();
    }
}