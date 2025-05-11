using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Text;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Services.Interfaces;

namespace AspnetCoreMvcFull.Services
{
  public class UserService : FirestoreService<User>
  {
    private readonly ILogger<UserService> _logger;
    private readonly AddressService _addressService;
    private readonly InvoiceService _invoiceService;
    private readonly IServiceProvider _serviceProvider;  // Replace AffiliateService with IServiceProvider

    public UserService(
        IConfiguration configuration,
        ILogger<UserService> logger,
        AddressService addressService,
        InvoiceService invoiceService,
        IServiceProvider serviceProvider)  // Replace AffiliateService parameter
        : base(configuration, "users")
    {
      _logger = logger;
      _addressService = addressService;
      _invoiceService = invoiceService;
      _serviceProvider = serviceProvider;
    }

    private AffiliateService GetAffiliateService()
    {
      return _serviceProvider.GetService<AffiliateService>();
    }

    public async Task<bool> SomeMethodThatUsesAffiliateService()
    {
      var affiliateService = GetAffiliateService();
      // Use affiliateService here
      return true;
    }
    public async Task<string> AddUserAsync(User user)
    {
      // Set timestamps if not already set
      if (user.CreatedAt == default)
        user.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

      if (user.UpdatedAt == default)
        user.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

      // Hash the password if it's not already hashed
      if (!string.IsNullOrEmpty(user.Password) && !user.Password.StartsWith("$2a$"))
      {
        user.Password = HashPassword(user.Password);
      }

      // Add the user to Firestore
      return await AddAsync(user);
    }

    public async Task<User> GetUserByIdAsync(string userId)
    {
      try
      {
        var user = await GetByIdAsync(userId);
        // Remove sensitive information for general view
        user.Password = null;
        return user;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting user with ID {userId}");
        throw;
      }
    }

    public async Task<User> GetUserWithSecurityInfoAsync(string userId)
    {
      try
      {
        // Get user with password (for security page)
        var user = await GetByIdAsync(userId);
        // We keep the password hash for verification purposes
        return user;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting user security info with ID {userId}");
        throw;
      }
    }

    public async Task<bool> VerifyPasswordAsync(string userId, string password)
    {
      try
      {
        var user = await GetByIdAsync(userId);
        if (user == null) return false;

        // Compare password hash
        string hashedPassword = HashPassword(password);
        return user.Password == hashedPassword;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error verifying password for user with ID {userId}");
        return false;
      }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
      try
      {
        // Get the user
        var user = await GetUserByIdAsync(userId);
        if (user == null)
        {
          return (false, "User not found");
        }

        if (!VerifyPassword(user.Password, currentPassword))
        {
          return (false, "Current password is incorrect");
        }

        // Validate new password
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
        {
          return (false, "New password must be at least 8 characters long");
        }

        // Update password
        user.Password = newPassword; // In a real app, hash the password
        user.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

        // Save changes
        await UpdateAsync(userId, user);

        return (true, "Password changed successfully");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error changing password for user {userId}");
        return (false, $"Error changing password: {ex.Message}");
      }
    }


    public async Task<bool> UpdateUserAsync(User updatedUser, string verificationPassword)
    {
      try
      {
        // Get current user data
        var currentUser = await GetByIdAsync(updatedUser.UserId);
        if (currentUser == null) return false;

        // Verify password
        if (!await VerifyPasswordAsync(updatedUser.UserId, verificationPassword))
        {
          return false;
        }

        // Preserve password (don't update it through this method)
        updatedUser.Password = currentUser.Password;
        updatedUser.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        await UpdateAsync(updatedUser.UserId, updatedUser);
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating user with ID {updatedUser.UserId}");
        return false;
      }
    }

    /*    private string HashPassword(string password)
        {
          using (SHA256 sha256 = SHA256.Create())
          {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
              builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
          }
        }*/

