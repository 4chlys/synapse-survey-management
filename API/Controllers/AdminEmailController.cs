using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SynapseEnqueteManagement.API.DTOs.Email;
using SynapseEnqueteManagement.BL.Interfaces;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/emails")]
[EnableRateLimiting("admin-email-send")]
public class AdminEmailController(IEmailService emailService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> Send(SendEmailDto dto, CancellationToken ct)
    {
        await emailService.SendAsync(dto.To, dto.Subject, dto.Body, ct);
        return NoContent();
    }
}
