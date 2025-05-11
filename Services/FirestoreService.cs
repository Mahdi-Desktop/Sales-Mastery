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

            // Try to convert using the standard converter
            var item = document.ConvertTo<T>();

            // If the item has a property called "Id" or similar that's null, set it to the document ID
            var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("UserId");
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

        return result;
      
    }

    // Add this method to your FirestoreService<T> class
    private T ManuallyConvertDocument(string documentId, Dictionary<string, object> data)
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

        // Check if the property exists in the data dictionary
        if (data.TryGetValue(propertyName, out var value))
        {
  
            // Handle different property types
            if (value == null)
            {
              // Leave as default value
              continue;
            }
            else if (property.PropertyType == typeof(string))
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
            else if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
              if (bool.TryParse(value.ToString(), out var boolValue))
              {
                property.SetValue(item, boolValue);
              }
            }
            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
            {
              if (value is Google.Cloud.Firestore.Timestamp timestamp)
              {
                property.SetValue(item, timestamp.ToDateTime());
              }
            }
            else if (property.PropertyType == typeof(Google.Cloud.Firestore.Timestamp))
            {
              if (value is Google.Cloud.Firestore.Timestamp timestamp)
              {
                property.SetValue(item, timestamp);
              }
              else
              {
                // Try to create a timestamp from a DateTime string
                if (DateTime.TryParse(value.ToString(), out var dateTime))
                {
                  property.SetValue(item, Google.Cloud.Firestore.Timestamp.FromDateTime(dateTime));
                }
              }
            }
            // Add more type conversions as needed

        }
        else if (propertyName == "Id" || propertyName == "UserId")
        {
          // Set ID property to document ID if it's not in the data
          if (property.PropertyType == typeof(string))
          {
            property.SetValue(item, documentId);
          }
        }
      }

      return item;
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
