using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/sections")]
public class SectionsController(IQuestionnaireManager questionnaireManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SectionDto>>> GetSections(int projectId)
    {
        var sections = await questionnaireManager.GetSectionsReadOnlyAsync(projectId);
        var dtos = sections.Select(s => new SectionDto
        {
            SectionId = s.SectionId,
            Name = s.Name,
            Description = s.Description
        });
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<SectionDto>> CreateSection(int projectId, [FromBody] CreateSectionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var section = await questionnaireManager.CreateSectionAsync(projectId, dto.Name, dto.Description, ct);
        if (section is null)
            return NotFound();

        return Created(string.Empty, new SectionDto
        {
            SectionId = section.SectionId,
            Name = section.Name,
            Description = section.Description
        });
    }

    [HttpPut("{sectionId}")]
    public async Task<IActionResult> UpdateSection(int sectionId, [FromBody] UpdateSectionDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var success = await questionnaireManager.UpdateSectionAsync(sectionId, dto.Name, dto.Description, ct);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{sectionId}")]
    public async Task<IActionResult> DeleteSection(int sectionId, CancellationToken ct)
    {
        var success = await questionnaireManager.DeleteSectionAsync(sectionId, ct);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
