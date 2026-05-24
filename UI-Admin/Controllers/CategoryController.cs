
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;
  using SynapseEnqueteManagement.UI.Admin.Filters;
  using SynapseEnqueteManagement.UI.Admin.Models.Category;
  using SynapseEnqueteManagement.UI.Admin.Services.Category;

  namespace SynapseEnqueteManagement.UI.Admin.Controllers;

  [Authorize(Roles = "ProjectPlatformAdmin")]
  [RequireProjectPlatformOwnership]
  public class CategoryController(CategoryAdminService categoryService) :
  Controller
  {
      public async Task<IActionResult> Index(int projectId)
      {
          var categories = await categoryService.GetCategoriesAsync(projectId);
          ViewBag.ProjectId = projectId;
          return View(categories);
      }

      public IActionResult Create(int projectId)
      {
          ViewBag.ProjectId = projectId;
          return View(new CategoryFormViewModel());
      }

      [HttpPost]
      public async Task<IActionResult> Create(int projectId, CategoryFormViewModel
  model)
      {
          if (!ModelState.IsValid)
          {
              ViewBag.ProjectId = projectId;
              return View(model);
          }

          await categoryService.CreateCategoryAsync(projectId, model.Name,
  model.Description);
          return RedirectToAction(nameof(Index), new { projectId });
      }

      public async Task<IActionResult> Edit(int projectId, int categoryId)
      {
          var categories = await categoryService.GetCategoriesAsync(projectId);
          var category = categories.FirstOrDefault(c => c.CategoryId ==
  categoryId);
          if (category is null)
              return NotFound();

          var model = new CategoryFormViewModel
          {
              CategoryId = categoryId,
              Name = category.Name,
              Description = category.Description
          };
          ViewBag.ProjectId = projectId;
          return View(model);
      }

      [HttpPost]
      public async Task<IActionResult> Edit(int projectId, int categoryId,
  CategoryFormViewModel model)
      {
          if (!ModelState.IsValid)
          {
              ViewBag.ProjectId = projectId;
              return View(model);
          }

          await categoryService.UpdateCategoryAsync(projectId, categoryId,
  model.Name, model.Description);
          return RedirectToAction(nameof(Index), new { projectId });
      }

      [HttpPost]
      public async Task<IActionResult> Delete(int projectId, int categoryId)
      {
          await categoryService.DeleteCategoryAsync(projectId, categoryId);
          return RedirectToAction(nameof(Index), new { projectId });
      }
  }