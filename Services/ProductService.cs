using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class ProductService : FirestoreService<Product>
  {
    private readonly ILogger<ProductService> _logger;
    private const string CollectionName = "products";
    private readonly IWebHostEnvironment _environment;
    private readonly CategoryService _categoryService;
    private readonly BrandService _brandService;

    public ProductService(
        IConfiguration configuration,
        ILogger<ProductService> logger,
        IWebHostEnvironment environment,
        CategoryService categoryService,
        BrandService brandService)
        : base(configuration, CollectionName)
    {
      _logger = logger;
      _environment = environment;
      _categoryService = categoryService;
      _brandService = brandService;
    }

    // Helper methods to safely extract values from dictionary
    private string GetString(Dictionary<string, object> data, string key)
    {
      return data.ContainsKey(key) ? data[key]?.ToString() : null;
    }

    private int GetInt(Dictionary<string, object> data, string key)
    {
      if (!data.ContainsKey(key) || data[key] == null)
        return 0;

      var value = data[key];

      switch (value)
      {
        case int i:
          return i;
        case long l:
          return (int)l;
        case double d:
          return (int)d;
        case float f:
          return (int)f;
        case decimal dec:
          return (int)dec;
        case string s when int.TryParse(s, out var result):
          return result;
        default:
          return 0;
      }
    }

    private double GetDouble(Dictionary<string, object> data, string key)
    {
      if (!data.ContainsKey(key) || data[key] == null)
        return 0.0;

      var value = data[key];

      switch (value)
      {
        case double d:
          return d;
        case float f:
          return f;
        case long l:
          return (double)l;
        case int i:
          return (double)i;
        case decimal dec:
          return (double)dec;
        case string s when double.TryParse(s, out var result):
          return result;
        default:
          return 0.0;
      }
    }


    private Timestamp? GetTimestamp(Dictionary<string, object> data, string key)
    {
      if (!data.ContainsKey(key)) return null;

      if (data[key] is Timestamp ts)
        return ts;

      return null;
    }
    public async Task<List<Product>> GetProductsAsync(
           string searchQuery = null,
           decimal? minPrice = null,
           decimal? maxPrice = null,
           string category = null,
           bool availableOnly = true,
           string sortBy = "name",
           bool ascending = true)
    {
      try
      {
        // Start with the collection reference
        Query query = _collection;

        // Filter by availability if needed
        if (availableOnly)
        {
          query = query.WhereEqualTo("IsAvailable", true);
        }

        // Filter by price range
        if (minPrice.HasValue)
        {
          query = query.WhereGreaterThanOrEqualTo("Price", minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
          query = query.WhereLessThanOrEqualTo("Price", maxPrice.Value);
        }

        // Get all products that match the filters
        var snapshot = await query.GetSnapshotAsync();
        var products = snapshot.Documents.Select(doc => doc.ConvertTo<Product>()).ToList();

        // Apply additional filtering that can't be done at the query level
        if (!string.IsNullOrEmpty(searchQuery))
        {
          searchQuery = searchQuery.ToLower();
          products = products.Where(p =>
              p.Name.ToLower().Contains(searchQuery) ||
              p.Description.ToLower().Contains(searchQuery)).ToList();
        }

        if (!string.IsNullOrEmpty(category))
        {
          var filteredProducts = new List<Product>();
          foreach (var p in products)
          {
            var categoryObj = await _categoryService.GetCategoryByIdAsync(p.CategoryId);
            if (categoryObj != null && categoryObj.Name == category) // Compare with the name property
            {
              filteredProducts.Add(p);
            }
          }
          products = filteredProducts;
        }

        // Sort products
        switch (sortBy.ToLower())
        {
          case "price":
            products = ascending
                ? products.OrderBy(p => p.Price).ToList()
                : products.OrderByDescending(p => p.Price).ToList();
            break;
          case "newest":
            products = products.OrderByDescending(p => p.CreatedAt).ToList();
            break;
          case "popularity":
            // If you had a property for popularity, you'd sort by it here
            break;
          default: // Default to sorting by name
            products = ascending
                ? products.OrderBy(p => p.Name).ToList()
                : products.OrderByDescending(p => p.Name).ToList();
            break;
        }

        return products;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting products with filters");
        throw;
      }
    }
    /*    public async Task<Product> GetProductById(string productId)
        {
          try
          {
            DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(productId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
              return null;  // This is returning null if the document doesn't exist

            // Convert document to Product
            var product = snapshot.ConvertTo<Product>();
            product.ProductId = snapshot.Id;

            // Convert gs:// URLs to https:// URLs
            if (product.Image != null && product.Image.Any())
            {
              var convertedUrls = new List<string>();
              foreach (var imageUrl in product.Image)
              {
                if (imageUrl.StartsWith("gs://"))
                {
                  // Extract and convert gs:// URL to HTTPS URL
                  string bucket = imageUrl.Substring(5).Split('/')[0];
                  string objectPath = imageUrl.Substring(5 + bucket.Length + 1);
                  string encodedPath = Uri.EscapeDataString(objectPath);
                  string httpsUrl = $"https://firebasestorage.googleapis.com/v0/b/{bucket}/o/{encodedPath}?alt=media";
                  convertedUrls.Add(httpsUrl);
                }
                else
                {
                  convertedUrls.Add(imageUrl);
                }
              }
              product.Image = convertedUrls;
            }

            // Handle other special fields if needed
            _logger.LogInformation($"Retrieved product: {product.Name}, Image count: {product.Image?.Count ?? 0}");

            return product;
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, $"Error getting product with ID {productId}");
            throw;
          }
        }*/
    // Update the product initialization to handle nullable timestamps
    public async Task<Product> GetProductById(string productId)
    {
      try
      {
        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(productId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
          return null;

        // Get raw dictionary first to handle references
        var data = snapshot.ToDictionary();

        // Create product and set ID
        var product = new Product
        {
          ProductId = snapshot.Id,
          Name = GetString(data, "Name"),
          Description = GetString(data, "Description"),
          SKU = GetString(data, "SKU"),
          Price = GetInt(data, "Price"),
          Stock = GetInt(data, "Stock"),
          Discount = GetInt(data, "Discount"),
          Commission = GetInt(data, "Commission")
        };

        // Handle nullable timestamps
        var createdAt = GetTimestamp(data, "CreatedAt");
        if (createdAt.HasValue)
          product.CreatedAt = createdAt.Value;
        else
          product.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        var updatedAt = GetTimestamp(data, "UpdatedAt");
        if (updatedAt.HasValue)
          product.UpdatedAt = updatedAt.Value;
        else
          product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        // Handle references properly
        if (data.ContainsKey("BrandId"))
        {
          string brandId = data["BrandId"].ToString();
          // Extract ID from reference path if it's a reference
          if (brandId.StartsWith("/brands/"))
          {
            product.BrandId = brandId.Substring("/brands/".Length);
          }
          else
          {
            product.BrandId = brandId;
          }

          // Load commission rate from brand
          await LoadCommissionFromBrand(product);
        }

        if (data.ContainsKey("CategoryId"))
        {
          string categoryId = data["CategoryId"].ToString();
          // Extract ID from reference path if it's a reference
          if (categoryId.StartsWith("/categories/"))
          {
            product.CategoryId = categoryId.Substring("/categories/".Length);
          }
          else
          {
            product.CategoryId = categoryId;
          }
        }

        // Handle image array
        if (data.ContainsKey("Image") && data["Image"] is List<object> imageList)
        {
          product.Image = imageList.Select(img => img.ToString()).ToList();
        }

        _logger.LogInformation($"Retrieved product: {product.Name}, ID: {product.ProductId}, " +
                               $"Price: {product.Price}, Stock: {product.Stock}, BrandId: {product.BrandId}, " +
                               $"Commission: {product.Commission}");
        return product;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting product with ID {productId}");
        throw;
      }
    }

    // Update the GetAllProducts method similarly
    public async Task<List<Product>> GetAllProducts()
    {
      try
      {
        QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
        List<Product> products = new List<Product>();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
          if (document.Exists)
          {
            // Get raw dictionary to handle references
            var data = document.ToDictionary();

            // Create product and set ID
            var product = new Product
            {
              ProductId = document.Id,
              Name = GetString(data, "Name"),
              Description = GetString(data, "Description"),
              SKU = GetString(data, "SKU"),
              Price = GetInt(data, "Price"),
              Stock = GetInt(data, "Stock"),
              Discount = GetInt(data, "Discount"),
              Commission = GetInt(data, "Commission")
            };

            // Handle nullable timestamps
            var createdAt = GetTimestamp(data, "CreatedAt");
            if (createdAt.HasValue)
              product.CreatedAt = createdAt.Value;
            else
              product.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

            var updatedAt = GetTimestamp(data, "UpdatedAt");
            if (updatedAt.HasValue)
              product.UpdatedAt = updatedAt.Value;
            else
              product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

            // Handle references properly
            if (data.ContainsKey("BrandId"))
            {
              string brandId = data["BrandId"].ToString();
              // Extract ID from reference path if it's a reference
              if (brandId.StartsWith("/brands/"))
              {
                product.BrandId = brandId.Substring("/brands/".Length);
              }
              else
              {
                product.BrandId = brandId;
              }

              // Load commission rate from brand
              await LoadCommissionFromBrand(product);
            }

            if (data.ContainsKey("CategoryId"))
            {
              string categoryId = data["CategoryId"].ToString();
              // Extract ID from reference path if it's a reference
              if (categoryId.StartsWith("/categories/"))
              {
                product.CategoryId = categoryId.Substring("/categories/".Length);
              }
              else
              {
                product.CategoryId = categoryId;
              }
            }

            // Handle image array
            if (data.ContainsKey("Image") && data["Image"] is List<object> imageList)
            {
              product.Image = imageList.Select(img => img.ToString()).ToList();
            }

            products.Add(product);
          }
        }

        _logger.LogInformation($"Retrieved {products.Count} products");
        return products;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all products");
        throw;
      }
    }

    public async Task CreateProductFromForm(Product product, string imagesJson)
    {
      try
      {
        _logger.LogInformation($"Creating product: {product.Name}, Images JSON: {imagesJson}");

        // Parse the images JSON string
        if (!string.IsNullOrEmpty(imagesJson))
        {
          try
          {
            product.Image = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson);
            _logger.LogInformation($"Parsed {product.Image?.Count ?? 0} images from JSON");
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error parsing images JSON");
            product.Image = new List<string>(); // Empty list instead of null
          }
        }
        else
        {
          product.Image = new List<string>(); // Initialize to empty list if no images
        }

        // Make sure product has a valid ID
        if (string.IsNullOrEmpty(product.ProductId))
        {
          product.ProductId = Guid.NewGuid().ToString();
        }

        // Ensure all required fields have values
        if (product.Discount == null)
          product.Discount = 0;

        if (product.Commission <= 0)
          product.Commission = 0;

        // Log the product before saving
        _logger.LogInformation($"About to save product: {System.Text.Json.JsonSerializer.Serialize(product)}");

        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(product.ProductId);
        await docRef.SetAsync(product);

        _logger.LogInformation($"Product {product.ProductId} created successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating product from form");
        throw;
      }
    }
    /*    private async Task LoadCommissionFromBrand(Product product)
        {
          try
          {
            if (string.IsNullOrEmpty(product.BrandId))
              return;

            var brandDoc = await _firestoreDb.Collection("brands").Document(product.BrandId).GetSnapshotAsync();
            if (brandDoc.Exists)
            {
              var brand = brandDoc.ConvertTo<Brand>();
              if (brand.CommissionRate > 0)
              {
                product.Commission = brand.CommissionRate;
              }
            }
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, $"Error loading commission from brand for product {product.ProductId}");
          }
        }
    */

    private async Task LoadCommissionFromBrand(Product product)
    {
      try
      {
        if (string.IsNullOrEmpty(product.BrandId))
          return;

        _logger.LogInformation($"Loading commission from brand {product.BrandId} for product {product.ProductId}");

        var brandDoc = await _firestoreDb.Collection("brands").Document(product.BrandId).GetSnapshotAsync();
        if (!brandDoc.Exists)
        {
          _logger.LogWarning($"Brand {product.BrandId} not found");
          return;
        }

        var brandData = brandDoc.ToDictionary();
        _logger.LogDebug($"Raw brand data: {System.Text.Json.JsonSerializer.Serialize(brandData)}");

        if (!brandData.ContainsKey("CommissionRate"))
        {
          _logger.LogWarning($"Brand {product.BrandId} has no CommissionRate field");
          return;
        }

        var commissionRateObj = brandData["CommissionRate"];
        int commissionRate = 0;

        // Handle different numeric types
        if (commissionRateObj is int intRate)
          commissionRate = intRate;
        else if (commissionRateObj is long longRate)
          commissionRate = (int)longRate;
        else if (commissionRateObj is double doubleRate)
          commissionRate = (int)doubleRate;

        if (commissionRate > 0)
        {
          product.Commission = commissionRate;
          _logger.LogInformation($"Set commission to {commissionRate}% from brand {product.BrandId}");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error loading commission from brand for product {product.ProductId}");
      }
    }

    [HttpGet]
    // Add this method to ProductService.cs
    public async Task<List<Category>> GetCategoriesByBrandAsync(string brandId, CategoryService categoryService)
    {
      try
      {
        _logger.LogInformation($"Getting categories for brand: {brandId}");

        if (string.IsNullOrEmpty(brandId))
        {
          return new List<Category>();
        }

        // Get all categories
        var allCategories = await categoryService.GetAllCategoriesAsync();
        _logger.LogInformation($"Retrieved {allCategories.Count} total categories");

        // Filter categories by brand
        var filteredCategories = allCategories
            .Where(c => c.BrandId == brandId)
            .ToList();

        _logger.LogInformation($"Filtered to {filteredCategories.Count} categories for brand {brandId}");

        // If no categories found for this brand, return all categories
        if (filteredCategories.Count == 0)
        {
          _logger.LogInformation("No categories found for this brand, returning all categories");
          return allCategories;
        }

        return filteredCategories;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting categories for brand {brandId}");
        throw;
      }
    }

    public async Task<List<Product>> GetProductsByCategory(string categoryId)
    {
      try
      {
        QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo("CategoryId", categoryId)
            .GetSnapshotAsync();

        List<Product> products = new();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
          if (document.Exists)
          {
            Product product = document.ConvertTo<Product>();
            product.ProductId = document.Id;
            products.Add(product);
          }
        }

        return products;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting products for category {categoryId}");
        throw;
      }
    }

    public async Task<List<Product>> GetProductsByBrand(string brandId)
    {
      try
      {
        QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName)
            .WhereEqualTo("BrandId", brandId)
            .GetSnapshotAsync();

        List<Product> products = new();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
          if (document.Exists)
          {
            Product product = document.ConvertTo<Product>();
            product.ProductId = document.Id;
            products.Add(product);
          }
        }

        return products;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting products for brand {brandId}");
        throw;
      }
    }

    // Add this method to your ProductService class
   /* public async Task CreateProductFromForm(Product product, string imagesJson)
    {
      try
      {
        _logger.LogInformation($"Creating product: {product.Name}, Images JSON: {imagesJson}");

        // Parse the images JSON string
        if (!string.IsNullOrEmpty(imagesJson))
        {
          try
          {
            product.Image = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson);
            _logger.LogInformation($"Parsed {product.Image?.Count ?? 0} images from JSON");
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error parsing images JSON");
            product.Image = new List<string>();
          }
        }

        // Generate a new ID if not provided
        if (string.IsNullOrEmpty(product.ProductId))
        {
          product.ProductId = Guid.NewGuid().ToString();
        }

        // Set timestamps
        product.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        // Log the product before saving
        _logger.LogInformation($"About to save product: {System.Text.Json.JsonSerializer.Serialize(product)}");

        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(product.ProductId);
        await docRef.SetAsync(product);

        _logger.LogInformation($"Product {product.ProductId} created successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating product from form");
        throw;
      }
    }*/


    public async Task UpdateProduct(Product product)
    {
      try
      {
        // Update timestamp
        product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(product.ProductId);
        await docRef.SetAsync(product, SetOptions.MergeAll);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating product with ID {product.ProductId}");
        throw;
      }
    }

    public async Task DeleteProduct(string productId)
    {
      try
      {
        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(productId);
        await docRef.DeleteAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting product with ID {productId}");
        throw;
      }
    }

    public async Task<List<Product>> SearchProducts(string searchTerm)
    {
      try
      {
        // Firestore doesn't support direct text search, so we'll get all products and filter in memory
        // In a production app, you might want to use a dedicated search service like Algolia or Elasticsearch
        var allProducts = await GetAllProducts();

        if (string.IsNullOrWhiteSpace(searchTerm))
          return allProducts;

        searchTerm = searchTerm.ToLowerInvariant();

        return allProducts.Where(p =>
            p.Name?.ToLowerInvariant().Contains(searchTerm) == true ||
            p.Description?.ToLowerInvariant().Contains(searchTerm) == true ||
            p.SKU?.ToLowerInvariant().Contains(searchTerm) == true
        ).ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error searching products for term '{searchTerm}'");
        throw;
      }
    }


    public async Task MigrateProductReferences()
    {
      var products = await GetAllProducts();

      foreach (var product in products)
      {
        bool needsUpdate = false;

        // Clean BrandId
        if (!string.IsNullOrEmpty(product.BrandId) && product.BrandId.Contains("/"))
        {
          product.BrandId = product.BrandId.Split('/').Last();
          needsUpdate = true;
        }

        // Clean CategoryId
        if (!string.IsNullOrEmpty(product.CategoryId) && product.CategoryId.Contains("/"))
        {
          product.CategoryId = product.CategoryId.Split('/').Last();
          needsUpdate = true;
        }

        // Update the product if needed
        if (needsUpdate)
        {
          await UpdateAsync(product.ProductId, product);
        }
      }
    }

    public async Task<List<string>> GetAllCategories()
    {
      try
      {
        // Approach 1: Get categories from a separate collection if you have one
        var categories = new List<string>();
        var query = _firestoreDb.Collection("categories");
        var snapshot = await query.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
          var category = document.ConvertTo<Category>();
          categories.Add(category.Name);
        }

        return categories;

        /* Approach 2: Extract unique categories from products
        var products = await GetAllProducts();
        return products
            .Select(p => p.CategoryId)
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .ToList();
        */
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all categories");
        return new List<string>();
      }
    }


  }
}
