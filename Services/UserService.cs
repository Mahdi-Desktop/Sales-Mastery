using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using System.Threading.Tasks;
using System;
using System.Security.Cryptography;
using System.Text;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Services
{
  public class UserService : FirestoreService<User>
  {
    private readonly ILogger<UserService> _logger;
    private readonly AddressService _addressService;
    private readonly InvoiceService _invoiceService;
    private readonly AffiliateService _affiliateService;

    public UserService(
        IConfiguration configuration,
        ILogger<UserService> logger,
        AddressService addressService,
        InvoiceService invoiceService,
        AffiliateService affiliateService)
        : base(configuration, "users")
    {
      _logger = logger;
      _addressService = addressService;
      _invoiceService = invoiceService;
      _affiliateService = affiliateService;
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

    public async Task<User> GetUserByEmailAsync(string email)
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


  }
}
