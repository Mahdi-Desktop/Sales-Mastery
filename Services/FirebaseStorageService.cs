using Firebase.Storage;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class FirebaseStorageService
  {
    private readonly FirebaseStorage _firebaseStorage;
    private readonly string _storageBucket;

    public FirebaseStorageService(IConfiguration configuration)
    {
      // Get storage bucket from configuration
      _storageBucket = configuration["Firebase:StorageBucket"];

      // Get auth credentials path
      string credentialsPath = configuration["Firebase:ServiceAccountKeyPath"];

      // Initialize Firebase Storage
      _firebaseStorage = new FirebaseStorage(_storageBucket,
          new FirebaseStorageOptions
          {
            AuthTokenAsyncFactory = () => Task.FromResult(GetFirebaseToken(credentialsPath)),
            ThrowOnCancel = true
          });
    }

    private string GetFirebaseToken(string credentialsPath)
    {
      // You may need to implement token generation based on your service account
      // This is a simplified example - you might need a proper JWT implementation
      // For now, return null to use default credentials from environment variable
      return null;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folderName = "products")
    {
      try
      {
        // Generate a unique filename to avoid collisions
        string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

        // Upload to Firebase Storage
        var task = await _firebaseStorage
            .Child(folderName)
            .Child(uniqueFileName)
            .PutAsync(fileStream);

        // Return the download URL
        return task;
      }
      catch (Exception ex)
      {
        throw new Exception($"File upload failed: {ex.Message}");
      }
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
      try
      {
        // Extract file path from URL
        Uri uri = new Uri(fileUrl);
        string filePath = uri.LocalPath;

        // Remove leading slash and get segments
        if (filePath.StartsWith("/"))
          filePath = filePath.Substring(1);

        // Split path into segments
        string[] segments = filePath.Split('/');

        // The first segment should be the folder (e.g., "products")
        // The last segment should be the filename
        if (segments.Length >= 2)
        {
          string folderName = segments[0];
          string fileName = segments[segments.Length - 1];

          // Delete from Firebase Storage
          await _firebaseStorage
              .Child(folderName)
              .Child(fileName)
              .DeleteAsync();
        }
        else
        {
          throw new Exception("Invalid file URL format");
        }
      }
      catch (Exception ex)
      {
        throw new Exception($"File deletion failed: {ex.Message}");
      }
    }
  }
}
