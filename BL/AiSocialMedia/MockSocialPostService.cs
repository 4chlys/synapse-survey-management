using SynapseEnqueteManagement.BL.Ai;

namespace SynapseEnqueteManagement.BL.AiSocialMedia;

public sealed class MockSocialPostService : IAiSocialPostService
{
    public Task<SocialPostReply> ChatAsync(
        SocialPostRequest req,
        ResolvedPlatformAiConfig platformConfig,
        int projectId,
        CancellationToken ct = default)
    {
        var copy = req.Platform switch
        {
            SocialPlatform.LinkedIn =>
                $"🚀 {req.OrganisationName} heeft geweldige resultaten geboekt met het project '{req.ProjectName}'. " +
                $"Al {req.Stats.ViewCount} weergaven en {req.Stats.CompletionCount} deelnemers! " +
                $"Bekijk de top ideeën: {string.Join(", ", req.Stats.TopIdeaDescriptions.Take(2))}.",
            SocialPlatform.Facebook =>
                $"Wat een mooie respons op '{req.ProjectName}'! 👏 " +
                $"{req.Stats.CompletionCount} mensen deden mee. Dank jullie wel!",
            _ =>
                $"'{req.ProjectName}' leeft! {req.Stats.ViewCount} weergaven, {req.Stats.CompletionCount} reacties. 🙌"
        };

        var tags = req.Platform switch
        {
            SocialPlatform.LinkedIn  => new[] { "#participatie", "#jongeren", $"#{req.ProjectName.Replace(" ", "")}" },
            SocialPlatform.Facebook  => new[] { "#meedoen", $"#{req.OrganisationName.Replace(" ", "")}" },
            _                        => new[] { "#participatie", "#jongeren", "#meedoen", $"#{req.ProjectName.Replace(" ", "")}", "#samen" }
        };

        return Task.FromResult(new SocialPostReply(copy, tags, copy.Length));
    }
}
