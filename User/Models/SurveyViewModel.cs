namespace User.Models;

public class SurveyViewModel
{
    public string Title { get; set; }
    public List<QuestionViewModel> Questions { get; set; }
}

public class QuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; }
    public QuestionFormat Format { get; set; }
    public List<string> AnswerChoices { get; set; }
    public int QuestionNumber { get; set; }
    public int TotalQuestions { get; set; }
}
