using System.Linq.Expressions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ParticipantManagerTests
{
    private readonly Mock<IParticipantRepository> _participantRepository = new(MockBehavior.Strict);
    private readonly Mock<IProjectRepository> _projectRepository = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ParticipantManager _manager;

    public ParticipantManagerTests()
    {
        _manager = new ParticipantManager(_participantRepository.Object, _projectRepository.Object, _uow.Object);
    }

    #region AddParticipantAsync

    [Fact]
    public async Task AddParticipantAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        var project = new Project { ProjectId = 10 };
        _projectRepository.Setup(r => r.ReadByIdForUpdateAsync(10))
            .ReturnsAsync(project);
        _participantRepository.Setup(r => r.CreateAsync(It.IsAny<Participant>(), It.IsAny<Expression<Func<Participant, bool>>>()))
            .ReturnsAsync((Participant p, Expression<Func<Participant, bool>> _) => p)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddParticipantAsync(10);

        // Assert
        Assert.NotNull(created);
        Assert.Same(project, created.Project);
        _participantRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddParticipantAsync_ProjectNotFound_ThrowsWithoutSaving()
    {
        // Arrange
        _projectRepository.Setup(r => r.ReadByIdForUpdateAsync(It.IsAny<int>()))
            .ReturnsAsync((Project)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _manager.AddParticipantAsync(999));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _participantRepository.Verify(r => r.CreateAsync(It.IsAny<Participant>(), It.IsAny<Expression<Func<Participant, bool>>>()), Times.Never);
    }

    [Fact]
    public async Task AddParticipantAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        _projectRepository.Setup(r => r.ReadByIdForUpdateAsync(It.IsAny<int>()))
            .ReturnsAsync(new Project());
        _participantRepository.Setup(r => r.CreateAsync(It.IsAny<Participant>(), It.IsAny<Expression<Func<Participant, bool>>>()))
            .ReturnsAsync((Participant p, Expression<Func<Participant, bool>> _) => p);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddParticipantAsync(1, ct);

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
        _participantRepository.Setup(r => r.ReadBySessionTokenForUpdateAsync(token))
            .ReturnsAsync(participant);
        _participantRepository.Setup(r => r.Update(participant)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.MarkAsSubmittedAsync(token);

        // Assert
        Assert.True(participant.HasSubmitted);
        _participantRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task MarkAsSubmittedAsync_ParticipantNotFound_ThrowsWithoutSaving()
    {
        // Arrange
        _participantRepository.Setup(r => r.ReadBySessionTokenForUpdateAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Participant)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.MarkAsSubmittedAsync(Guid.NewGuid()));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _participantRepository.Verify(r => r.Update(It.IsAny<Participant>()), Times.Never);
    }

    #endregion

    #region SaveContactInfoAsync

    [Fact]
    public async Task SaveContactInfoAsync_HappyPath_UpdatesAndSavesOnce()
    {
        // Arrange
        var participant = new Participant { ParticipantId = 1 };
        _participantRepository.Setup(r => r.ReadByIdForUpdateAsync(1))
            .ReturnsAsync(participant);
        _participantRepository.Setup(r => r.Update(participant)).Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.SaveContactInfoAsync(1, "a@b.c");

        // Assert
        Assert.Equal("a@b.c", participant.Email);
        _participantRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task SaveContactInfoAsync_ParticipantNotFound_ThrowsWithoutSaving()
    {
        // Arrange
        _participantRepository.Setup(r => r.ReadByIdForUpdateAsync(It.IsAny<int>()))
            .ReturnsAsync((Participant)null);

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _manager.SaveContactInfoAsync(42, "x"));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
