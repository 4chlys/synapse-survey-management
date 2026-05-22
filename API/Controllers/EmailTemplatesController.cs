using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.Email;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/email-template")]
public class EmailTemplatesController(IEmailTemplateManager emailTemplateManager) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<EmailTemplateDto>> Get(int projectId)
    {
        var template = await emailTemplateManager.GetByProjectIdAsync(projectId);
        if (template is null) return Ok(new EmailTemplateDto(string.Empty, string.Empty, false));
        return Ok(new EmailTemplateDto(template.Subject, template.Body, template.IsEnabled));
    }

    [HttpPut]
    public async Task<ActionResult> Save(int projectId, SaveEmailTemplateDto dto, CancellationToken ct)
    {
        await emailTemplateManager.UpsertAsync(projectId, dto.Subject, dto.Body, dto.IsEnabled, ct);
        return NoContent();
    }
}