    // Helper method to verify password
    private bool VerifyPassword(string storedPassword, string inputPassword)
    {
      // If you're storing plain text passwords (not recommended)
      return storedPassword == inputPassword;

      // If you're using hashing (recommended), you'd use something like:
      // return HashPassword(inputPassword) == storedPassword;
    }

    // Hash password method if you're using hashing
    /* private string HashPassword(string password)
     {
       using (SHA256 sha256 = SHA256.Create())
       {
         byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
         StringBuilder builder = new StringBuilder();
         for (int i = 0; i < bytes.Length; i++)
         {
           builder.Append(bytes[i].ToString("x2"));
         }
         return builder.ToString();
       }
     }*/
    public async Task<ResultDto> UpdateUserWithVerificationAsync(UserUpdateDto updateDto)
    {
      var user = await GetByIdAsync(updateDto.UserId);
      if (user == null)
      {
        return new ResultDto { Success = false, Message = "User not found" };
      }

      // Check if the entered password matches the stored password
      if (user.Password != updateDto.VerificationPassword)
      {
        return new ResultDto { Success = false, Message = "Incorrect password. Changes not saved." };
      }

      // Check if there are any actual changes
      bool isChanged = false;

      if (!string.Equals(user.FirstName, updateDto.FirstName))
      {
        user.FirstName = updateDto.FirstName;
        isChanged = true;
      }
      if (!string.Equals(user.MiddleName, updateDto.MiddleName))
      {
        user.MiddleName = updateDto.MiddleName;
        isChanged = true;
      }
      if (!string.Equals(user.LastName, updateDto.LastName))
      {
        user.LastName = updateDto.LastName;
        isChanged = true;
      }
      if (!string.Equals(user.Email, updateDto.Email))
      {
        user.Email = updateDto.Email;
        isChanged = true;
      }
      if (!string.Equals(user.PhoneNumber, updateDto.PhoneNumber))
      {
        user.PhoneNumber = updateDto.PhoneNumber;
        isChanged = true;
      }
      if (!string.Equals(user.Role, updateDto.Role))
      {
        user.Role = updateDto.Role;
        isChanged = true;
      }

      if (!isChanged)
      {
        return new ResultDto { Success = false, Message = "No changes detected." };
      }

      user.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

      // Save changes in Firestore
      await UpdateAsync(updateDto.UserId, user);

      return new ResultDto { Success = true, Message = "User updated successfully" };
    }

    /*    public async Task<User> GetUserByEmailAsync(string email)
        {
          // Logic to get user by email from database
          var query = _collection.WhereEqualTo("Email", email);
          var snapshot = await query.GetSnapshotAsync();
          if (snapshot.Count > 0)
          {
            var user = snapshot.Documents.FirstOrDefault().ConvertTo<User>();
            return user;
          }
          return null;
        }*/

