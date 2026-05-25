using SynapseEnqueteManagement.BL.Domain.I18n.Entities;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ParticipantManagerTests
{
    private readonly Mock<IParticipantRepository> _participantRepository = new(MockBehavior.Strict);
    private readonly Mock<ILanguageResolver> _languageResolver = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _uow = new(MockBehavior.Strict);
    private readonly ParticipantManager _manager;

    public ParticipantManagerTests()
    {
        _manager = new ParticipantManager(
            _participantRepository.Object,
            _languageResolver.Object,
            _uow.Object);
    }

    #region AddParticipantAsync

    [Fact]
    public async Task AddParticipantAsync_HappyPath_CreatesAndSavesOnce()
    {
        // Arrange
        var project = new Project { ProjectId = 10, Questionnaire = new Questionnaire() };
        var language = new Language { Code = "nl", IsActive = true };
        _languageResolver.Setup(r => r.ResolveSingleAsync("nl", true)).ReturnsAsync(language);
        _participantRepository.Setup(r => r.CreateAsync(It.IsAny<Participant>()))
            .ReturnsAsync((Participant p) => p)
            .Verifiable(Times.Exactly(1));
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var created = await _manager.AddParticipantAsync(project, "nl");

        // Assert
        Assert.NotNull(created);
        Assert.Same(project, created.Project);
        _participantRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task AddParticipantAsync_ForwardsCancellationTokenToUnitOfWork()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;
        var language = new Language { Code = "nl", IsActive = true };
        _languageResolver.Setup(r => r.ResolveSingleAsync("nl", true)).ReturnsAsync(language);
        _participantRepository.Setup(r => r.CreateAsync(It.IsAny<Participant>()))
            .ReturnsAsync((Participant p) => p);
        _uow.Setup(u => u.SaveChangesAsync(ct))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        await _manager.AddParticipantAsync(new Project { Questionnaire = new Questionnaire() }, "nl", ct);

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

    #region SetLanguageAsync

    [Fact]
    public async Task SetLanguageAsync_ValidActiveLanguage_SetsLanguageAndSavesOnce()
    {
        // Arrange
        var participant = new Participant { ParticipantId = 1 };
        var language = new Language { Code = "en", IsActive = true };
        _languageResolver.Setup(r => r.ResolveSingleAsync("en", true)).ReturnsAsync(language);
        _participantRepository.Setup(r => r.ReadByIdForUpdateAsync(1)).ReturnsAsync(participant);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Verifiable(Times.Exactly(1));

        // Act
        var returned = await _manager.SetLanguageAsync(1, "en");

        // Assert
        Assert.Same(language, participant.Language);
        Assert.Same(language, returned);
        _participantRepository.VerifyAll();
        _uow.VerifyAll();
    }

    [Fact]
    public async Task SetLanguageAsync_UnknownLanguageCode_ThrowsWithoutSaving()
    {
        // Arrange
        _languageResolver.Setup(r => r.ResolveSingleAsync("xx", true))
            .ThrowsAsync(new KeyNotFoundException("Language 'xx' is not a supported active language."));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _manager.SetLanguageAsync(1, "xx"));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _participantRepository.Verify(r => r.Update(It.IsAny<Participant>()), Times.Never);
    }

    [Fact]
    public async Task SetLanguageAsync_InactiveLanguage_ThrowsWithoutSaving()
    {
        // Arrange
        _languageResolver.Setup(r => r.ResolveSingleAsync("fr", true))
            .ThrowsAsync(new KeyNotFoundException("Language 'fr' is not a supported active language."));

        // Act + Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => _manager.SetLanguageAsync(1, "fr"));

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _participantRepository.Verify(r => r.Update(It.IsAny<Participant>()), Times.Never);
    }

    #endregion
}
