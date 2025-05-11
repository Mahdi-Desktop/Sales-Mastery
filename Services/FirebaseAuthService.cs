using AspnetCoreMvcFull.Interfaces;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;

namespace AspnetCoreMvcFull.Services;

public class FirebaseAuthService : IFirebaseAuthService
{
  private readonly FirestoreDb _firestoreDb;
  private readonly ILogger<FirebaseAuthService> _logger;

  public FirebaseAuthService(FirestoreDb firestoreDb, ILogger<FirebaseAuthService> logger)
  {
    _firestoreDb = firestoreDb;
    _logger = logger;
  }

  public async Task<string?> SignUp(string email, string password)
  {
    try
    {
      // Check if user already exists
      var usersRef = _firestoreDb.Collection("users");
      var query = usersRef.WhereEqualTo("Email", email);
      var snapshot = await query.GetSnapshotAsync();

      if (snapshot.Count > 0)
      {
        _logger.LogWarning($"User with email {email} already exists");
        return null;
      }

      // Create new user document
      var user = new Dictionary<string, object>
            {
                { "Email", email },
                { "Password", password },
                { "CreatedAt", Timestamp.FromDateTime(DateTime.UtcNow) },
                { "UpdatedAt", Timestamp.FromDateTime(DateTime.UtcNow) }
            };

      var docRef = await usersRef.AddAsync(user);
      return docRef.Id;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during sign up");
      return null;
    }
  }

  public async Task<string?> Login(string email, string password)
  {
    try
    {
      var usersRef = _firestoreDb.Collection("users");
      var query = usersRef.WhereEqualTo("Email", email);
      var snapshot = await query.GetSnapshotAsync();

      if (snapshot.Count == 0)
      {
        _logger.LogWarning($"No user found with email {email}");
        return null;
      }

      var userDoc = snapshot.Documents[0];
      var userData = userDoc.ConvertTo<Dictionary<string, object>>();

      if (userData["Password"].ToString() != password)
      {
        _logger.LogWarning($"Invalid password for user {email}");
        return null;
      }

      return userDoc.Id;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error during login");
      return null;
    }
  }

  public async Task<string?> SendPhoneVerificationCode(string phoneNumber)
  {
    try
    {
      _logger.LogInformation($"Sending verification code to {phoneNumber}");
      // Generate a random verification code (6 digits)
      Random random = new Random();
      string verificationCode = random.Next(100000, 999999).ToString();
      string verificationId = Guid.NewGuid().ToString();

      _logger.LogInformation($"DEVELOPMENT ONLY - Verification code for {phoneNumber}: {verificationCode}");
      _logger.LogInformation($"DEVELOPMENT ONLY - Verification ID: {verificationId}");

      return verificationId;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error sending verification code to {phoneNumber}");
      return null;
    }
  }

  public async Task<bool> VerifyPhoneNumber(string verificationId, string code)
  {
    try
    {
      _logger.LogInformation($"Verifying code: {code} for verification ID: {verificationId}");
      bool isValid = code.Length == 6 && code.All(char.IsDigit);

      if (code == "123456" || code == "000000")
      {
        _logger.LogInformation("DEVELOPMENT ONLY - Test code accepted");
        return true;
      }

      return isValid;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error verifying code: {code}");
      return false;
    }
  }

  public async Task<bool> ResetPasswordByPhone(string uid, string newPassword)
  {
    try
    {
      _logger.LogInformation($"Resetting password for user: {uid}");
      var userRef = _firestoreDb.Collection("users").Document(uid);
      await userRef.UpdateAsync("Password", newPassword);
      return true;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error resetting password for user: {uid}");
      return false;
    }
  }

  public void SignOut()
  {
    // No Firebase Auth sign out needed since we're using Firestore
  }
}
