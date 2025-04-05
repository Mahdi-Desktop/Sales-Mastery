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

    public async Task<Product> GetProductById(string productId)
    {
      try
      {
        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(productId);
        DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

        return snapshot.Exists ? snapshot.ConvertTo<Product>() : null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting product with ID {productId}");
        throw;
      }
    }

    /*    public async Task<List<Product>> GetAllProducts()
        {
          try
          {
            QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
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
            _logger.LogError(ex, "Error getting all products");
            throw;
          }
        }
    */
    /*    public async Task<List<Product>> GetAllProducts()
        {
          try
          {
            QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
            List<Product> products = new();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
              if (document.Exists)
              {
                try
                {
                  // Log the raw document data for debugging
                  var rawData = document.ToDictionary();
                  _logger.LogInformation($"Raw document data for {document.Id}: {System.Text.Json.JsonSerializer.Serialize(rawData)}");

                  // Use Firestore's built-in conversion
                  Product product = document.ConvertTo<Product>();
                  product.ProductId = document.Id;

                  products.Add(product);
                }
                catch (Exception conversionEx)
                {
                  _logger.LogError(conversionEx, $"Error converting document {document.Id} to Product");
                  // Continue with next document instead of failing the entire operation
                }
              }
            }

            return products;
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error getting all products");
            throw;
          }
        }*/
    /*    public async Task<List<Product>> GetAllProducts()
        {
          try
          {
            QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
            List<Product> products = new();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
              if (document.Exists)
              {
                // Use Firestore's built-in conversion with proper attribute mapping
                Product product = document.ConvertTo<Product>();

                // Ensure the document ID is set as ProductId
                product.ProductId = document.Id;

                // Handle any special cases if needed
                // For example, if Image is stored differently than expected
                if (document.ContainsField("Image") && document.GetValue<object>("Image") is IEnumerable<object> imageList)
                {
                  product.Image = imageList.Select(img => img.ToString()).ToList();
                }

                products.Add(product);
              }
            }

            return products;
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error getting all products");
            throw;
          }
        }*/
    public async Task<List<Product>> GetAllProducts()
    {
      try
      {
        _logger.LogInformation("Getting all products");
        QuerySnapshot snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
        List<Product> products = new();

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
          if (document.Exists)
          {
            try
            {
              // Log the raw document data for debugging
              var rawData = document.ToDictionary();
              _logger.LogDebug($"Raw document data for {document.Id}: {System.Text.Json.JsonSerializer.Serialize(rawData)}");

              // Use Firestore's built-in conversion
              Product product = document.ConvertTo<Product>();

              // Ensure the document ID is set as ProductId
              product.ProductId = document.Id;

              // Ensure Image is never null
              if (product.Image == null)
              {
                product.Image = new List<string>();
              }

              // Ensure numeric fields have valid values
              if (product.Stock < 0) product.Stock = 0;
              if (product.Price < 0) product.Price = 0;
              if (product.Discount < 0) product.Discount = 0;

              _logger.LogDebug($"Converted product: ID={product.ProductId}, Name={product.Name}, " +
                              $"BrandId={product.BrandId}, CategoryId={product.CategoryId}, " +
                              $"Stock={product.Stock}, Price={product.Price}");

              products.Add(product);
            }
            catch (Exception conversionEx)
            {
              _logger.LogError(conversionEx, $"Error converting document {document.Id} to Product");
              // Continue with next document instead of failing the entire operation
            }
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

    [HttpGet]
    // Remove this method from ProductService.cs (lines 212-258):
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
    public async Task CreateProductFromForm(Product product, string imagesJson)
    {
      try
      {
        // Parse the images JSON string
        if (!string.IsNullOrEmpty(imagesJson))
        {
          product.Image = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson);
        }

        // Generate a new ID if not provided
        if (string.IsNullOrEmpty(product.ProductId))
        {
          product.ProductId = Guid.NewGuid().ToString();
        }

        // Set timestamps
        product.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        DocumentReference docRef = _firestoreDb.Collection(CollectionName).Document(product.ProductId);
        await docRef.SetAsync(product);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating product from form");
        throw;
      }
    }


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

/*    private string GetStringValue(Dictionary<string, object> data, string key)
{
    return data.TryGetValue(key, out var value) && value != null ? value.ToString() : null;
}

    private int GetIntValue(Dictionary<string, object> data, string key, int defaultValue = 0)
    {
        if (data.TryGetValue(key, out var value) && value != null)
        {
            if (value is long longValue)
                return (int)longValue;
            if (value is int intValue)
                return intValue;
            if (value is double doubleValue)
                return (int)doubleValue;
            if (int.TryParse(value.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private decimal GetDecimalValue(Dictionary<string, object> data, string key, decimal defaultValue = 0)
    {
        if (data.TryGetValue(key, out var value) && value != null)
        {
            if (value is double doubleValue)
                return (decimal)doubleValue;
            if (value is long longValue)
                return (decimal)longValue;
            if (value is int intValue)
                return (decimal)intValue;
            if (decimal.TryParse(value.ToString(), out var result))
                return result;
        }
        return defaultValue;
    }

    private decimal? GetNullableDecimalValue(Dictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value != null)
        {
            if (value is double doubleValue)
                return (decimal)doubleValue;
            if (value is long longValue)
                return (decimal)longValue;
            if (value is int intValue)
                return (decimal)intValue;
            if (decimal.TryParse(value.ToString(), out var result))
                return result;
        }
        return null;
    }

    private Timestamp GetTimestampValue(Dictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value is Timestamp timestamp)
            return timestamp;
        return Timestamp.FromDateTime(DateTime.UtcNow);
    }

    private List<T> GetListValue<T>(Dictionary<string, object> data, string key)
    {
        if (data.TryGetValue(key, out var value) && value is IEnumerable<object> list)
        {
            return list.Select(item => (T)Convert.ChangeType(item, typeof(T))).ToList();
        }
        return new List<T>();
    }*/

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


  }
}
