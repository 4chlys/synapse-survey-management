using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs;
using SynapseEnqueteManagement.BL;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectManager _projectManager;

    public ProjectsController(IProjectManager projectManager)
    {
        _projectManager = projectManager;
    }

    [HttpGet("{projectId}")]
    public ActionResult<ProjectDto> GetProject(int projectId)
    {
        var project = _projectManager.GetProjectById(projectId);
        if (project is null)
            return NotFound();

        return Ok(new ProjectDto
        {
            ProjectId = project.ProjectId,
            ProjectState = project.ProjectState.ToString()
        });
    }
}
