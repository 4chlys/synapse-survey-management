using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Survey;
using SynapseEnqueteManagement.BL.Domain.Project.Entities;
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
        var dtos = sections.Select(s => MapToDto(s, languageCode));
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

        return Created(string.Empty, MapToAdminDto(section));
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

    private static SectionDto MapToDto(Section section, string languageCode)
    {
        var translation = section.Translations
            ?.FirstOrDefault(t => t.Language?.Code == languageCode)
            ?? section.Translations?.FirstOrDefault();

        return new SectionDto
        {
            SectionId = section.SectionId,
            Name = translation?.Name,
            Description = translation?.Description,
            Translations = section.Translations?.Select(t => new SectionTranslationDto
            {
                LanguageCode = t.Language?.Code,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? new List<SectionTranslationDto>()
        };
    }

    private static AdminSectionDto MapToAdminDto(Section section)
        => new()
        {
            SectionId = section.SectionId,
            Translations = section.Translations?.Select(t => new SectionTranslationDto
            {
                LanguageCode = t.Language?.Code,
                Name = t.Name,
                Description = t.Description
            }).ToList() ?? new List<SectionTranslationDto>()
        };
}
