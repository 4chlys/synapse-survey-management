using User.Models;

namespace User.Services;

public interface IQuestionnaireService
{
    SurveyViewModel GetSurvey();
}
