using System.Linq.Expressions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ParticipantManagerTests
{
    private readonly Mock<IRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ParticipantManager _manager;

    public ParticipantManagerTests()
    {
        _manager = new ParticipantManager(_repository.Object, _uow.Object);
    }

    #region AddParticipantAsync

    [Fact]
    public async Task AddParticipantAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        var questionnaire = new Questionnaire { QuestionnaireId = 5 };
        var project = new Project { ProjectId = 10 };
        _repository.Setup(r => r.ReadByIdAsync<Questionnaire>(5, It.IsAny<Expression<Func<Questionnaire, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(questionnaire);
        _repository.Setup(r => r.ReadByIdAsync<Project>(10, It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(project);
        _repository.Setup(r => r.CreateAsync(It.IsAny<Participant>(), It.IsAny<Expression<Func<Participant, bool>>>()))
            .ReturnsAsync((Participant p, Expression<Func<Participant, bool>> _) => p)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddParticipantAsync(questionnaire, 10);

        // Assert
        Assert.NotNull(created);
        Assert.Same(questionnaire, created.Questionnaire);
        Assert.Same(project, created.Project);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddParticipantAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _repository.Setup(r => r.ReadByIdAsync<Questionnaire>(It.IsAny<object>(), It.IsAny<Expression<Func<Questionnaire, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Questionnaire());
        _repository.Setup(r => r.ReadByIdAsync<Project>(It.IsAny<object>(), It.IsAny<Expression<Func<Project, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(new Project());
        _repository.Setup(r => r.CreateAsync(It.IsAny<Participant>(), It.IsAny<Expression<Func<Participant, bool>>>()))
            .ReturnsAsync((Participant p, Expression<Func<Participant, bool>> _) => p);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddParticipantAsync(new Questionnaire { QuestionnaireId = 1 }, 1, ct);

        // Assert
        _uow.VerifyAll();
    }

    #endregion

    #region MarkAsSubmittedAsync

    [Fact]
    public async Task MarkAsSubmittedAsync_HappyPath_UpdatesAndSavesOnce()
    {
        // Arrange
        var token = Guid.NewGuid();
        var participant = new Participant { SessionToken = token, HasSubmitted = false };
        _repository.Setup(r => r.ReadSingleAsync<Participant>(
                It.IsAny<Expression<Func<Participant, bool>>[]>(),
                It.IsAny<Expression<Func<Participant, object>>[]>(),
                It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync(participant);
        _repository.Setup(r => r.Update(participant)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.MarkAsSubmittedAsync(token);

        // Assert
        Assert.True(participant.HasSubmitted);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task MarkAsSubmittedAsync_ParticipantNotFound_ThrowsWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadSingleAsync<Participant>(
                It.IsAny<Expression<Func<Participant, bool>>[]>(),
                It.IsAny<Expression<Func<Participant, object>>[]>(),
                It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Participant)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.MarkAsSubmittedAsync(Guid.NewGuid()));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.Update(It.IsAny<Participant>()), Times.Never);
    }

    #endregion

    #region SaveContactInfoAsync

    [Fact]
    public async Task SaveContactInfoAsync_HappyPath_UpdatesAndSavesOnce()
    {
        // Arrange
        var participant = new Participant { ParticipantId = 1 };
        _repository.Setup(r => r.ReadByIdAsync<Participant>(1, It.IsAny<Expression<Func<Participant, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync(participant);
        _repository.Setup(r => r.Update(participant)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.SaveContactInfoAsync(1, "a@b.c");

        // Assert
        Assert.Equal("a@b.c", participant.Email);
        _repository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task SaveContactInfoAsync_ParticipantNotFound_ThrowsWithoutSaving()
    {
        // Arrange
        _repository.Setup(r => r.ReadByIdAsync<Participant>(It.IsAny<object>(), It.IsAny<Expression<Func<Participant, object>>[]>(), It.IsAny<string[]>(), It.IsAny<bool>()))
            .ReturnsAsync((Participant)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.SaveContactInfoAsync(42, "x"));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
