using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.API.Services;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/sections")]
public class SectionsController(
    ISectionManager sectionManager,
    IProjectManager projectManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SectionDto>>> GetSections(int projectId,
        [FromQuery] string languageCode = null)
    {
        if (languageCode is null)
        {
            var project = await projectManager.GetProjectByIdReadOnlyAsync(projectId);
            languageCode = project?.DefaultLanguage?.Code ?? "nl";
        }

        var sections = await sectionManager.GetSectionsReadOnlyAsync(projectId);
        var dtos = sections.Select(s => SectionDtoMapper.ToDto(s, languageCode));
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<AdminSectionDto>> CreateSection(int projectId, CreateSectionDto dto,
        CancellationToken ct)
    {
        var translationInputs = dto.Translations.Select(t =>
            new SectionTranslationInput(t.LanguageCode, t.Name, t.Description));

        var section = await sectionManager.CreateSectionAsync(projectId, translationInputs, ct);
        if (section is null)
            return NotFound();

        return Created(string.Empty, SectionDtoMapper.ToAdminDto(section));
    }

    [HttpPut("{sectionId}")]
    public async Task<IActionResult> UpdateSection(int sectionId, UpdateSectionDto dto, CancellationToken ct)
    {
        var translationInputs = dto.Translations.Select(t =>
            new SectionTranslationInput(t.LanguageCode, t.Name, t.Description));

        var success = await sectionManager.UpdateSectionAsync(sectionId, translationInputs, ct);
        if (!success)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{sectionId}")]
    public async Task<IActionResult> DeleteSection(int sectionId, CancellationToken ct)
    {
        var success = await sectionManager.DeleteSectionAsync(sectionId, ct);
        if (!success)
            return NotFound();

        return NoContent();
    }
}
