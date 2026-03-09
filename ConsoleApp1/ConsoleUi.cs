using SynapseEnqueteManagement.BL;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
using SynapseEnqueteManagement.BL.Domain.Project.Enums;

namespace ConsoleApp1;

public class ConsoleUi
{
    private IManager _manager;
    public ConsoleUi(IManager manager)
    {
        _manager = manager;
    }

    public void Run()
    {
        Console.WriteLine("Welcome to the Synapse Enquete!");
        Console.WriteLine("================================\n");

        var questionnaire = _manager.GetFirstQuestionnaire();
        if (questionnaire == null)
        {
            Console.WriteLine("No questionnaire found.");
            return;
        }

        var participant = _manager.AddParticipant(questionnaire);

        var questions = _manager.GetQuestions()
            .Where(q => q.QuestionFormat == QuestionFormat.Open);

        foreach (var question in questions)
        {
            Console.WriteLine($"Question {question.Text} (open):");
            Console.Write("> ");
            var answer = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(answer))
            {
                _manager.AddResponse(answer, question, participant);
                Console.WriteLine("Answer saved!\n");
            }
            else
            {
                Console.WriteLine("Skipped.\n");
            }
        }

        var responses = _manager.GetAllResponses();
        foreach (Response response in responses) Console.WriteLine(response.Value);

        Console.WriteLine("Thank you for completing the enquete!");
    }
}