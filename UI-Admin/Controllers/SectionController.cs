  using Microsoft.AspNetCore.Mvc;
  using SynapseEnqueteManagement.UI.Admin.Models.Section;
  using SynapseEnqueteManagement.UI.Admin.Services.Section;

  namespace SynapseEnqueteManagement.UI.Admin.Controllers;

  public class SectionController(SectionAdminService sectionService) : Controller
  {
      public async Task<IActionResult> Index(int projectId)
      {
          var sections = await sectionService.GetSectionsAsync(projectId);
          ViewBag.ProjectId = projectId;
          return View(sections);
      }

      public IActionResult Create(int projectId)
      {
          ViewBag.ProjectId = projectId;
          return View(new SectionFormViewModel());
      }

      [HttpPost]
      public async Task<IActionResult> Create(int projectId, SectionFormViewModel
  model)
      {
          if (!ModelState.IsValid)
          {
              ViewBag.ProjectId = projectId;
              return View(model);
          }

          await sectionService.CreateSectionAsync(projectId, model.Name,
  model.Description);
          return RedirectToAction(nameof(Index), new { projectId });
      }

      public async Task<IActionResult> Edit(int projectId, int sectionId)
      {
          var sections = await sectionService.GetSectionsAsync(projectId);
          var section = sections.FirstOrDefault(s => s.SectionId == sectionId);
          if (section is null)
              return NotFound();

          var model = new SectionFormViewModel
          {
              SectionId = sectionId,
              Name = section.Name,
              Description = section.Description
          };
          ViewBag.ProjectId = projectId;
          return View(model);
      }

      [HttpPost]
      public async Task<IActionResult> Edit(int projectId, int sectionId,
  SectionFormViewModel model)
      {
          if (!ModelState.IsValid)
          {
              ViewBag.ProjectId = projectId;
              return View(model);
          }

          await sectionService.UpdateSectionAsync(projectId, sectionId, model.Name,
   model.Description);
          return RedirectToAction(nameof(Index), new { projectId });
      }

      [HttpPost]
      public async Task<IActionResult> Delete(int projectId, int sectionId)
      {
          await sectionService.DeleteSectionAsync(projectId, sectionId);
          return RedirectToAction(nameof(Index), new { projectId });
      }
  }