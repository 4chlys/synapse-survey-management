using SynapseEnqueteManagement.BL.Helpers;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ContentLanguageResolverTests
{
    private static readonly Language Nl = new() { Code = "nl", DisplayName = "Nederlands", IsActive = true };
    private static readonly Language En = new() { Code = "en", DisplayName = "English", IsActive = true };
    private static readonly Language Fr = new() { Code = "fr", DisplayName = "Français", IsActive = true };

    [Fact]
    public void Resolve_SubmissionCodeInSupportedSet_WinsOverParticipantLanguage()
    {
        var participant = new Participant { Language = Nl };
        var project = new Project { SupportedLanguages = new List<Language> { Nl, En } };

        var result = ContentLanguageResolver.Resolve(participant, project, "en");

        Assert.Same(En, result);
    }

    [Fact]
    public void Resolve_SubmissionCodeCaseInsensitive_Matches()
    {
        var participant = new Participant { Language = Nl };
        var project = new Project { SupportedLanguages = new List<Language> { Nl, En } };

        var result = ContentLanguageResolver.Resolve(participant, project, "EN");

        Assert.Same(En, result);
    }

    [Fact]
    public void Resolve_SubmissionCodeNotInSupportedSet_FallsBackToParticipantLanguage()
    {
        var participant = new Participant { Language = Nl };
        var project = new Project { SupportedLanguages = new List<Language> { Nl, En } };

        var result = ContentLanguageResolver.Resolve(participant, project, "fr");

        Assert.Same(Nl, result);
    }

    [Fact]
    public void Resolve_NoSubmissionCode_UsesParticipantLanguage()
    {
        var participant = new Participant { Language = Nl };
        var project = new Project { SupportedLanguages = new List<Language> { Nl, En } };

        var result = ContentLanguageResolver.Resolve(participant, project, null);

        Assert.Same(Nl, result);
    }

    [Fact]
    public void Resolve_NoParticipantLanguage_FallsBackToParticipantProjectDefault()
    {
        var participantProject = new Project { DefaultLanguage = Fr };
        var participant = new Participant { Language = null, Project = participantProject };
        var project = new Project { SupportedLanguages = new List<Language> { Nl, En }, DefaultLanguage = Nl };

        var result = ContentLanguageResolver.Resolve(participant, project, null);

        Assert.Same(Fr, result);
    }

    [Fact]
    public void Resolve_NoParticipantOrParticipantProjectLanguage_FallsBackToProjectDefault()
    {
        // The widening over the old inline logic: when the participant carries no language at all,
        // the passed-in project's default is the last resort.
        var participant = new Participant { Language = null, Project = null };
        var project = new Project { SupportedLanguages = new List<Language> { Nl, En }, DefaultLanguage = Nl };

        var result = ContentLanguageResolver.Resolve(participant, project, null);

        Assert.Same(Nl, result);
    }

    [Fact]
    public void Resolve_NothingResolvable_ReturnsNull()
    {
        var participant = new Participant { Language = null, Project = null };

        var result = ContentLanguageResolver.Resolve(participant, project: null, submissionLanguageCode: null);

        Assert.Null(result);
    }
}
