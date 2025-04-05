using AspnetCoreMvcFull.DTO;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class AuthService
  {
    private readonly UserService _userService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserService userService, ILogger<AuthService> logger)
    {
      _userService = userService;
      _logger = logger;
    }

    public async Task<(bool Success, User User, string Message)> LoginAsync(string email, string password)
    {
      try
      {
        // Get user by email
        var user = await _userService.GetUserByEmailAsync(email);

        if (user == null)
        {
          _logger.LogWarning($"No user found with email: {email}");
          return (false, null, "Invalid email or password");
        }

        // Verify password
        string hashedPassword = _userService.HashPassword(password);

        // Check if the stored password is empty/null
        if (string.IsNullOrEmpty(user.Password))
        {
          _logger.LogWarning($"User found but password is null/empty: {email}");
          return (false, null, "Invalid account configuration. Please contact support.");
        }

        // Compare passwords
        if (user.Password != hashedPassword)
        {
          _logger.LogWarning($"Password mismatch for: {email}");
          return (false, null, "Invalid email or password");
        }

        // Ensure all required properties are set
        if (string.IsNullOrEmpty(user.UserId))
        {
          user.UserId = Guid.NewGuid().ToString(); // Generate an ID if missing
          _logger.LogWarning($"Generated missing UserId for user: {email}");
        }

        if (string.IsNullOrEmpty(user.Role))
        {
          user.Role = "3"; // Default to customer role if missing
          _logger.LogWarning($"Set default Role '3' for user: {email}");
        }

        // Remove password before returning user object
        user.Password = null;

        return (true, user, "Login successful");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error during login for email {email}");
        return (false, null, $"An error occurred: {ex.Message}");
      }
    }


  }
}
