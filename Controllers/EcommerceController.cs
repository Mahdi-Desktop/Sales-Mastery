using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.DTO;
using Microsoft.Extensions.Caching.Memory;

namespace AspnetCoreMvcFull.Controllers;

public class EcommerceController : Controller
{
  private readonly ProductService _productService;
  private readonly BrandService _brandService;
  private readonly CategoryService _categoryService;
  private readonly IMemoryCache _cache;

  public EcommerceController(
      ProductService productService,
      BrandService brandService,
      CategoryService categoryService,
       IMemoryCache cache)
  {
    _productService = productService;
    _brandService = brandService;
    _categoryService = categoryService;
    _cache = cache;
  }

  /* public async Task<IActionResult> GetProductsJson()
   {
     try
     {
       var products = await _productService.GetAllProducts();

       // Debug information
       System.Diagnostics.Debug.WriteLine($"API - Products count: {products.Count}");

       // Get categories directly without caching
       var categories = await _categoryService.GetAllCategoriesAsync();

       // Get brands directly without caching
       var brands = await _brandService.GetAllBrandsAsync();

       var result = products.Select(p => new {
         productId = p.ProductId,
         name = p.Name,  // Ensure these match exactly
         sku = p.SKU,
         categoryId = p.CategoryId,
         categoryName = categories.FirstOrDefault(c => c.CategoryId == p.CategoryId)?.Name ?? "Uncategorized",
         brandId = p.BrandId,
         brandName = brands.FirstOrDefault(b => b.BrandId == p.BrandId)?.Name ?? "Unbranded",
         price = p.Price,
         discount = p.Discount,
         stock = p.Stock, // Convert string to int
         Commission = brands.FirstOrDefault(b => b.BrandId == p.BrandId)?.CommissionRate ?? p.Commission,
         image = p.Image != null ? p.Image : new List<string>() // Ensure image is never null
       }).ToList();

       // Debug the result
       System.Diagnostics.Debug.WriteLine($"API - Result count: {result.Count()}");

       return Json(result);
     }
     catch (Exception ex)
     {
       System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
       return Json(new { error = ex.Message });
     }
   }*/
  [HttpGet]
  public async Task<IActionResult> MigrateProductReferences()
  {
    try
    {
      await _productService.MigrateProductReferences();
      return Ok(new { success = true, message = "Product references migrated successfully" });
    }
    catch (Exception ex)
    {
      return StatusCode(500, new { error = ex.Message });
    }
  }

  public async Task<IActionResult> GetProductsJson()
  {
    try
    {
      var products = await _productService.GetAllProducts();
      System.Diagnostics.Debug.WriteLine($"API - Products count: {products.Count}");

      var result = products.Select(p => new {
        productId = p.ProductId,
        name = p.Name,
        sku = p.SKU,
        price = p.Price,
        discount = p.Discount,
        stock = p.Stock,
        commission = p.Commission,
        categoryId = p.CategoryId,
        brandId = p.BrandId,
        // Improved image URL conversion
        image = p.Image != null && p.Image.Any()
              ? p.Image.Select(img => ConvertStorageUrlToHttpUrl(img)).Where(url => !string.IsNullOrEmpty(url)).ToList()
              : new List<string>() { "/assets/img/elements/1.jpg" } // Default image if none available
      }).ToList();

      System.Diagnostics.Debug.WriteLine($"API - Result count: {result.Count()}");
      return Json(result);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
      return Json(new { error = ex.Message });
    }
  }

  // Improved URL conversion method
  private string ConvertStorageUrlToHttpUrl(string storageUrl)
  {
    if (string.IsNullOrEmpty(storageUrl)) return "/assets/img/elements/1.jpg";

    // Check if it's already an HTTP URL
    if (storageUrl.StartsWith("http")) return storageUrl;

    // Convert gs:// URL to HTTP URL
    if (storageUrl.StartsWith("gs://"))
    {
      try
      {
        // Extract bucket and path
        var gsPath = storageUrl.Substring(5); // Remove "gs://"
        var firstSlashIndex = gsPath.IndexOf('/');

        if (firstSlashIndex > 0)
        {
          var bucket = gsPath.Substring(0, firstSlashIndex);
          var objectPath = gsPath.Substring(firstSlashIndex + 1);

          // Log the conversion for debugging
          System.Diagnostics.Debug.WriteLine($"Converting: {storageUrl}");
          System.Diagnostics.Debug.WriteLine($"Bucket: {bucket}");
          System.Diagnostics.Debug.WriteLine($"Path: {objectPath}");

          // Construct Firebase Storage download URL
          var downloadUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{Uri.EscapeDataString(objectPath)}?alt=media";
          System.Diagnostics.Debug.WriteLine($"Download URL: {downloadUrl}");

          return downloadUrl;
        }
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error converting URL: {ex.Message}");
      }
    }

    // If we can't convert it or there's an error, return a default image path
    return "/assets/img/elements/1.jpg";
  }


