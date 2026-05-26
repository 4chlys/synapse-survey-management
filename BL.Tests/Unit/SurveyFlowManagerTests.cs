namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class SurveyFlowManagerTests
{
    private readonly Mock<IQuestionnaireManager> _questionnaireManager = new(MockBehavior.Strict);
    private readonly Mock<IResponseManager> _responseManager = new(MockBehavior.Strict);
    private readonly SurveyFlowManager _manager;

    private const int ProjectId = 1;
    private const int ParticipantId = 7;

    private static readonly Language En = new() { Code = "en", DisplayName = "English", IsActive = true };
    private static readonly Language Nl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };

    public SurveyFlowManagerTests()
    {
        _manager = new SurveyFlowManager(_questionnaireManager.Object, _responseManager.Object);
    }

    private static Question TopLevel(int id, QuestionFormat format, int position,
        params (Language lang, string[] choices)[] translations)
        => new()
        {
            QuestionId = id,
            QuestionFormat = format,
            Position = position,
            ParentQuestion = null,
            ChoiceIndex = null,
            Translations = translations
                .Select(t => new QuestionTranslation { Language = t.lang, AnswerChoices = t.choices.ToList() })
                .ToList()
        };

    private static Question Child(int id, Question parent, int choiceIndex, int position, QuestionFormat format = QuestionFormat.Open)
        => new()
        {
            QuestionId = id,
            QuestionFormat = format,
            Position = position,
            ParentQuestion = parent,
            ChoiceIndex = choiceIndex,
            Translations = new List<QuestionTranslation> { new() { Language = En, AnswerChoices = new List<string>() } }
        };

    private static Response Answer(Question q, string value, Language sourceLanguage)
        => new() { Question = q, Value = value, Language = sourceLanguage };

    private static Questionnaire QuestionnaireOf(params Question[] questions)
        => new() { QuestionnaireId = 99, Questions = questions.ToList() };

    private void SetupResponses(params Response[] responses)
        => _responseManager.Setup(r => r.GetResponsesForParticipantReadOnlyAsync(ParticipantId))
            .ReturnsAsync(responses);

    private void SetupQuestionnaireByProject(Questionnaire questionnaire)
        => _questionnaireManager.Setup(m => m.GetQuestionnaireByProjectIdWithTranslationsReadOnlyAsync(ProjectId))
            .ReturnsAsync(questionnaire);

    private void SetupQuestionnaireByQuestion(int questionId, Questionnaire questionnaire)
        => _questionnaireManager.Setup(m => m.GetQuestionnaireByQuestionIdWithTranslationsReadOnlyAsync(questionId))
            .ReturnsAsync(questionnaire);

    [Fact]
    public async Task ComputeVisiblePathAsync_NoBranching_ReturnsTopLevelInPositionOrder()
    {
        var q1 = TopLevel(1, QuestionFormat.Open, 1);
        var q2 = TopLevel(2, QuestionFormat.Open, 2);
        SetupQuestionnaireByProject(QuestionnaireOf(q2, q1)); // unordered on purpose
        SetupResponses();

        var path = await _manager.ComputeVisiblePathAsync(ProjectId, ParticipantId);

        Assert.Equal(new[] { 1, 2 }, path);
    }

    [Fact]
    public async Task ComputeVisiblePathAsync_AnswerRevealsChildAtThatChoiceIndex()
    {
        var q1 = TopLevel(1, QuestionFormat.SingleChoice, 1, (En, new[] { "A", "B" }));
        var q2 = Child(2, q1, choiceIndex: 0, position: 1);
        var q3 = TopLevel(3, QuestionFormat.Open, 2);
        SetupQuestionnaireByProject(QuestionnaireOf(q1, q2, q3));
        SetupResponses(Answer(q1, "A", En));

        var path = await _manager.ComputeVisiblePathAsync(ProjectId, ParticipantId);

        Assert.Equal(new[] { 1, 2, 3 }, path);
    }

    [Fact]
    public async Task ComputeVisiblePathAsync_DifferentAnswer_HidesChild()
    {
        var q1 = TopLevel(1, QuestionFormat.SingleChoice, 1, (En, new[] { "A", "B" }));
        var q2 = Child(2, q1, choiceIndex: 0, position: 1);
        var q3 = TopLevel(3, QuestionFormat.Open, 2);
        SetupQuestionnaireByProject(QuestionnaireOf(q1, q2, q3));
        SetupResponses(Answer(q1, "B", En));

        var path = await _manager.ComputeVisiblePathAsync(ProjectId, ParticipantId);

        Assert.Equal(new[] { 1, 3 }, path);
    }

    [Fact]
    public async Task ComputeVisiblePathAsync_BranchingUsesResponseSourceLanguageNotFirstTranslation()
    {
        // Q1 is authored in both en and nl; the participant answered in nl ("Ja"). The branch must
        // resolve the choice index against the nl translation, not the first (en) one. If the picker
        // wrongly used en, "Ja" would not match ["Yes","No"] and Q2 would stay hidden.
        var q1 = TopLevel(1, QuestionFormat.SingleChoice, 1,
            (En, new[] { "Yes", "No" }),
            (Nl, new[] { "Ja", "Nee" }));
        var q2 = Child(2, q1, choiceIndex: 0, position: 1);
        SetupQuestionnaireByProject(QuestionnaireOf(q1, q2));
        SetupResponses(Answer(q1, "Ja", Nl));

        var path = await _manager.ComputeVisiblePathAsync(ProjectId, ParticipantId);

        Assert.Equal(new[] { 1, 2 }, path);
    }

    [Fact]
    public async Task ComputeVisiblePathAsync_EndingAnswerInResponseLanguage_TerminatesPath()
    {
        var q1 = TopLevel(1, QuestionFormat.SingleChoice, 1, (En, new[] { "Yes", "No" }));
        q1.EndingAnswers.Add(new EndingAnswer
        {
            Translations = new List<EndingAnswerTranslation>
            {
                new() { Language = En, Value = "End" },
                new() { Language = Nl, Value = "Einde" },
            }
        });
        var q3 = TopLevel(3, QuestionFormat.Open, 2);
        SetupQuestionnaireByProject(QuestionnaireOf(q1, q3));
        SetupResponses(Answer(q1, "Einde", Nl));

        var path = await _manager.ComputeVisiblePathAsync(ProjectId, ParticipantId);

        // Ending answer matched (in nl) → walk stops; Q3 is never reached.
        Assert.Equal(new[] { 1 }, path);
    }

    [Fact]
    public async Task ComputeVisiblePathAsync_NullQuestionnaire_ReturnsEmpty()
    {
        _questionnaireManager.Setup(m => m.GetQuestionnaireByProjectIdWithTranslationsReadOnlyAsync(ProjectId))
            .ReturnsAsync((Questionnaire)null);

        var path = await _manager.ComputeVisiblePathAsync(ProjectId, ParticipantId);

        Assert.Empty(path);
    }

    [Fact]
    public async Task GetNextQuestionAsync_ReturnsFollowingQuestionInPath()
    {
        var q1 = TopLevel(1, QuestionFormat.Open, 1);
        var q2 = TopLevel(2, QuestionFormat.Open, 2);
        SetupQuestionnaireByQuestion(1, QuestionnaireOf(q1, q2));
        SetupResponses();

        var next = await _manager.GetNextQuestionAsync(currentQuestionId: 1, ParticipantId);

        Assert.Equal(2, next.QuestionId);
    }

    [Fact]
    public async Task GetNextQuestionAsync_LastQuestion_ReturnsNull()
    {
        var q1 = TopLevel(1, QuestionFormat.Open, 1);
        var q2 = TopLevel(2, QuestionFormat.Open, 2);
        SetupQuestionnaireByQuestion(2, QuestionnaireOf(q1, q2));
        SetupResponses();

        var next = await _manager.GetNextQuestionAsync(currentQuestionId: 2, ParticipantId);

        Assert.Null(next);
    }

    [Fact]
    public async Task GetPreviousQuestionAsync_ReturnsPrecedingQuestionInPath()
    {
        var q1 = TopLevel(1, QuestionFormat.Open, 1);
        var q2 = TopLevel(2, QuestionFormat.Open, 2);
        SetupQuestionnaireByQuestion(2, QuestionnaireOf(q1, q2));
        SetupResponses();

        var prev = await _manager.GetPreviousQuestionAsync(currentQuestionId: 2, ParticipantId);

        Assert.Equal(1, prev.QuestionId);
    }

    [Fact]
    public async Task GetResumeQuestionAsync_ReturnsFirstUnansweredQuestion()
    {
        var q1 = TopLevel(1, QuestionFormat.Open, 1);
        var q2 = TopLevel(2, QuestionFormat.Open, 2);
        SetupQuestionnaireByProject(QuestionnaireOf(q1, q2));
        SetupResponses(Answer(q1, "done", En));

        var resume = await _manager.GetResumeQuestionAsync(ProjectId, ParticipantId);

        Assert.Equal(2, resume.QuestionId);
    }
}
