using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services.Interface;
using FirebaseAdmin.Auth;

namespace AspnetCoreMvcFull.Services
{
  public class AuthService : IFirebaseAuthService
  {
    private readonly FirestoreDb _firestore;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(FirestoreDb firestore, IConfiguration config, ILogger<AuthService> logger)
    {
      _firestore = firestore;
      _config = config;
      _logger = logger;
    }

    public async Task<User> LoginAsync(string email, string password)
    {
      try
      {
        // Check user in Firestore
        var userQuery = _firestore.Collection("Users")
            .WhereEqualTo("Email", email)
            .Limit(1);

        var snapshot = await userQuery.GetSnapshotAsync();
        if (snapshot.Count == 0)
          throw new Exception("Invalid credentials");

        var userDoc = snapshot.Documents[0];
        var user = userDoc.ConvertTo<User>();

        // Verify password (in a real app, use proper password hashing)
        if (user.Password != password)
          throw new Exception("Invalid credentials");

        user.UserId = userDoc.Id;
        return user;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Login failed");
        throw;
      }
    }

    public async Task<string> SignUpAsync(string email, string password, string role, string createdBy)
    {
      try
      {
        var newUser = new User
        {
          Email = email,
          Password = password, // In production, hash this password
          Role = role,
          CreatedBy = createdBy,
          CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
          UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var docRef = await _firestore.Collection("Users").AddAsync(newUser);
        return docRef.Id;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Signup failed");
        throw;
      }
    }

    public Task SendPasswordResetEmailAsync(string email)
    {
      // Implement email sending logic here
      return Task.CompletedTask;
    }

    public async Task<User> GetUserAsync(string userId)
    {
      var snapshot = await _firestore.Collection("Users").Document(userId).GetSnapshotAsync();
      if (!snapshot.Exists)
        return null;

      var user = snapshot.ConvertTo<User>();
      user.UserId = snapshot.Id;
      return user;
    }

    public Task LogoutAsync()
    {
      // Session cleanup would be handled by the controller
      return Task.CompletedTask;
    }
  }
}
