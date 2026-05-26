using SynapseEnqueteManagement.BL.Exceptions;

namespace SynapseEnqueteManagement.BL.Tests.Unit;

public class ShortFormServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepo = new(MockBehavior.Strict);
    private readonly ShortFormService _service;

    public ShortFormServiceTests()
    {
        _service = new ShortFormService(_projectRepo.Object, Array.Empty<string>());
    }

    private void SetupShortFormFree(string shortForm, int? excludeProjectId = null)
        => _projectRepo.Setup(r => r.ExistsByShortFormAsync(shortForm, excludeProjectId)).ReturnsAsync(false);

    [Fact]
    public async Task ResolveAsync_DesiredGivenValidAndUnique_ReturnsDesired()
    {
        SetupShortFormFree("my-form");

        var result = await _service.ResolveAsync("my-form", defaultName: "ignored");

        Assert.Equal("my-form", result);
    }

    [Fact]
    public async Task ResolveAsync_BlankDesired_NormalisesFromDefaultName()
    {
        SetupShortFormFree("my-project");

        var result = await _service.ResolveAsync(desired: " ", defaultName: "My Project");

        Assert.Equal("my-project", result);
    }

    [Fact]
    public async Task ResolveAsync_InvalidCandidate_ThrowsInvalid()
    {
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _service.ResolveAsync("Invalid Form!", defaultName: null));

        Assert.Equal("Validation.ShortForm.Invalid", ex.Code);
    }

    [Fact]
    public async Task ResolveAsync_ReservedCandidate_ThrowsReserved()
    {
        // "admin" is in the hard-reserved set baked into ShortFormService.
        var ex = await Assert.ThrowsAsync<LocalizedValidationException>(() =>
            _service.ResolveAsync("admin", defaultName: null));

        Assert.Equal("Validation.ShortForm.Reserved", ex.Code);
    }

    [Fact]
    public async Task ResolveAsync_Collision_AppendsUniqueSuffix()
    {
        _projectRepo.Setup(r => r.ExistsByShortFormAsync("taken", null)).ReturnsAsync(true);
        _projectRepo.Setup(r => r.ExistsByShortFormAsync("taken-2", null)).ReturnsAsync(false);

        var result = await _service.ResolveAsync("taken", defaultName: null);

        Assert.Equal("taken-2", result);
    }

    [Fact]
    public async Task ResolveAsync_ExcludeProjectId_FlowsThroughToUniquenessCheck()
    {
        SetupShortFormFree("keep-form", excludeProjectId: 7);

        var result = await _service.ResolveAsync("keep-form", defaultName: null, excludeProjectId: 7);

        Assert.Equal("keep-form", result);
    }
}
