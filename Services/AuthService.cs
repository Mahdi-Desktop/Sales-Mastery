using AspnetCoreMvcFull.DTO;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TechTalk.SpecFlow.CommonModels;

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

    public string? message { get; private set; }
    public bool success { get; private set; }

    /*    public async Task<(bool Success, User User, string Message)> LoginAsync(string email, string password)
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
    */

    /*
        public async Task<(bool Success, User User, string Message)> LoginAsync(string email, string password, ISession session)
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
        }*/
    public async Task<(bool Success, User User, string Message)> LoginAsync(
        string email,
        string password,
        ISession session)
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

        // Create a copy of the user without exposing the password
        var userWithoutPassword = new User
        {
          UserId = user.UserId,
          Email = user.Email,
          FirstName = user.FirstName,
          LastName = user.LastName,
          PhoneNumber = user.PhoneNumber,
          Role = user.Role,
          CreatedAt = user.CreatedAt,
          UpdatedAt = user.UpdatedAt,
          Password = null // Set password to null for security
        };

        return (true, userWithoutPassword, "Login successful");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error during login for email {email}");
        return (false, null, $"An error occurred: {ex.Message}");
      }
    }

  }
}
