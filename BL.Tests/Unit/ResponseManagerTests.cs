using System.Linq.Expressions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ResponseManagerTests
{
    private readonly Mock<IResponseRepository> _responseRepository = new(MockBehavior.Strict);
    private readonly Mock<IQuestionRepository> _questionRepository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly Mock<IResponseValidatorFactory> _validatorFactory = new(MockBehavior.Strict);
    private readonly Mock<IResponseValidator> _validator = new(MockBehavior.Strict);
    private readonly ResponseManager _manager;

    private readonly Participant _participant = new() { ParticipantId = 3 };
    private readonly Question _question = new() { QuestionId = 77, QuestionFormat = QuestionFormat.Open };

    public ResponseManagerTests()
    {
        _validatorFactory.Setup(f => f.GetValidator(It.IsAny<QuestionFormat>())).Returns(_validator.Object);
        _validator.Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<Question>()));
        _questionRepository.Setup(r => r.ReadByIdForUpdateAsync(_question.QuestionId)).ReturnsAsync(_question);
        _manager = new ResponseManager(_responseRepository.Object, _questionRepository.Object, _uow.Object, _validatorFactory.Object);
    }

    [Fact]
    public async Task AddResponseAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        _responseRepository.Setup(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()))
            .ReturnsAsync((Response r, Expression<Func<Response, bool>> _) => r)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.AddResponseAsync(_question.QuestionId, "value", _participant);

        // Assert
        Assert.Equal("value", result.Value);
        _responseRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddResponseAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _responseRepository.Setup(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()))
            .ReturnsAsync((Response r, Expression<Func<Response, bool>> _) => r);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddResponseAsync(_question.QuestionId, "value", _participant, ct);

        // Assert
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddResponseAsync_QuestionNotFound_ThrowsAndDoesNotSave()
    {
        // Arrange
        _questionRepository.Setup(r => r.ReadByIdForUpdateAsync(999)).ReturnsAsync((Question)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.AddResponseAsync(999, "value", _participant));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _responseRepository.Verify(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrCreateResponseAsync_WhenExisting_UpdatesAndSavesOnce()
    {
        // Arrange
        var existing = new Response { ResponseId = 1, Question = _question, Participant = _participant, Value = "old", IsCommitted = true };
        _responseRepository.Setup(r => r.ReadByQuestionAndParticipantForUpdateAsync(_question.QuestionId, _participant.ParticipantId))
            .ReturnsAsync(existing);
        _responseRepository.Setup(r => r.Update(existing)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.UpdateOrCreateResponseAsync(_question.QuestionId, "new", _participant);

        // Assert
        Assert.Same(existing, result);
        Assert.Equal("new", existing.Value);
        Assert.False(existing.IsCommitted);
        _responseRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task UpdateOrCreateResponseAsync_WhenNotExisting_CreatesAndSavesOnce()
    {
        // Arrange
        _responseRepository.Setup(r => r.ReadByQuestionAndParticipantForUpdateAsync(_question.QuestionId, _participant.ParticipantId))
            .ReturnsAsync((Response)null);
        _responseRepository.Setup(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()))
            .ReturnsAsync((Response r, Expression<Func<Response, bool>> _) => r)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.UpdateOrCreateResponseAsync(_question.QuestionId, "new", _participant);

        // Assert
        Assert.Equal("new", result.Value);
        Assert.False(result.IsCommitted);
        _responseRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddResponseAsync_ValueTooLong_ThrowsAndDoesNotSave()
    {
        // Arrange
        var tooLong = new string('x', 501);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.AddResponseAsync(_question.QuestionId, tooLong, _participant));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _responseRepository.Verify(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task CommitResponsesAsync_SavesExactlyOnce_RegardlessOfNumberOfUpdates()
    {
        // Arrange — three responses, two need state changes
        var q1 = new Question { QuestionId = 1 };
        var q2 = new Question { QuestionId = 2 };
        var q3 = new Question { QuestionId = 3 };
        var responses = new List<Response>
        {
            new() { Question = q1, IsCommitted = false }, // → visible, will change to true
            new() { Question = q2, IsCommitted = true  }, // → hidden, will change to false
            new() { Question = q3, IsCommitted = true  }, // → visible, unchanged
        };
        _responseRepository.Setup(r => r.ReadByParticipantWithQuestionForUpdateAsync(_participant.ParticipantId))
            .ReturnsAsync(responses);
        _responseRepository.Setup(r => r.Update(It.IsAny<Response>())).Verifiable(Times.Exactly(2));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.CommitResponsesAsync(_participant.ParticipantId, [1, 3]);

        // Assert
        _responseRepository.VerifyAll();
        _uow.VerifyAll();
        Assert.True(responses[0].IsCommitted);
        Assert.False(responses[1].IsCommitted);
        Assert.True(responses[2].IsCommitted);
    }

    [Fact]
    public async Task CommitResponsesAsync_WhenNothingChanged_DoesNotSave()
    {
        // Arrange — all responses already in correct state
        var q1 = new Question { QuestionId = 1 };
        var responses = new List<Response>
        {
            new() { Question = q1, IsCommitted = true }
        };
        _responseRepository.Setup(r => r.ReadByParticipantWithQuestionForUpdateAsync(_participant.ParticipantId))
            .ReturnsAsync(responses);

        // Act
        await _manager.CommitResponsesAsync(_participant.ParticipantId, [1]);

        // Assert
        _responseRepository.Verify(r => r.Update(It.IsAny<Response>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
