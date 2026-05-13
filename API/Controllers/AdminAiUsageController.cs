using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.DTOs.AiUsage;
using SynapseEnqueteManagement.BL.AiUsage;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/ai-usage")]
public class AdminAiUsageController(IAiUsageManager usageManager) : ControllerBase
{
    [HttpGet("projects")]
    public async Task<ActionResult<ICollection<ProjectCostAggregateDto>>> GetProjects([FromQuery] DateTime? from)
    {
        var effectiveFrom = from ?? DateTime.UtcNow.AddHours(-24);
        var aggregates = await usageManager.ReadProjectAggregatesAsync(effectiveFrom);
        return Ok(aggregates.Select(a => new ProjectCostAggregateDto(
            a.ProjectId,
            a.ShortForm,
            a.ProjectPlatformId,
            a.ProjectPlatformName,
            a.InputTokens,
            a.OutputTokens,
            a.CostEur)).ToList());
    }

    [HttpGet("platforms")]
    public async Task<ActionResult<ICollection<PlatformCostAggregateDto>>> GetPlatforms([FromQuery] DateTime? from)
    {
        var effectiveFrom = from ?? DateTime.UtcNow.AddHours(-24);
        var aggregates = await usageManager.ReadPlatformAggregatesAsync(effectiveFrom);
        return Ok(aggregates.Select(a => new PlatformCostAggregateDto(
            a.ProjectPlatformId,
            a.ProjectPlatformName,
            a.InputTokens,
            a.OutputTokens,
            a.CostEur)).ToList());
    }

    [HttpGet("projects/{id:int}/by-service")]
    public async Task<ActionResult<ICollection<ServiceCostAggregateDto>>> GetByService(
        int id,
        [FromQuery] DateTime? from)
    {
        var effectiveFrom = from ?? DateTime.UtcNow.AddHours(-24);
        var aggregates = await usageManager.ReadServiceBreakdownByProjectAsync(id, effectiveFrom);
        return Ok(aggregates.Select(a => new ServiceCostAggregateDto(
            a.Service.ToString(),
            a.Model,
            a.InputTokens,
            a.OutputTokens,
            a.CostEur)).ToList());
    }
}