  // Helper method to convert Firebase Storage URLs
  /*  private string ConvertStorageUrlToHttpUrl(string storageUrl)
    {
      if (string.IsNullOrEmpty(storageUrl)) return "";

      // Check if it's already an HTTP URL
      if (storageUrl.StartsWith("http")) return storageUrl;

      // Convert gs:// URL to HTTP URL
      if (storageUrl.StartsWith("gs://"))
      {
        try
        {
          // Extract bucket and path
          var gsPath = storageUrl.Substring(5); // Remove "gs://"
                                                // Extract bucket and path
          var firstSlashIndex = gsPath.IndexOf('/');

          if (firstSlashIndex > 0)
          {
            var bucket = gsPath.Substring(0, firstSlashIndex);
            var objectPath = gsPath.Substring(firstSlashIndex + 1);

            // Log the conversion for debugging
            System.Diagnostics.Debug.WriteLine($"Converting: {storageUrl}");
            System.Diagnostics.Debug.WriteLine($"Bucket: {bucket}");
            System.Diagnostics.Debug.WriteLine($"Path: {objectPath}");

            // Construct Firebase Storage download URL
            var downloadUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{Uri.EscapeDataString(objectPath)}?alt=media";
            System.Diagnostics.Debug.WriteLine($"Download URL: {downloadUrl}");

            return downloadUrl;
          }
        }
        catch (Exception ex)
        {
          System.Diagnostics.Debug.WriteLine($"Error converting URL: {ex.Message}");
        }
      }

      // If we can't convert it or there's an error, return a default image path
      return "/assets/img/elements/1.jpg";
    }
  */
  /*  public async Task<IActionResult> ProductAdd()
    {
      try
      {
        var brands = await _brandService.GetAllBrandsAsync();
        var categories = await _categoryService.GetAllCategoriesAsync();

        // Debug information to verify data is being loaded
        System.Diagnostics.Debug.WriteLine($"Loaded {brands.Count} brands");
        System.Diagnostics.Debug.WriteLine($"Loaded {categories.Count} categories");

        ViewBag.Brands = brands;
        ViewBag.Categories = categories;
        return View();
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error loading product form: {ex.Message}");
        TempData["ErrorMessage"] = "Error loading product form: " + ex.Message;
        return RedirectToAction("ProductList");
      }
    }

    [HttpPost]
    public async Task<IActionResult> ProductAdd(Product product, string imagesJson)
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
              product.Commission = (int)brand.CommissionRate;
            }
          }

          // Set timestamps
          product.CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);
          product.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

          await _productService.CreateProductFromForm(product, imagesJson);

          TempData["SuccessMessage"] = "Product created successfully!";
          return RedirectToAction("ProductList");
        }
        catch (Exception ex)
        {
          ModelState.AddModelError("", $"Error creating product: {ex.Message}");
        }
      }

      // If we got this far, something failed, redisplay form
      ViewBag.Brands = await _brandService.GetAllBrandsAsync();
      ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();

      return View(product);
    }*/
  public async Task<IActionResult> ProductAdd()
  {
    try
    {
      var brands = await _brandService.GetAllBrandsAsync();
      var categories = await _categoryService.GetAllCategoriesAsync();

      // Debug information to verify data is being loaded
      System.Diagnostics.Debug.WriteLine($"Loaded {brands.Count} brands");
      foreach (var brand in brands)
      {
        System.Diagnostics.Debug.WriteLine($"Brand: {brand.BrandId} - {brand.Name}");
      }

      System.Diagnostics.Debug.WriteLine($"Loaded {categories.Count} categories");
      foreach (var category in categories)
      {
        System.Diagnostics.Debug.WriteLine($"Category: {category.CategoryId} - {category.Name}");
      }

      ViewBag.Brands = brands;
      ViewBag.Categories = categories;
      return View();
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Error loading product form: {ex.Message}");
      TempData["ErrorMessage"] = "Error loading product form: " + ex.Message;
      return RedirectToAction("ProductList");
    }
  }

