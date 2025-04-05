using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class CategoryService : FirestoreService<Category>
  {
      private readonly ILogger<CategoryService> _logger;
      private const string CollectionName = "categories";
    public CategoryService(IConfiguration configuration, ILogger<CategoryService> logger)
        : base(configuration, CollectionName)
    {
      _logger = logger;
    }

    // If there's no method to override, use the constant directly in your methods
    public async Task<List<Category>> GetAllCategoriesAsync()
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();

        var categories = new List<Category>();
        foreach (var document in snapshot.Documents)
        {
          var category = document.ConvertTo<Category>();
          category.CategoryId = document.Id;
          categories.Add(category);
        }

        return categories;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all categories");
        throw;
      }
    }

    public async Task<Category> GetCategoryByIdAsync(string id)
    {
      try
      {
        var docRef = _firestoreDb.Collection(CollectionName).Document(id);
        var snapshot = await docRef.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
          return null;
        }

        var category = snapshot.ConvertTo<Category>();
        category.CategoryId = snapshot.Id;
        return category;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting category with ID {id}");
        throw;
      }
    }

    public async Task<string> CreateCategoryAsync(Category category)
    {
      try
      {
        category.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        category.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        var docRef = _firestoreDb.Collection(CollectionName).Document();
        string newId = docRef.Id;

        var categoryData = new Dictionary<string, object>
        {
          { nameof(Category.Name), category.Name },
          { nameof(Category.Description), category.Description },
          { nameof(Category.CreatedAt), category.CreatedAt },
          { nameof(Category.UpdatedAt), category.UpdatedAt }
        };

        if (!string.IsNullOrEmpty(category.BrandId))
        {
          categoryData.Add(nameof(Category.BrandId), category.BrandId);
        }

        await docRef.SetAsync(categoryData);
        return newId;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating category");
        throw;
      }
    }

    public async Task UpdateCategoryAsync(string id, Category category)
    {
      try
      {
        category.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        var docRef = _firestoreDb.Collection(CollectionName).Document(id);

        var updates = new Dictionary<string, object>
        {
          { nameof(Category.Name), category.Name },
          { nameof(Category.Description), category.Description },
          { nameof(Category.UpdatedAt), category.UpdatedAt }
        };

        if (!string.IsNullOrEmpty(category.BrandId))
        {
          updates.Add(nameof(Category.BrandId), category.BrandId);
        }

        await docRef.UpdateAsync(updates);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating category with ID {id}");
        throw;
      }
    }

    public async Task DeleteCategoryAsync(string id)
    {
      try
      {
        var docRef = _firestoreDb.Collection(CollectionName).Document(id);
        await docRef.DeleteAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting category with ID {id}");
        throw;
      }
    }
  }
}
