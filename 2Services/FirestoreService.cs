using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Services.Interface;

namespace AspnetCoreMvcFull.Services
{
  public class FirestoreService<T> : IFirestoreService<T> where T : class
  {
    protected readonly FirestoreDb _firestoreDb;
    protected readonly string _collectionName;
    protected readonly ILogger<FirestoreService<T>> _logger;

    public FirestoreService(FirestoreDb firestoreDb, ILogger<FirestoreService<T>> logger)
    {
      _firestoreDb = firestoreDb ?? throw new ArgumentNullException(nameof(firestoreDb));
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));
      _collectionName = typeof(T).Name + "s";
    }

    public async Task<string> AddAsync(T entity)
    {
      try
      {
        var docRef = await _firestoreDb.Collection(_collectionName).AddAsync(entity);
        return docRef.Id;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error adding {typeof(T).Name} document");
        throw;
      }
    }

    public async Task<T> GetByIdAsync(string id)
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(_collectionName).Document(id).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<T>() : null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting {typeof(T).Name} document with ID {id}");
        throw;
      }
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
        var results = new List<T>();

        foreach (var doc in snapshot.Documents)
        {
          results.Add(doc.ConvertTo<T>());
        }

        return results;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting all {typeof(T).Name} documents");
        throw;
      }
    }

    public async Task UpdateAsync(string id, T entity)
    {
      try
      {
        await _firestoreDb.Collection(_collectionName).Document(id).SetAsync(entity, SetOptions.MergeAll);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating {typeof(T).Name} document with ID {id}");
        throw;
      }
    }

    public async Task DeleteAsync(string id)
    {
      try
      {
        await _firestoreDb.Collection(_collectionName).Document(id).DeleteAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting {typeof(T).Name} document with ID {id}");
        throw;
      }
    }
  }
}