  [HttpPost]
  public async Task<IActionResult> ProductAdd(Product product, string imagesJson)
  {
    try
    {
      System.Diagnostics.Debug.WriteLine($"Received product data: Name={product.Name}, BrandId={product.BrandId}, CategoryId={product.CategoryId}, Stock={product.Stock}");
      System.Diagnostics.Debug.WriteLine($"Images JSON: {imagesJson}");

      if (ModelState.IsValid)
      {
        // Ensure stock is set properly
        if (product.Stock <= 0)
        {
          product.Stock = 0;
        }

        // If brand is selected, get the commission rate from the brand
        if (!string.IsNullOrEmpty(product.BrandId))
        {
          var brand = await _brandService.GetByIdAsync(product.BrandId);
          if (brand != null)
          {
            product.Commission = (int)brand.CommissionRate;
            System.Diagnostics.Debug.WriteLine($"Set commission to {product.Commission} from brand {brand.Name}");
          }
        }

        // Set timestamps
        product.CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);
        product.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

        await _productService.CreateProductFromForm(product, imagesJson);

        TempData["SuccessMessage"] = "Product created successfully!";
        return RedirectToAction("ProductList");
      }
      else
      {
        // Log validation errors
        foreach (var modelState in ModelState.Values)
        {
          foreach (var error in modelState.Errors)
          {
            System.Diagnostics.Debug.WriteLine($"Validation error: {error.ErrorMessage}");
          }
        }
      }
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"Error creating product: {ex.Message}");
      ModelState.AddModelError("", $"Error creating product: {ex.Message}");
    }

    // If we got this far, something failed, redisplay form
    ViewBag.Brands = await _brandService.GetAllBrandsAsync();
    ViewBag.Categories = await _categoryService.GetAllCategoriesAsync();

    return View(product);
  }

  public async Task<IActionResult> ProductList()
  {
    var products = await _productService.GetAllProducts();

    // Calculate statistics for the dashboard
    ViewBag.TotalProducts = products.Count;
    ViewBag.InStockProducts = products.Count(p => p.Stock > 0);
    ViewBag.OutOfStockProducts = products.Count(p => p.Stock <= 0);
    ViewBag.TotalValue = products.Sum(p => p.Price * p.Stock);

    return View(products);
  }

  [HttpGet]
  public async Task<IActionResult> GetCategoriesByBrand(string brandId)
  {
    try
    {
      System.Diagnostics.Debug.WriteLine($"Getting categories for brand: {brandId}");

      if (string.IsNullOrEmpty(brandId))
      {
        return Json(new List<object>());
      }

      // Get all categories
      var allCategories = await _categoryService.GetAllCategoriesAsync();
      System.Diagnostics.Debug.WriteLine($"Retrieved {allCategories.Count} total categories");

      // Filter categories by brand
      var filteredCategories = allCategories
          .Where(c => c.BrandId == brandId)
          .ToList();

      System.Diagnostics.Debug.WriteLine($"Filtered to {filteredCategories.Count} categories for brand {brandId}");

      // If no categories found for this brand, return all categories
      if (filteredCategories.Count == 0)
      {
        System.Diagnostics.Debug.WriteLine("No categories found for this brand, returning all categories");
        var result = allCategories.Select(c => new {
          categoryId = c.CategoryId,
          name = c.Name
        });
        return Json(result);
      }

      var filteredResult = filteredCategories.Select(c => new {
        categoryId = c.CategoryId,
        name = c.Name
      });

      return Json(filteredResult);
    }
    catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
      return Json(new { error = ex.Message });
    }
  }

  public IActionResult Dashboard() => View();

  public IActionResult ProductCategoryList() => View();
  public IActionResult CustomerAll() => View();
  public IActionResult CustomerDetailsBilling() => View();
  public IActionResult CustomerDetailsNotifications() => View();
  public IActionResult CustomerDetailsOverview() => View();
  public IActionResult CustomerDetailsSecurity() => View();

  public IActionResult OrderList() => View();
  public IActionResult OrderDetails() => View();
  public IActionResult SettingsCheckout() => View();
  public IActionResult SettingsLocations() => View();
  public IActionResult SettingsShipping() => View();
  public IActionResult SettingsPayments() => View();
  public IActionResult SettingsNotifications() => View();
  public IActionResult SettingsStoreDetails() => View();
  public IActionResult Referrals() => View();
  public IActionResult Reviews() => View();
}
