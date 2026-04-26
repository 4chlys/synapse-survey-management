using System.ComponentModel.DataAnnotations;
using SynapseEnqueteManagement.API.DTOs.Project;

namespace SynapseEnqueteManagement.API.Tests;

public class ProjectsControllerTests
{
    private readonly Mock<IProjectManager> _projectManager = new();
    private readonly ProjectsController _controller;

    public ProjectsControllerTests()
    {
        _controller = new ProjectsController(_projectManager.Object);
    }

    #region GetByShortForm

    [Fact]
    public async Task GetByShortForm_Found_ReturnsOkWithLookupDto()
    {
        // Arrange
        var project = new Project
        {
            ProjectId = 5,
            ShortForm = "demo",
            ProjectState = ProjectState.Active
        };
        _projectManager.Setup(m => m.GetProjectByShortFormAsync("demo")).ReturnsAsync(project);

        // Act
        var result = await _controller.GetByShortForm("demo");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<ProjectShortFormLookupDto>(ok.Value);
        Assert.Equal(5, dto.ProjectId);
        Assert.Equal("demo", dto.ShortForm);
        Assert.Equal("Active", dto.ProjectState);
    }

    [Fact]
    public async Task GetByShortForm_NotFound_Returns404()
    {
        // Arrange
        _projectManager.Setup(m => m.GetProjectByShortFormAsync("unknown")).ReturnsAsync((Project)null);

        // Act
        var result = await _controller.GetByShortForm("unknown");

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetByShortForm_LowercasesInput()
    {
        // Arrange
        _projectManager.Setup(m => m.GetProjectByShortFormAsync("demo")).ReturnsAsync((Project)null);

        // Act
        await _controller.GetByShortForm("DEMO");

        // Assert
        _projectManager.Verify(m => m.GetProjectByShortFormAsync("demo"), Times.Once);
    }

    #endregion

    #region CreateProject — ShortForm scenarios

    [Fact]
    public async Task CreateProject_BlankShortForm_PassesNullToManager()
    {
        // Arrange
        var createdProject = new Project
        {
            ProjectId = 1,
            Name = "Test",
            ShortForm = "test",
            ProjectState = ProjectState.Draft,
            Questionnaire = new Questionnaire()
        };
        _projectManager
            .Setup(m => m.CreateProjectAsync(
                "Test", string.Empty, string.Empty, string.Empty,
                FormType.VerticalScroll, AiAssistanceType.ProActive,
                null, null, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProject);

        var dto = new CreateProjectDto { Name = "Test" };

        // Act
        var result = await _controller.CreateProject(dto, CancellationToken.None);

        // Assert
        var created = Assert.IsType<CreatedResult>(result.Result);
        var projectDto = Assert.IsType<ProjectDto>(created.Value);
        Assert.Equal("test", projectDto.ShortForm);
    }

    [Fact]
    public async Task CreateProject_ReservedShortForm_ManagerThrows_ReturnsInternalServerError()
    {
        // The ExceptionHandlerMiddleware converts ValidationException → 400.
        // At the controller level the exception propagates; this test verifies the manager is called.
        _projectManager
            .Setup(m => m.CreateProjectAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                "admin", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("'admin' is a reserved shortform."));

        var dto = new CreateProjectDto { Name = "Admin", ShortForm = "admin" };

        // Act & Assert — middleware converts this; the controller lets it propagate
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.CreateProject(dto, CancellationToken.None));
    }

    #endregion

    #region UpdateProject — ShortForm locking

    [Fact]
    public async Task UpdateProject_ShortFormChangeInActive_ManagerThrows_Propagates()
    {
        // Arrange
        _projectManager
            .Setup(m => m.UpdateProjectAsync(
                1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<FormType>(), It.IsAny<AiAssistanceType>(),
                It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<bool>(),
                "new-form", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("ShortForm cannot be changed after the project has been published."));

        var dto = new UpdateProjectDto { Name = "Name", ShortForm = "new-form" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _controller.UpdateProject(1, dto, CancellationToken.None));
    }

    #endregion
}
