using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services.Interface;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class UserService : IUserService
  {
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<UserService> _logger;
    private const string CollectionName = "Users";

    public UserService(FirestoreDb firestoreDb, ILogger<UserService> logger)
    {
      _firestoreDb = firestoreDb;
      _logger = logger;
    }

    public async Task<User> GetUserByIdAsync(string userId)
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(CollectionName).Document(userId).GetSnapshotAsync();
        if (!snapshot.Exists)
        {
          _logger.LogWarning($"User with ID {userId} not found");
          return null;
        }

        var user = snapshot.ConvertTo<User>();
        user.UserId = snapshot.Id;

        // Load related invoices
        user.InvoiceId = (await _firestoreDb.Collection("Invoices")
            .WhereEqualTo("UserId", userId)
            .GetSnapshotAsync())
            .Documents
            .Select(d => d.Id)
            .ToList();

        return user;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting user by ID {userId}");
        throw;
      }
    }
    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
        return snapshot.Documents.Select(doc =>
        {
          var user = doc.ConvertTo<User>();
          user.UserId = doc.Id;
          return user;
        }).ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all users");
        throw;
      }
    }

    public async Task<string> AddUserAsync(User user)
    {
      try
      {
        user.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        user.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        var docRef = await _firestoreDb.Collection(CollectionName).AddAsync(user);
        return docRef.Id;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error adding user");
        throw;
      }
    }

    public async Task UpdateUserAsync(User user)
    {
      try
      {
        user.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        await _firestoreDb.Collection(CollectionName)
            .Document(user.UserId)
            .SetAsync(user, SetOptions.MergeAll);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating user with ID {user.UserId}");
        throw;
      }
    }

    public async Task DeleteUserAsync(string userId)
    {
      try
      {
        await _firestoreDb.Collection(CollectionName).Document(userId).DeleteAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting user with ID {userId}");
        throw;
      }
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
      try
      {
        var query = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(User.Email), email)
            .Limit(1);

        var snapshot = await query.GetSnapshotAsync();
        var doc = snapshot.Documents.FirstOrDefault();

        if (doc == null)
        {
          _logger.LogWarning($"User with email {email} not found");
          return null;
        }

        var user = doc.ConvertTo<User>();
        user.UserId = doc.Id;
        return user;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting user by email {email}");
        throw;
      }
    }
  }
}
