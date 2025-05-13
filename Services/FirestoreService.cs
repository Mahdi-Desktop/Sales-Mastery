using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspnetCoreMvcFull.DTO;
namespace AspnetCoreMvcFull.Services
{
  public class FirestoreService<T> where T : class
  {
    protected readonly FirestoreDb _firestoreDb;
    protected readonly string _collectionName;
    protected readonly CollectionReference _collection;

    public FirestoreService(IConfiguration configuration, string collectionName)
    {
      // Get project ID from configuration
      string projectId = configuration["Firebase:ProjectId"];


      // Set environment variable for credentials file if provided in config
      string credentialsPath = configuration["Firebase:ServiceAccountKeyPath"];
      if (!string.IsNullOrEmpty(credentialsPath))
      {
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
      }

      // Create Firestore client
      _firestoreDb = FirestoreDb.Create(projectId);
      _collectionName = collectionName;
      _collection = _firestoreDb.Collection(collectionName);
    }

    /*      public async Task<List<T>> GetAllAsync()
          {
            QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
            List<T> items = new List<T>();

            foreach (DocumentSnapshot document in snapshot.Documents)
            {
              if (document.Exists)
              {
                T item = document.ConvertTo<T>();

                // Set the document ID based on the type
                if (typeof(T) == typeof(User))
                {
                  // For User type, set UserId property
                  var user = item as User;
                  if (user != null)
                  {
                    user.UserId = document.Id;
                  }
                }
                else
                {
                  // For other types, try to set FirestoreId property if it exists
                  var docIdProperty = typeof(T).GetProperty("FirestoreId");
                  if (docIdProperty != null)
                  {
                    docIdProperty.SetValue(item, document.Id);
                  }
                }

                items.Add(item);
              }
            }

            return items;
          }*/


    public async Task<T> GetByIdAsync(string id)
    {
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(id);
      DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

      if (snapshot.Exists)
      {
        T item = snapshot.ConvertTo<T>();

        // Set the document ID based on the type
        if (typeof(T) == typeof(User))
        {
          // For User type, set UserId property
          var user = item as User;
          if (user != null)
          {
            user.UserId = snapshot.Id;
          }
        }
        else
        {
          // For other types, try to set FirestoreId property if it exists
          var docIdProperty = typeof(T).GetProperty("FirestoreId");
          if (docIdProperty != null)
          {
            docIdProperty.SetValue(item, snapshot.Id);
          }
        }

        return item;
      }

      return null;
    }
    public async Task<List<T>> GetAllAsync()
    {
      var snapshot = await _collection.GetSnapshotAsync();
      var result = new List<T>();

      foreach (var document in snapshot.Documents)
      {
        try
        {
          // Try to convert using the standard converter
          var item = document.ConvertTo<T>();

          // If the item has a property called "Id" or similar that's null, set it to the document ID
          var idProperty = typeof(T).GetProperty("Id") ??
                          typeof(T).GetProperty("UserId") ??
                          typeof(T).GetProperty($"{typeof(T).Name}Id");

          if (idProperty != null && idProperty.PropertyType == typeof(string))
          {
            var currentValue = idProperty.GetValue(item) as string;
            if (string.IsNullOrEmpty(currentValue))
            {
              idProperty.SetValue(item, document.Id);
            }
          }

          result.Add(item);
        }
        catch (ArgumentException ex) when (ex.Message.Contains("Unable to convert reference value to System.String"))
        {
          // Handle specific error for reference-to-string conversion
          try
          {
            // Fall back to manual conversion for this document
            var data = document.ToDictionary();
            var manualItem = ManuallyConvertDocument(document.Id, data);
            if (manualItem != null)
            {
              result.Add(manualItem);
            }
          }
          catch (Exception innerEx)
          {
            // Log but continue with next document
            System.Diagnostics.Debug.WriteLine($"Error manually converting document {document.Id}: {innerEx.Message}");
          }
        }
        catch (Exception ex)
        {
          // Log but continue with next document
          System.Diagnostics.Debug.WriteLine($"Error converting document {document.Id}: {ex.Message}");
        }
      }

      return result;
    }

