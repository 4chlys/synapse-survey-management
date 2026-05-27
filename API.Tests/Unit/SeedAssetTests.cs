using SynapseEnqueteManagement.API.Services;

namespace SynapseEnqueteManagement.API.Tests.Unit;

public class SeedAssetTests
{
    [Theory]
    [InlineData("https://localhost:7001/seed/questions/images/study-stress.jpg", true)]
    [InlineData("/seed/ideas/idea-meditation.jpg", true)]                       // relative URL
    [InlineData("https://localhost:7001/uploads/ideas/photo.jpg", false)]
    [InlineData("https://localhost:7001/uploads/seed-summary/photo.jpg", false)] // "seed" only as a name fragment
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsSeedUrl_DetectsSeedHostedAssets(string url, bool expected)
    {
        Assert.Equal(expected, SeedAsset.IsSeedUrl(url));
    }
}
