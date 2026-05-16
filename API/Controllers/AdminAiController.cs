using Microsoft.AspNetCore.Mvc;
using SynapseEnqueteManagement.API.Ai;
using SynapseEnqueteManagement.BL.Domain.Ai;

namespace SynapseEnqueteManagement.API.Controllers;

[ApiController]
[Route("api/admin/ai")]
public class AdminAiController(ChatClientRegistry registry) : ControllerBase
{
    [HttpGet("available-models")]
    public IActionResult GetAvailableModels()
    {
        var ids = registry.AvailableModelIds.ToHashSet();
        var models = AiModelCatalog.All
            .Where(m => ids.Contains(m.Id))
            .Select(m => m.Id)
            .ToList();
        return Ok(models);
    }
}