    // Enhance the manual conversion method to better handle references
    private T ManuallyConvertDocument(string documentId, Dictionary<string, object> data)
    {
      try
      {
        // Create a new instance of T
        T item = Activator.CreateInstance<T>();

        // Get all properties of T
        var properties = typeof(T).GetProperties();

        foreach (var property in properties)
        {
          // Skip read-only properties
          if (!property.CanWrite)
            continue;

          string propertyName = property.Name;

          // Try both camelCase and PascalCase property names
          object value = null;
          if (data.TryGetValue(propertyName, out value))
          {
            // Found using exact case
          }
          else if (data.TryGetValue(propertyName.ToLower().First() + propertyName.Substring(1), out value))
          {
            // Found using camelCase
          }
          else if (data.TryGetValue(propertyName.ToUpper().First() + propertyName.Substring(1), out value))
          {
            // Found using PascalCase
          }
          else
          {
            // Property not found, skip
            continue;
          }

          // Handle document ID property
          if ((propertyName == "Id" || propertyName == "UserId" || propertyName == $"{typeof(T).Name}Id")
              && property.PropertyType == typeof(string))
          {
            property.SetValue(item, documentId);
            continue;
          }

          if (value == null)
          {
            // Leave as default value
            continue;
          }

          // Handle standard property types
          if (property.PropertyType == typeof(string))
          {
            property.SetValue(item, value.ToString());
          }
          else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
          {
            if (int.TryParse(value.ToString(), out var intValue))
            {
              property.SetValue(item, intValue);
            }
          }
          else if (property.PropertyType == typeof(double) || property.PropertyType == typeof(double?))
          {
            if (double.TryParse(value.ToString(), out var doubleValue))
            {
              property.SetValue(item, doubleValue);
            }
          }
          else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
          {
            if (bool.TryParse(value.ToString(), out var boolValue))
            {
              property.SetValue(item, boolValue);
            }
          }
          else if (property.PropertyType == typeof(Timestamp) || property.PropertyType == typeof(Timestamp?))
          {
            if (value is Timestamp timestamp)
            {
              property.SetValue(item, timestamp);
            }
          }
          else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
          {
            if (value is Timestamp timestamp)
            {
              property.SetValue(item, timestamp.ToDateTime());
            }
          }
          // Handle collections of strings or document references
          else if (property.PropertyType == typeof(List<string>))
          {
            var stringList = new List<string>();

            // Handle array values
            if (value is IEnumerable<object> collection)
            {
              foreach (var element in collection)
              {
                // For document references, extract the ID
                if (element is DocumentReference docRef)
                {
                  stringList.Add(docRef.Id);
                }
                else if (element != null)
                {
                  stringList.Add(element.ToString());
                }
              }
              property.SetValue(item, stringList);
            }
          }
          // Special handling for OrderId which is List<object>
          else if (property.PropertyType == typeof(List<object>))
          {
            var objectList = new List<object>();

            // Handle array values
            if (value is IEnumerable<object> collection)
            {
              foreach (var element in collection)
              {
                // For document references, convert to string ID
                if (element is DocumentReference docRef)
                {
                  objectList.Add(docRef.Id);
                }
                else if (element != null)
                {
                  objectList.Add(element);
                }
              }
              property.SetValue(item, objectList);
            }
          }
        }

        return item;
      }
      catch (Exception ex)
      {
        System.Diagnostics.Debug.WriteLine($"Error in manual document conversion: {ex.Message}");
        return null;
      }
    }
    public async Task<string> AddAsync(T item)
    {
      // Generate a new document ID
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document();

      // Set the generated ID on the item (this happens BEFORE saving)
      if (typeof(T) == typeof(User))
      {
        var user = item as User;
        if (user != null)
        {
          user.UserId = docRef.Id;
        }
      }

      // Save to Firestore (this happens AFTER setting the ID)
      await docRef.SetAsync(item);

      // Set timestamps
      var createdAtProperty = typeof(T).GetProperty("CreatedAt");
      if (createdAtProperty != null && createdAtProperty.PropertyType == typeof(Timestamp))
      {
        createdAtProperty.SetValue(item, Timestamp.FromDateTime(DateTime.UtcNow));
      }

      var updatedAtProperty = typeof(T).GetProperty("UpdatedAt");
      if (updatedAtProperty != null && updatedAtProperty.PropertyType == typeof(Timestamp))
      {
        updatedAtProperty.SetValue(item, Timestamp.FromDateTime(DateTime.UtcNow));
      }

      // Save to Firestore
      await docRef.SetAsync(item);
      return docRef.Id;
    }


    public async Task UpdateAsync(string id, T item)
    {
      // Update timestamp if the class has a property for it
      var updatedAtProperty = typeof(T).GetProperty("UpdatedAt");
      if (updatedAtProperty != null && updatedAtProperty.PropertyType == typeof(Timestamp))
      {
        updatedAtProperty.SetValue(item, Timestamp.FromDateTime(DateTime.UtcNow));
      }

      // Update in Firestore
      await _firestoreDb.Collection(_collectionName).Document(id).SetAsync(item);
    }

    public async Task DeleteAsync(string id)
    {
      await _firestoreDb.Collection(_collectionName).Document(id).DeleteAsync();
    }
  }
}
