using System.Linq.Expressions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ResponseManagerTests
{
    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
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
        _manager = new ResponseManager(_repository.Object, _uow.Object, _validatorFactory.Object);
    }

    [Fact]
    public async Task AddResponseAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        _repository.Setup(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()))
            .ReturnsAsync((Response r, Expression<Func<Response, bool>> _) => r)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.AddResponseAsync("value", _question, _participant);

        // Assert
        Assert.Equal("value", result.Value);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddResponseAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _repository.Setup(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()))
            .ReturnsAsync((Response r, Expression<Func<Response, bool>> _) => r);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddResponseAsync("value", _question, _participant, ct);

        // Assert
        _uow.VerifyAll();
    }

    [Fact]
    public async Task UpdateOrCreateResponseAsync_WhenExisting_UpdatesAndSavesOnce()
    {
        // Arrange
        var existing = new Response { ResponseId = 1, Question = _question, Participant = _participant, Value = "old", IsCommitted = true };
        _repository.Setup(r => r.ReadSingleAsync<Response>(
                It.IsAny<Expression<Func<Response, bool>>[]>(),
                It.IsAny<Expression<Func<Response, object>>[]>(),
                It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync(existing);
        _repository.Setup(r => r.Update(existing)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.UpdateOrCreateResponseAsync("new", _question, _participant);

        // Assert
        Assert.Same(existing, result);
        Assert.Equal("new", existing.Value);
        Assert.False(existing.IsCommitted);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task UpdateOrCreateResponseAsync_WhenNotExisting_CreatesAndSavesOnce()
    {
        // Arrange
        _repository.Setup(r => r.ReadSingleAsync<Response>(
                It.IsAny<Expression<Func<Response, bool>>[]>(),
                It.IsAny<Expression<Func<Response, object>>[]>(),
                It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Response)null);
        _repository.Setup(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()))
            .ReturnsAsync((Response r, Expression<Func<Response, bool>> _) => r)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var result = await _manager.UpdateOrCreateResponseAsync("new", _question, _participant);

        // Assert
        Assert.Equal("new", result.Value);
        Assert.False(result.IsCommitted);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddResponseAsync_ValueTooLong_ThrowsAndDoesNotSave()
    {
        // Arrange
        var tooLong = new string('x', 501);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.AddResponseAsync(tooLong, _question, _participant));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.CreateAsync(It.IsAny<Response>(), It.IsAny<Expression<Func<Response, bool>>>()), Times.Never);
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
        _repository.Setup(r => r.ReadAsync<Response>(
                It.IsAny<Expression<Func<Response, bool>>[]>(),
                It.IsAny<Expression<Func<Response, object>>[]>(),
                It.IsAny<string[]>(),
                It.IsAny<Func<IQueryable<Response>, IOrderedQueryable<Response>>>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(responses);
        _repository.Setup(r => r.Update(It.IsAny<Response>())).Verifiable(Times.Exactly(2));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.CommitResponsesAsync(_participant.ParticipantId, [1, 3]);

        // Assert
        _repository.VerifyAll();
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
        _repository.Setup(r => r.ReadAsync<Response>(
                It.IsAny<Expression<Func<Response, bool>>[]>(),
                It.IsAny<Expression<Func<Response, object>>[]>(),
                It.IsAny<string[]>(),
                It.IsAny<Func<IQueryable<Response>, IOrderedQueryable<Response>>>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<int?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(responses);

        // Act
        await _manager.CommitResponsesAsync(_participant.ParticipantId, [1]);

        // Assert
        _repository.Verify(r => r.Update(It.IsAny<Response>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
