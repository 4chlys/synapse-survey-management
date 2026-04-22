 using Microsoft.AspNetCore.Mvc;
  using SynapseEnqueteManagement.API.DTOs.Survey;
  using SynapseEnqueteManagement.BL.Interfaces;

  namespace SynapseEnqueteManagement.API.Controllers;

  [ApiController]
  [Route("api/projects/{projectId}/sections")]
  public class SectionsController(ISectionManager sectionManager) : ControllerBase
  {
      [HttpGet]
      public async Task<ActionResult<IEnumerable<SectionDto>>> GetSections(int
  projectId)
      {
          var sections = await sectionManager.GetSectionsAsync(projectId);
          var dtos = sections.Select(s => new SectionDto
          {
              SectionId = s.SectionId,
              Name = s.Name,
              Description = s.Description
          });
          return Ok(dtos);
      }

      [HttpPost]
      public async Task<ActionResult<SectionDto>> CreateSection(int projectId,
  [FromBody] CreateSectionDto dto)
      {
          if (!ModelState.IsValid)
              return BadRequest(ModelState);

          var section = await sectionManager.CreateSectionAsync(projectId,
  dto.Name, dto.Description);
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
      public async Task<IActionResult> UpdateSection(int sectionId, [FromBody]
  UpdateSectionDto dto)
      {
          if (!ModelState.IsValid)
              return BadRequest(ModelState);

          var success = await sectionManager.UpdateSectionAsync(sectionId,
  dto.Name, dto.Description);
          if (!success)
              return NotFound();

          return NoContent();
      }

      [HttpDelete("{sectionId}")]
      public async Task<IActionResult> DeleteSection(int sectionId)
      {
          var success = await sectionManager.DeleteSectionAsync(sectionId);
          if (!success)
              return NotFound();

          return NoContent();
      }
  }