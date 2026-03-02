using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace SynapseEnqueteManagement.DAL;

public class DummyDataSeeder
{
    public static void Seed(SynapseEnqueteContext context)
    {
        if (context.Projects.Any()) return;

        var questionnaire = new Questionnaire();
        
        var question1 = new Question{
            QuestionFormat = QuestionFormat.Open, 
            Questionnaire = questionnaire,
            Text = "How are you feeling today"
        };
    

        questionnaire.Questions.Add(question1);

        
        context.Questionnaires.Add(questionnaire);
        
        context.SaveChanges();
    }
}