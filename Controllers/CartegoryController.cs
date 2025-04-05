using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AspnetCoreMvcFull.Controllers
{
  public class CategoryController : Controller
  {
    private readonly CategoryService _categoryService;

    public CategoryController(CategoryService categoryService)
    {
      _categoryService = categoryService;
    }

    [HttpGet]
    public IActionResult Index()
    {
      return View("~/Views/Ecommerce/ProductCategoryList.cshtml");
    }

    [HttpGet]
    public async Task<IActionResult> GetAllCategories()
    {
      try
      {
        var categories = await _categoryService.GetAllCategoriesAsync();
        var formattedCategories = categories.Select(c => new {
          id = c.CategoryId,
          categories = c.Name,
          category_detail = c.Description,
          total_products = "0", // Placeholder - can be updated with actual count
          total_earnings = "$0", // Placeholder - can be updated with actual earnings
          cat_image = "" // Placeholder for image
        }).ToList();

        return Json(new { data = formattedCategories });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    [HttpGet]
    public async Task<IActionResult> GetCategory(string id)
    {
      try
      {
        var category = await _categoryService.GetCategoryByIdAsync(id);
        if (category == null)
        {
          return NotFound(new { success = false, message = "Category not found" });
        }

        return Json(new
        {
          success = true,
          category = new
          {
            id = category.CategoryId,
            name = category.Name,
            description = category.Description
          }
        });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, error = ex.Message });
      }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Category category)
    {
      try
      {
        var categoryId = await _categoryService.CreateCategoryAsync(category);
        return Json(new { success = true, id = categoryId });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, error = ex.Message });
      }
    }

    [HttpPut]
    public async Task<IActionResult> Update(string id, [FromBody] Category category)
    {
      try
      {
        await _categoryService.UpdateCategoryAsync(id, category);
        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, error = ex.Message });
      }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(string id)
    {
      try
      {
        await _categoryService.DeleteCategoryAsync(id);
        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { success = false, error = ex.Message });
      }
    }
  }
}
