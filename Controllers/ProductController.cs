/*using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Threading.Tasks;
using static AspnetCoreMvcFull.Models.Firestore;

namespace AspnetCoreMvcFull.Controllers
{
  public class ProductController : Controller
  {
    private readonly ProductService _productService;
    private readonly BrandService _brandService;
    private readonly CategoryService _categoryService;

    public ProductController(
        ProductService productService,
        BrandService brandService,
        CategoryService categoryService)
    {
      _productService = productService;
      _brandService = brandService;
      _categoryService = categoryService;
    }

    // GET: Product/List
    public IActionResult List()
    {
      return View();
    }

    // GET: Product/Add
    public async Task<IActionResult> ProductAdd()
    {
      // Load brands for dropdown
      var brands = await _brandService.GetAllBrandsAsync();
      ViewBag.Brands = new SelectList(brands, "BrandId", "Name");

      // Load categories for dropdown
      var categories = await _categoryService.GetAllCategoriesAsync();
      ViewBag.Categories = new SelectList(categories, "FirestoreId", "Name");

      return View();
    }

    // POST: Product/Add
    [HttpPost]
    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> ProductAdd(DTO.Product product, IFormFile Image)
    {
      if (ModelState.IsValid)
      {
        try
        {
          // If brand is selected, get the commission rate from the brand
          if (!string.IsNullOrEmpty(product.BrandId))
          {
            var brand = await _brandService.GetByIdAsync(product.BrandId);
            if (brand != null)
            {
              product.Commission = brand.CommissionRate;
            }
          }

          await _productService.CreateProductFromForm(product, Image);

          TempData["SuccessMessage"] = "Product created successfully!";
          return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"Error creating product: {ex.Message}");
        }
      }

      // If we got this far, something failed, redisplay form
      var brands = await _brandService.GetAllBrandsAsync();
      ViewBag.Brands = new SelectList(brands, "BrandId", "Name", product.BrandId);

      var categories = await _categoryService.GetAllCategoriesAsync();
      ViewBag.Categories = new SelectList(categories, "FirestoreId", "Name", product.CategoryId);

      return View(product);
    }


    // GET: Product/Edit/5
    public async Task<IActionResult> ProductEdit(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      var product = await _productService.GetProductById(id);
      if (product == null)
      {
        return NotFound();
      }

      var brands = await _brandService.GetAllBrandsAsync();
      ViewBag.Brands = new SelectList(brands, "BrandId", "Name", product.BrandId);

      var categories = await _categoryService.GetAllCategoriesAsync();
      ViewBag.Categories = new SelectList(categories, "FirestoreId", "Name", product.CategoryId);

      return View(product);
    }

    // POST: Product/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProductEdit(string id, DTO.Product product)
    {
      if (id != product.ProductId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          // Update timestamp
          product.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

          await _productService.UpdateProduct(product);

          TempData["SuccessMessage"] = "Product updated successfully!";
          return RedirectToAction(nameof(List));
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"Error updating product: {ex.Message}");
        }
      }

      var brands = await _brandService.GetAllBrandsAsync();
      ViewBag.Brands = new SelectList(brands, "BrandId", "Name", product.BrandId);

      var categories = await _categoryService.GetAllCategoriesAsync();
      ViewBag.Categories = new SelectList(categories, "FirestoreId", "Name", product.CategoryId);

      return View(product);
    }

    // GET: Product/Delete/5
    public async Task<IActionResult> ProductDelete(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      var product = await _productService.GetProductById(id);
      if (product == null) { 
        return NotFound();
    } return View(product);
   }

        // POST: Product/Delete/5
        [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
      await _productService.DeleteProduct(id);

      TempData["SuccessMessage"] = "Product deleted successfully!";
      return RedirectToAction(nameof(List));
    }

    // API endpoint to get products for DataTables
    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
      try
      {
        var products = await _productService.GetAllProducts();

        // Fetch category and brand names for display
        foreach (var product in products)
        {
          if (!string.IsNullOrEmpty(product.CategoryId))
          {
            var category = await _categoryService.GetCategoryByIdAsync(product.CategoryId);
            if (category != null)
            {
              // Store the category name in a temporary property for display
              product.CategoryId = category.Name;
            }
          }

          if (!string.IsNullOrEmpty(product.BrandId))
          {
            var brand = await _brandService.GetByIdAsync(product.BrandId);
            if (brand != null)
            {
              // Store the brand name in a temporary property for display
              product.BrandId = brand.Name;
            }
          }
        }

        return Json(new { data = products });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }
  }
}
*/
