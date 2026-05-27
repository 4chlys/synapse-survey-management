using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Platform;
using SynapseEnqueteManagement.BL.Domain.Platform.Entities;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

/// <summary>
/// Admin CRUD for ProjectPlatform rows. Used by UI-Admin so it doesn't need its own
/// EF mapping of the ProjectPlatforms table (UI-Admin only knows the AdminPlatformDto shape).
/// Auth: X-Api-Key (the API-wide middleware) — same trust level as every other endpoint UI-Admin calls.
/// </summary>
[ApiController]
[Route("api/admin/platforms")]
public class AdminPlatformsController(IProjectPlatformManager platformManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AdminPlatformDto>>> GetAll()
    {
        var platforms = await platformManager.GetAllOrderedByNameReadOnlyAsync();
        return Ok(platforms.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AdminPlatformDto>> GetById(int id)
    {
        var platform = await platformManager.GetByIdReadOnlyAsync(id);
        return platform is null ? NotFound() : Ok(ToDto(platform));
    }

    [HttpPost]
    public async Task<ActionResult<AdminPlatformDto>> Create([FromBody] CreateProjectPlatformDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var platform = await platformManager.CreateAsync(
            dto.Name,
            dto.LogoUrl    ?? string.Empty,
            dto.WebsiteUrl ?? string.Empty);

        return CreatedAtAction(nameof(GetById),
            new { id = platform.ProjectPlatformId },
            ToDto(platform));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectPlatformDto dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var updated = await platformManager.UpdateAsync(
            id,
            dto.Name,
            dto.LogoUrl    ?? string.Empty,
            dto.WebsiteUrl ?? string.Empty);

        return updated ? NoContent() : NotFound();
    }

    private static AdminPlatformDto ToDto(ProjectPlatform p) => new()
    {
        ProjectPlatformId = p.ProjectPlatformId,
        Name              = p.Name,
        LogoUrl           = p.LogoUrl,
        WebsiteUrl        = p.WebsiteUrl,
        CreatedAt         = p.CreatedAt,
    };
}
