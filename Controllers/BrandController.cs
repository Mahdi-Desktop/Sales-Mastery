using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspnetCoreMvcFull.Controllers
{
  public class BrandController : Controller // Add inheritance from Controller
  {
    private readonly BrandService _brandService;

    public BrandController(BrandService brandService)
    {
      _brandService = brandService;
    }

    [HttpGet("api/brands/{id}")]
    public async Task<IActionResult> GetBrandDetails(string id)
    {
      try
      {
        var brand = await _brandService.GetByIdAsync(id);
        if (brand == null)
        {
          return NotFound();
        }
        return Json(brand);
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    // Add additional methods for brand management
    [HttpGet]
    public async Task<IActionResult> Index()
    {
      var brands = await _brandService.GetAllBrandsAsync();
      return View(brands);
    }

    [HttpGet]
    public IActionResult Create()
    {
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Brand brand)
    {
      if (ModelState.IsValid)
      {
        await _brandService.AddAsync(brand);
        return RedirectToAction(nameof(Index));
      }
      return View(brand);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      var brand = await _brandService.GetByIdAsync(id);
      if (brand == null)
      {
        return NotFound();
      }

      return View(brand);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Brand brand)
    {
      if (id != brand.BrandId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        await _brandService.UpdateAsync(id, brand);
        return RedirectToAction(nameof(Index));
      }
      return View(brand);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      var brand = await _brandService.GetByIdAsync(id);
      if (brand == null)
      {
        return NotFound();
      }

      return View(brand);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
      await _brandService.DeleteAsync(id);
      return RedirectToAction(nameof(Index));
    }
  }
}