    public async Task<User> GetUserByEmailAsync(string email)
    {
      try
      {
        // Logic to get user by email from database
        var query = _collection.WhereEqualTo("Email", email);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count > 0)
        {
          var document = snapshot.Documents.FirstOrDefault();
          _logger.LogInformation($"Found user document with ID: {document.Id}");

          try
          {
            var user = document.ConvertTo<User>();

            // Ensure critical fields are not null
            user.UserId = user.UserId ?? document.Id;
            user.Role = user.Role ?? "3"; // Default to customer if null

            return user;
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, $"Error converting document to User: {document.Id}");

            // Manual conversion as fallback
            var userData = document.ToDictionary();
            var user = new User
            {
              UserId = document.Id,
              Email = GetStringValue(userData, "Email"),
              Password = GetStringValue(userData, "Password"),
              FirstName = GetStringValue(userData, "FirstName"),
              LastName = GetStringValue(userData, "LastName"),
              PhoneNumber = GetStringValue(userData, "PhoneNumber"),
              Role = GetStringValue(userData, "Role", "3"),
              CreatedAt = GetTimestampValue(userData, "CreatedAt"),
              UpdatedAt = GetTimestampValue(userData, "UpdatedAt")
            };

            return user;
          }
        }

        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error in GetUserByEmailAsync for email: {email}");
        throw;
      }
    }

    // Modify the NormalizePhoneNumber method to better handle Lebanese phone formats
    // Improved phone number normalization
    private string NormalizePhoneNumber(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber)) return "";

      // Remove all non-digit characters
      string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

      // Remove leading zeros
      digitsOnly = digitsOnly.TrimStart('0');

      // For Lebanese numbers, if it doesn't have country code and has exactly 8 digits (mobile number length),
      // add the country code
      if (digitsOnly.Length == 8 && !digitsOnly.StartsWith("961"))
      {
        digitsOnly = "961" + digitsOnly;
      }

      return digitsOnly;
    }


    // Change this method from private to public
    public string HashPassword(string password)
    {
      using (SHA256 sha256 = SHA256.Create())
      {
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
          builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
      }
    }
    // Get a user by phone number
    // Get a user by phone number with improved matching
    public async Task<User> GetUserByPhoneNumberAsync(string phoneNumber)
    {
      try
      {
        _logger.LogInformation($"Searching for user with phone number: {phoneNumber}");

        // Normalize the input phone number
        string normalizedInput = NormalizePhoneNumber(phoneNumber);

        // First try direct query (exact match)
        var query = _collection.WhereEqualTo("PhoneNumber", phoneNumber);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count > 0)
        {
          var user = snapshot.Documents[0].ConvertTo<User>();
          _logger.LogInformation($"Found exact match for phone: {phoneNumber}, user: {user.UserId}");
          return user;
        }

        // If not found, get all users and try normalized comparison
        var allUsers = await GetAllAsync();

        foreach (var user in allUsers)
        {
          if (!string.IsNullOrEmpty(user.PhoneNumber))
          {
            string normalizedUserPhone = NormalizePhoneNumber(user.PhoneNumber);

            // Check if normalized versions match
            if (normalizedUserPhone == normalizedInput)
            {
              _logger.LogInformation($"Found match by normalized phone: {user.PhoneNumber} ↔ {phoneNumber}");
              return user;
            }

            // Also check last 8 digits (typical Lebanese mobile number length)
            if (normalizedUserPhone.Length >= 8 && normalizedInput.Length >= 8)
            {
              string userLast8 = normalizedUserPhone.Substring(normalizedUserPhone.Length - 8);
              string inputLast8 = normalizedInput.Substring(normalizedInput.Length - 8);

              if (userLast8 == inputLast8)
              {
                _logger.LogInformation($"Found match by last 8 digits: {userLast8}");
                return user;
              }
            }
          }
        }

        _logger.LogWarning($"No user found with phone number: {phoneNumber}");
        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting user by phone number: {phoneNumber}");
        throw;
      }
    }
    // Update a user's password
    public async Task<bool> UpdateUserPasswordAsync(string userId, string hashedPassword)
    {
      try
      {
        // Get a reference to the user document
        var userRef = _firestoreDb.Collection("users").Document(userId);

        // Update only the password field
        await userRef.UpdateAsync(new Dictionary<string, object>
        {
            { "Password", hashedPassword }
        });

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating password for user: {userId}");
        return false;
      }
    }

    // Helper methods for safe data extraction
    private string GetStringValue(Dictionary<string, object> data, string key, string defaultValue = "")
    {
      if (data.TryGetValue(key, out var value) && value != null)
      {
        return value.ToString();
      }
      return defaultValue;
    }

    private Google.Cloud.Firestore.Timestamp GetTimestampValue(Dictionary<string, object> data, string key)
    {
      if (data.TryGetValue(key, out var value) && value is Google.Cloud.Firestore.Timestamp timestamp)
      {
        return timestamp;
      }
      return Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);
    }
  }
}
