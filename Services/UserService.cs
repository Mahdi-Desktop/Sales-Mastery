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

        // Use the updated VerifyPassword method for consistent password checking
        return VerifyPassword(user.Password, password);
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
        // Get the user with full details including password hash
        var user = await GetByIdAsync(userId);
        if (user == null)
        {
          return (false, "User not found");
        }

        // Verify the current password by hashing it and comparing with stored hash
        string hashedCurrentPassword = HashPassword(currentPassword);
        if (user.Password != hashedCurrentPassword)
        {
          _logger.LogWarning($"Password verification failed for user {userId}");
          return (false, "Current password is incorrect");
        }

        // Validate new password
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
        {
          return (false, "New password must be at least 8 characters long");
        }

        // Hash the new password
        string hashedNewPassword = HashPassword(newPassword);

        // Update password with the new hash
        user.Password = hashedNewPassword;
        user.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

        // Save changes
        await UpdateAsync(userId, user);
        _logger.LogInformation($"Password changed successfully for user {userId}");

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

        // Verify password using our VerifyPassword method
        if (!VerifyPassword(currentUser.Password, verificationPassword))
        {
          _logger.LogWarning($"Password verification failed for user {updatedUser.UserId} during update");
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

    // Helper method to verify password by comparing hashes
    private bool VerifyPassword(string storedPasswordHash, string inputPassword)
    {
      // Don't compare directly, hash the input password first
      string hashedInputPassword = HashPassword(inputPassword);

      // Compare the hashed input with the stored hash
      return storedPasswordHash == hashedInputPassword;
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

      // Hash the verification password and compare with stored hash
      string hashedVerificationPassword = HashPassword(updateDto.VerificationPassword);
      if (user.Password != hashedVerificationPassword)
      {
        _logger.LogWarning($"Password verification failed for user {updateDto.UserId} during profile update");
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

    // Improved phone number normalization for Lebanese numbers
    private string NormalizePhoneNumber(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber)) return "";

      // Log the original input
      _logger.LogDebug($"Normalizing phone number: {phoneNumber}");

      // Remove all non-digit characters except the + at the beginning
      string cleaned = phoneNumber;
      if (cleaned.StartsWith("+"))
      {
        cleaned = "+" + new string(cleaned.Substring(1).Where(char.IsDigit).ToArray());
      }
      else
      {
        cleaned = new string(cleaned.Where(char.IsDigit).ToArray());
      }

      _logger.LogDebug($"After cleaning: {cleaned}");

      // Handle different formats for Lebanese numbers
      string normalized;

      // If it starts with +961, it's already in international format
      if (cleaned.StartsWith("+961"))
      {
        normalized = cleaned;
      }
      // If it starts with +, but not +961, remove the + and proceed
      else if (cleaned.StartsWith("+"))
      {
        normalized = cleaned.Substring(1);
      }
      // If it starts with 961
      else if (cleaned.StartsWith("961"))
      {
        normalized = cleaned;
      }
      // If it starts with 0, replace with 961
      else if (cleaned.StartsWith("0"))
      {
        normalized = "961" + cleaned.Substring(1);
      }
      // Otherwise, assume it's a number without prefix and add 961
      else
      {
        normalized = "961" + cleaned;
      }

      _logger.LogDebug($"Normalized result: {normalized}");
      return normalized;
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

        // Add more diagnostic logging
        _logger.LogInformation($"Original phone input: {phoneNumber}");

        // Normalize the input phone number
        string normalizedInput = NormalizePhoneNumber(phoneNumber);
        _logger.LogInformation($"Normalized phone input: {normalizedInput}");

        // Try direct queries with different formats
        // 1. Try exact match
        var exactQuery = _collection.WhereEqualTo("PhoneNumber", phoneNumber);
        var exactSnapshot = await exactQuery.GetSnapshotAsync();
        _logger.LogInformation($"Exact phone query ({phoneNumber}) found: {exactSnapshot.Count} results");

        // 2. Try with + prefix if not present
        if (exactSnapshot.Count == 0 && !phoneNumber.StartsWith("+"))
        {
          var withPlusQuery = _collection.WhereEqualTo("PhoneNumber", "+" + phoneNumber);
          var withPlusSnapshot = await withPlusQuery.GetSnapshotAsync();
          _logger.LogInformation($"With + prefix query (+{phoneNumber}) found: {withPlusSnapshot.Count} results");

          if (withPlusSnapshot.Count > 0)
          {
            exactSnapshot = withPlusSnapshot;
            _logger.LogInformation($"Using results from + prefix query");
          }
        }

        // 3. Try with +961 prefix if not present
        if (exactSnapshot.Count == 0 && !phoneNumber.StartsWith("+961"))
        {
          string phoneWith961 = phoneNumber;
          if (phoneNumber.StartsWith("+"))
            phoneWith961 = "+961" + phoneNumber.Substring(1);
          else if (phoneNumber.StartsWith("961"))
            phoneWith961 = "+" + phoneNumber;
          else
            phoneWith961 = "+961" + phoneNumber;

          var with961Query = _collection.WhereEqualTo("PhoneNumber", phoneWith961);
          var with961Snapshot = await with961Query.GetSnapshotAsync();
          _logger.LogInformation($"With +961 prefix query ({phoneWith961}) found: {with961Snapshot.Count} results");

          if (with961Snapshot.Count > 0)
          {
            exactSnapshot = with961Snapshot;
            _logger.LogInformation($"Using results from +961 prefix query");
          }
        }

        // Use the results from any of the direct queries that succeeded
        if (exactSnapshot.Count > 0)
        {
          var document = exactSnapshot.Documents[0];
          try
          {
            var userData = document.ToDictionary();

            // Get and log the actual phone number in the document for comparison
            string storedPhone = GetStringValue(userData, "PhoneNumber");
            _logger.LogInformation($"Found document with phone: '{storedPhone}', comparing to input: '{phoneNumber}'");

            // Create user object manually to handle type conversions
            var user = new User
            {
              UserId = document.Id,
              Email = GetStringValue(userData, "Email"),
              Password = GetStringValue(userData, "Password"),
              FirstName = GetStringValue(userData, "FirstName"),
              LastName = GetStringValue(userData, "LastName"),
              MiddleName = GetStringValue(userData, "MiddleName"),
              PhoneNumber = GetStringValue(userData, "PhoneNumber"),
              Role = GetStringValue(userData, "Role", "3"),
              CreatedAt = GetTimestampValue(userData, "CreatedAt"),
              UpdatedAt = GetTimestampValue(userData, "UpdatedAt"),
              CreatedBy = GetStringValue(userData, "CreatedBy"),
              CustomerId = GetStringListValue(userData, "CustomerId"),
              AffiliateId = GetStringListValue(userData, "AffiliateId"),
              InvoiceId = GetStringListValue(userData, "InvoiceId"),
              OrderId = GetOrderIdListValue(userData, "OrderId")
            };

            _logger.LogInformation($"Found exact match for phone: {phoneNumber}, user: {user.UserId}");
            return user;
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, $"Error converting document to User: {document.Id}");
            throw;
          }
        }

        // If direct query not found, try fetching all users and compare
        _logger.LogInformation("No exact phone match found, trying with all users");
        var allUsers = await GetAllAsync();
        _logger.LogInformation($"Retrieved {allUsers.Count} users for phone comparison");

        // Log all phone numbers for debugging
        foreach (var user in allUsers)
        {
          if (!string.IsNullOrEmpty(user.PhoneNumber))
          {
            _logger.LogInformation($"User {user.UserId}: Phone = '{user.PhoneNumber}'");
          }
        }

        foreach (var user in allUsers)
        {
          if (!string.IsNullOrEmpty(user.PhoneNumber))
          {
            string normalizedUserPhone = NormalizePhoneNumber(user.PhoneNumber);
            _logger.LogInformation($"Comparing: Input='{normalizedInput}' with User='{normalizedUserPhone}'");

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
              _logger.LogInformation($"Last 8 comparison: Input='{inputLast8}' with User='{userLast8}'");

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

    // Add these helper methods
    private List<string> GetStringListValue(Dictionary<string, object> data, string key)
    {
      if (data.TryGetValue(key, out var value) && value is List<object> list)
      {
        return list.Select(item => item?.ToString() ?? "").ToList();
      }
      return new List<string>();
    }

    private List<object> GetOrderIdListValue(Dictionary<string, object> data, string key)
    {
      if (data.TryGetValue(key, out var value) && value is List<object> list)
      {
        return list;
      }
      return new List<object>();
    }
  }
}
