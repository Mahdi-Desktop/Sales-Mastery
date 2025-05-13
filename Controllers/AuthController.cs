using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Interfaces;
using global::AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Services.Interfaces;
using System.Configuration;
using Org.BouncyCastle.Crypto;
using Google.Type;
using Google.Cloud.Firestore;

namespace AspnetCoreMvcFull.Controllers
{
  public class AuthController : Controller
  {

    private readonly AuthService _authService;
    private readonly IFirebaseAuthService _firebaseAuthService;
    private readonly ILogger<AuthController> _logger;
    private readonly UserService _userService;
    private readonly IConfiguration _configuration;
    public AuthController(
      AuthService authService,
      UserService userService,
      IFirebaseAuthService firebaseAuthService,
      ILogger<AuthController> logger,
      IConfiguration configuration)
    {
      _authService = authService;
      _userService = userService;
      _firebaseAuthService = firebaseAuthService;
      _logger = logger;
      _configuration = configuration;
    }

    // For development only - creates a test user with a phone number
    [HttpGet]
    public async Task<IActionResult> CreateTestUserWithPhone()
    {
      try
      {
        // Create a test user with a phone number for testing reset password flow
        var user = new User
        {
          Email = "test@example.com",
          Password = _userService.HashPassword("Test123456"),
          PhoneNumber = "+96178854350", // Test phone number
          FirstName = "Test",
          LastName = "User",
          Role = "3", // Customer
          CreatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(System.DateTime.UtcNow),
          UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(System.DateTime.UtcNow)
        };

        string userId = await _userService.AddUserAsync(user);

        return Content($"Test user created successfully with phone +96178854350 and ID: {userId}");
      }
      catch (Exception ex)
      {
        return Content($"Error creating test user: {ex.Message}");
      }
    }


    [HttpGet]
    public IActionResult LoginBasic()
    {
      // If user is already logged in, redirect to dashboard
      //if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
      //{
      //  return RedirectToAction("Index", "Dashboards");
      //}

      return View();
    }
    /*    [HttpPost]
        public async Task<IActionResult> LoginBasic(LoginRequest loginRequest)
        {
          if (!ModelState.IsValid)
          {
            return View(loginRequest);
          }

          try
          {
            // Explicitly specify the types for tuple deconstruction
            (bool success, User user, string message) = await _authService.LoginAsync(
                loginRequest.Email,
                loginRequest.Password,
                HttpContext.Session);

            if (success && user != null)
            {
              // Store user information in session
              var userJson = JsonSerializer.Serialize(user);
              HttpContext.Session.SetString("CurrentUser", userJson);
              HttpContext.Session.SetString("UserId", user.UserId ?? string.Empty);

              // Get Firebase token
              var firebaseToken = await _firebaseAuthService.Login(loginRequest.Email, loginRequest.Password);
              if (!string.IsNullOrEmpty(firebaseToken))
              {
                HttpContext.Session.SetString("token", firebaseToken);
              }

              // Store role information
              HttpContext.Session.SetString("UserRole", user.Role ?? string.Empty);

              // Set role-specific boolean flags for navigation visibility
              HttpContext.Session.SetString("IsAdmin", "0");
              HttpContext.Session.SetString("IsAffiliate", "0");
              HttpContext.Session.SetString("IsCustomer", "0");

              // Set the appropriate role to 1 (true) based on user.Role
              switch (user.Role)
              {
                case "1": // Admin role
                  HttpContext.Session.SetString("IsAdmin", "1");

                  break;

                case "2": // Affiliate role
                  HttpContext.Session.SetString("IsAffiliate", "1");
                  break;

                case "3": // Customer role
                  HttpContext.Session.SetString("IsCustomer", "1");
                  break;
              }

              return RedirectToAction("Index", "Dashboards");
            }
            else
            {
              ModelState.AddModelError(string.Empty, message);
              return View(loginRequest);
            }
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error during login attempt");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
            return View(loginRequest);
          }
        }*/
    // Modify the LoginBasic method to add more logging
    [HttpPost]
    public async Task<IActionResult> LoginBasic(LoginRequest loginRequest)
    {
      if (!ModelState.IsValid)
      {
        return View(loginRequest);
      }

      try
      {
        _logger.LogInformation($"Login attempt for email: {loginRequest.Email}");

        // Explicitly specify the types for tuple deconstruction
        (bool success, User user, string message) = await _authService.LoginAsync(
            loginRequest.Email,
            loginRequest.Password,
            HttpContext.Session);

        if (success && user != null)
        {
          // Store user information in session
          var userJson = JsonSerializer.Serialize(user);
          HttpContext.Session.SetString("CurrentUser", userJson);
          HttpContext.Session.SetString("UserId", user.UserId ?? string.Empty);
          _logger.LogInformation($"User authenticated successfully. User ID: {user.UserId}");

          // Get Firebase token
          var firebaseToken = await _firebaseAuthService.Login(loginRequest.Email, loginRequest.Password);
          if (!string.IsNullOrEmpty(firebaseToken))
          {
            HttpContext.Session.SetString("token", firebaseToken);
            _logger.LogInformation("Firebase token stored in session");
          }
          else
          {
            _logger.LogWarning("Firebase token was null or empty");
          }

          // Store role information
          HttpContext.Session.SetString("UserRole", user.Role ?? string.Empty);
          _logger.LogInformation($"User role set: {user.Role}");

          // Set role-specific boolean flags for navigation visibility
          HttpContext.Session.SetString("IsAdmin", "0");
          HttpContext.Session.SetString("IsAffiliate", "0");
          HttpContext.Session.SetString("IsCustomer", "0");

          // Set the appropriate role to 1 (true) based on user.Role
          switch (user.Role)
          {
            case "1": // Admin role
              HttpContext.Session.SetString("IsAdmin", "1");
              _logger.LogInformation("User is an Admin");
              break;

            case "2": // Affiliate role
              HttpContext.Session.SetString("IsAffiliate", "1");
              _logger.LogInformation("User is an Affiliate");
              break;

            case "3": // Customer role
              HttpContext.Session.SetString("IsCustomer", "1");
              _logger.LogInformation("User is a Customer");
              break;
          }

          return RedirectToAction("Index", "Dashboards");
        }
        else
        {
          _logger.LogWarning($"Login failed: {message}");
          ModelState.AddModelError(string.Empty, message);
          return View(loginRequest);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during login attempt");
        ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
        return View(loginRequest);
      }
    }

    // Add this method to AuthController
    [HttpGet]
    public IActionResult CheckAuthStatus()
    {
      var authStatus = new Dictionary<string, string>();

      // Check if we have a token
      var token = HttpContext.Session.GetString("token");
      authStatus.Add("HasToken", !string.IsNullOrEmpty(token) ? "Yes" : "No");

      if (!string.IsNullOrEmpty(token))
      {
        // Don't show the full token for security reasons
        authStatus.Add("TokenLength", token.Length.ToString());
        authStatus.Add("TokenStart", token.Substring(0, 10) + "...");
      }

      // Check if we have a user ID
      var userId = HttpContext.Session.GetString("UserId");
      authStatus.Add("HasUserId", !string.IsNullOrEmpty(userId) ? "Yes" : "No");

      if (!string.IsNullOrEmpty(userId))
      {
        authStatus.Add("UserId", userId);
      }

      // Check user role
      var userRole = HttpContext.Session.GetString("UserRole");
      authStatus.Add("UserRole", !string.IsNullOrEmpty(userRole) ? userRole : "None");

      // Check environment variables
      authStatus.Add("GOOGLE_APPLICATION_CREDENTIALS",
          !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS"))
          ? "Set" : "Not Set");

      return Json(authStatus);
    }

    // Add this method to AuthController
    [HttpGet]
    public async Task<IActionResult> TestFirestoreConnection()
    {
      try
      {
        // Get the FirestoreDb from the service provider
        var firestoreDb = HttpContext.RequestServices.GetRequiredService<FirestoreDb>();

        // Try a simple operation
        var collection = firestoreDb.Collection("users");
        var snapshot = await collection.Limit(1).GetSnapshotAsync();

        return Content($"Firestore connection successful. Found {snapshot.Count} documents.");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error testing Firestore connection");
        return Content($"Firestore connection error: {ex.Message}");
      }
    }

    // Add this method to AuthController
    [HttpGet]
    public IActionResult CheckServiceAccount()
    {
      try
      {
        var credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        if (string.IsNullOrEmpty(credentialsPath))
        {
          return Content("GOOGLE_APPLICATION_CREDENTIALS environment variable is not set");
        }

        if (!System.IO.File.Exists(credentialsPath))
        {
          return Content($"Service account file not found at path: {credentialsPath}");
        }

        // Read the file (don't show the full content for security)
        var fileContent = System.IO.File.ReadAllText(credentialsPath);
        var fileSize = fileContent.Length;

        // Check if it contains key fields that should be in a service account JSON
        bool hasProjectId = fileContent.Contains("project_id");
        bool hasPrivateKey = fileContent.Contains("private_key");
        bool hasClientEmail = fileContent.Contains("client_email");

        return Content($"Service account file exists ({fileSize} bytes). " +
                      $"Contains project_id: {hasProjectId}, " +
                      $"Contains private_key: {hasPrivateKey}, " +
                      $"Contains client_email: {hasClientEmail}");
      }
      catch (Exception ex)
      {
        return Content($"Error checking service account: {ex.Message}");
      }
    }


    /*    [HttpPost]
        public async Task<IActionResult> LoginBasic(LoginRequest loginRequest)
        {
          if (!ModelState.IsValid)
          {
            return View(loginRequest);
          }

          try
          {
            var (success, user, message) = await _authService.LoginAsync(
                loginRequest.Email,
                loginRequest.Password,
                HttpContext.Session);

            if (success && user != null)
            {
              // Store user information in session
              var userJson = JsonSerializer.Serialize(user);
              HttpContext.Session.SetString("CurrentUser", userJson);
              HttpContext.Session.SetString("UserId", user.UserId ?? string.Empty);

              // Get Firebase token
              var firebaseToken = await _firebaseAuthService.Login(loginRequest.Email, loginRequest.Password);
              if (!string.IsNullOrEmpty(firebaseToken))
              {
                HttpContext.Session.SetString("token", firebaseToken);
              }

              // Store role information
              HttpContext.Session.SetString("UserRole", user.Role ?? string.Empty);

              // Set role-specific boolean flags for navigation visibility
              HttpContext.Session.SetString("IsAdmin", "0");
              HttpContext.Session.SetString("IsAffiliate", "0");
              HttpContext.Session.SetString("IsCustomer", "0");

              // Set the appropriate role to 1 (true) based on user.Role
              switch (user.Role)
              {
                case "1": // Admin role
                  HttpContext.Session.SetString("IsAdmin", "1");
                  break;

                case "2": // Affiliate role
                  HttpContext.Session.SetString("IsAffiliate", "1");
                  break;

                case "3": // Customer role
                  HttpContext.Session.SetString("IsCustomer", "1");
                  break;
              }

              return RedirectToAction("Index", "Dashboards");
            }
            else
            {
              ModelState.AddModelError(string.Empty, message);
              return View(loginRequest);
            }
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error during login attempt");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
            return View(loginRequest);
          }
        }*/
    [HttpGet]
    public IActionResult Logout()
    {
      // Clear the session
      HttpContext.Session.Clear();

      // Redirect to the login page
      return RedirectToAction("LoginBasic", "Auth");
    }
    /*    [HttpGet]
        public IActionResult Logout()
        {
          // Clear the session
          HttpContext.Session.Clear();

          // Redirect to the Not Authorized page
          return RedirectToAction("NotAuthorized", "Pages", "Misc");
        }*/

    [HttpGet]
    public IActionResult ResetPasswordBasic()
    {
      ViewData["FirebaseApiKey"] = _configuration["Firebase:ApiKey"];
      ViewData["FirebaseAuthDomain"] = $"{_configuration["Firebase:ProjectId"]}.firebaseapp.com";
      ViewData["FirebaseProjectId"] = _configuration["Firebase:ProjectId"];
      ViewData["FirebaseStorageBucket"] = $"{_configuration["Firebase:ProjectId"]}.appspot.com";
      ViewData["FirebaseMessagingSenderId"] = _configuration["Firebase:MessagingSenderId"] ?? "";
      ViewData["FirebaseAppId"] = _configuration["Firebase:AppId"] ?? "";

      return View();
    }
    public IActionResult ForgotPasswordBasic() => View();
    public IActionResult ForgotPasswordCover() => View();
    public IActionResult LoginCover() => View();
    public IActionResult RegisterCover() => View();
    public IActionResult RegisterMultiSteps() => View();
    // Add this new action to check if a phone exists before sending verification
    /*  [HttpPost]
      public async Task<IActionResult> CheckPhoneExists(string phoneNumber)
      {
        try
        {
          // Format phone number
          string formattedPhoneNumber = "+961" + phoneNumber.TrimStart('0');
          _logger.LogInformation($"Checking phone: {formattedPhoneNumber}");

          var user = await _userService.GetUserByPhoneNumberAsync(formattedPhoneNumber);

          if (user != null)
          {
            _logger.LogInformation($"Found user with this phone: {user.UserId}");
            // Store user ID in session for password reset
            HttpContext.Session.SetString("ResetPasswordUserId", user.UserId);
            HttpContext.Session.SetString("ResetPasswordPhoneNumber", formattedPhoneNumber);
            return Json(new { exists = true });
          }

          // FOR DEVELOPMENT ONLY - Allow testing with any phone number
          bool devMode = true; // Set to false in production
          if (devMode)
          {
            // Use first user in database for testing
            var allUsers = await _userService.GetAllAsync();
            if (allUsers.Count > 0)
            {
              var testUser = allUsers[0];
              _logger.LogWarning($"DEV MODE: Using user ID {testUser.UserId} for testing");
              HttpContext.Session.SetString("ResetPasswordUserId", testUser.UserId);
              HttpContext.Session.SetString("ResetPasswordPhoneNumber", formattedPhoneNumber);
              return Json(new { exists = true });
            }
          }

          return Json(new { exists = false, message = "Phone number not found" });
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error checking phone");
          return Json(new { exists = false, error = ex.Message });
        }
      }*/

    /*    public async Task<IActionResult> CheckPhoneExists(string phoneNumber)
        {
          try
          {
            // Format phone number for Lebanon
            string formattedPhoneNumber = "+961" + phoneNumber.TrimStart('0');
            _logger.LogInformation($"Checking if phone exists: {formattedPhoneNumber}");

            // Try to find user with this phone number
            var user = await _userService.GetUserByPhoneNumberAsync(formattedPhoneNumber);

            if (user != null)
            {
              _logger.LogInformation($"Found user with matching phone: {user.UserId}, {user.FirstName} {user.LastName}");

              // Store user ID in session for later use when resetting password
              HttpContext.Session.SetString("ResetPasswordUserId", user.UserId);

              return Json(new { exists = true });
            }

            _logger.LogWarning($"No user found with phone number: {formattedPhoneNumber}");
            return Json(new { exists = false, message = "Phone number not found in our records" });
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error checking phone existence");
            return Json(new { exists = false, error = ex.Message });
          }
        }

    */
    [HttpPost]
    public async Task<IActionResult> CheckPhoneExists(string phoneNumber)
    {
      try
      {
        _logger.LogInformation($"CHECK PHONE EXISTS - Original input: '{phoneNumber}'");

        // Create several possible formats of the phone number to check
        var possibleFormats = new List<string>();

        // Normalize the input first - remove all non-digit characters except +
        string sanitized = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());
        possibleFormats.Add(sanitized);

        // Special handling for Lebanese mobile numbers
        if (IsLebanesePhoneNumber(sanitized))
        {
          _logger.LogInformation($"Lebanese mobile number detected: {sanitized}");

          // Create Lebanese standard format for the number
          string standardFormat = FormatLebanesePhoneNumber(sanitized);
          if (!possibleFormats.Contains(standardFormat))
          {
            possibleFormats.Add(standardFormat);
          }
        }

        // If doesn't start with +, add variants with +
        if (!sanitized.StartsWith("+"))
        {
          // If it starts with 961, add + in front
          if (sanitized.StartsWith("961"))
          {
            possibleFormats.Add("+" + sanitized);
          }
          // If it starts with 0, replace with +961
          else if (sanitized.StartsWith("0"))
          {
            possibleFormats.Add("+961" + sanitized.Substring(1));
          }
          // Otherwise just add +961
          else
          {
            possibleFormats.Add("+961" + sanitized);
          }
        }

        // Also add without + and without leading zeros
        possibleFormats.Add(sanitized.Replace("+", ""));
        possibleFormats.Add(sanitized.TrimStart('0'));

        // Try to find the user
        User? foundUser = null;

        _logger.LogInformation($"CHECK PHONE EXISTS - Trying these formats: {string.Join(", ", possibleFormats)}");

        // Check all formats
        foreach (var format in possibleFormats)
        {
          _logger.LogInformation($"CHECK PHONE EXISTS - Checking format: '{format}'");
          var user = await _userService.GetUserByPhoneNumberAsync(format);
          if (user != null)
          {
            _logger.LogInformation($"CHECK PHONE EXISTS - Found user with phone format: '{format}', UserId: {user.UserId}");
            foundUser = user;
            break;
          }
        }

        // If still not found, get all users and try partial matching
        if (foundUser == null)
        {
          var allUsers = await _userService.GetAllAsync();
          _logger.LogInformation($"CHECK PHONE EXISTS - Trying partial matching with {allUsers.Count} users");

          foreach (var user in allUsers)
          {
            if (!string.IsNullOrEmpty(user.PhoneNumber))
            {
              _logger.LogInformation($"CHECK PHONE EXISTS - Examining user phone: '{user.PhoneNumber}'");

              // Try to find if any of our formats end with or are contained in the user's phone
              foreach (var format in possibleFormats)
              {
                string userPhone = user.PhoneNumber.Replace("+", "").Replace(" ", "");
                string formatPhone = format.Replace("+", "").Replace(" ", "");

                // Check if the last 8 digits match (typical Lebanese phone length)
                if (userPhone.Length >= 8 && formatPhone.Length >= 8)
                {
                  string userLast8 = userPhone.Substring(userPhone.Length - 8);
                  string formatLast8 = formatPhone.Substring(formatPhone.Length - 8);

                  _logger.LogInformation($"CHECK PHONE EXISTS - Comparing last 8: '{userLast8}' vs '{formatLast8}'");

                  if (userLast8 == formatLast8)
                  {
                    _logger.LogInformation($"CHECK PHONE EXISTS - Found match in last 8 digits! User: '{user.PhoneNumber}', Input: '{format}'");
                    foundUser = user;
                    break;
                  }
                }

                // Also check if one contains the other (for partial matches)
                if (userPhone.EndsWith(formatPhone) || formatPhone.EndsWith(userPhone))
                {
                  _logger.LogInformation($"CHECK PHONE EXISTS - Found partial match! User: '{user.PhoneNumber}', Input: '{format}'");
                  foundUser = user;
                  break;
                }
              }
            }

            if (foundUser != null) break;
          }
        }

        if (foundUser != null)
        {
          _logger.LogInformation($"CHECK PHONE EXISTS - Success! User found with ID: {foundUser.UserId}");
          HttpContext.Session.SetString("ResetPasswordUserId", foundUser.UserId ?? string.Empty);
          HttpContext.Session.SetString("ResetPasswordPhoneNumber", foundUser.PhoneNumber ?? string.Empty);
          return Json(new { exists = true });
        }

        _logger.LogWarning("CHECK PHONE EXISTS - No user found with any phone format");

        // FOR TESTING ONLY - Allow using any phone in development
        // REMOVE THIS IN PRODUCTION
        bool devMode = true; // Set to false in production
        if (devMode)
        {
          var allUsers = await _userService.GetAllAsync();
          if (allUsers.Count > 0)
          {
            var testUser = allUsers[0];
            _logger.LogWarning($"CHECK PHONE EXISTS - DEV MODE: Using user ID {testUser.UserId} for testing");
            HttpContext.Session.SetString("ResetPasswordUserId", testUser.UserId ?? string.Empty);

            // Format phone for Firebase (must start with +)
            string formattedPhone = phoneNumber;
            if (!formattedPhone.StartsWith("+"))
            {
              if (formattedPhone.StartsWith("0"))
                formattedPhone = "+961" + formattedPhone.Substring(1);
              else if (formattedPhone.StartsWith("961"))
                formattedPhone = "+" + formattedPhone;
              else
                formattedPhone = "+961" + formattedPhone;
            }

            HttpContext.Session.SetString("ResetPasswordPhoneNumber", formattedPhone);
            return Json(new { exists = true, message = "DEV MODE: Using test user" });
          }
        }

        return Json(new { exists = false, message = "Phone number not found in our records" });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking phone");
        return Json(new { exists = false, error = ex.Message });
      }
    }

    // Add this to store Firebase verification token
    [HttpPost]
    public IActionResult StoreFirebaseVerification(string token)
    {
      try
      {
        // Store verification token in session
        HttpContext.Session.SetString("FirebaseVerificationToken", token);
        HttpContext.Session.SetString("PhoneVerified", "true");

        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error storing Firebase verification");
        return Json(new { success = false, message = ex.Message });
      }
    }

    // Update the ResetPassword action
    // Update the ResetPassword action
    [HttpPost]
    public async Task<IActionResult> ResetPassword(string newPassword, string firebaseToken = "")
    {
      try
      {
        // Get user ID from session
        var userId = HttpContext.Session.GetString("ResetPasswordUserId");

        if (string.IsNullOrEmpty(userId))
        {
          _logger.LogError("No user ID found in session for password reset");
          return Json(new { success = false, message = "User identification failed. Please restart the password reset process." });
        }

        // Verify phone is verified
        var isVerified = HttpContext.Session.GetString("PhoneVerified");
        if (string.IsNullOrEmpty(isVerified) || isVerified != "true")
        {
          _logger.LogWarning("Attempt to reset password without phone verification");
          return Json(new { success = false, message = "Phone verification required" });
        }

        _logger.LogInformation($"Resetting password for user: {userId}");

        // Validate password
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
        {
          return Json(new { success = false, message = "Password must be at least 6 characters" });
        }

        // Hash the password and update
        string hashedPassword = _userService.HashPassword(newPassword);
        bool updated = await _userService.UpdateUserPasswordAsync(userId, hashedPassword);

        if (updated)
        {
          _logger.LogInformation("Password reset successful");

          // Clear session data
          HttpContext.Session.Remove("ResetPasswordUserId");
          HttpContext.Session.Remove("ResetPasswordPhoneNumber");
          HttpContext.Session.Remove("PhoneVerified");
          HttpContext.Session.Remove("FirebaseVerificationToken");

          return Json(new { success = true, message = "Password reset successfully" });
        }
        else
        {
          _logger.LogWarning("Password update failed in database");
          return Json(new { success = false, message = "Failed to update password. Please try again." });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in password reset");
        return Json(new { success = false, message = "An error occurred: " + ex.Message });
      }
    }
    // Add this to store Firebase verification token
    [HttpPost]
    /*    public IActionResult StoreFirebaseVerification(string token)
        {
          try
          {
            // Store verification token in session
            HttpContext.Session.SetString("FirebaseVerificationToken", token);
            HttpContext.Session.SetString("PhoneVerified", "true");

            return Json(new { success = true });
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error storing Firebase verification");
            return Json(new { success = false, message = ex.Message });
          }
        }*/

    /*    [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword)
        {
          try
          {
            // Check if phone is verified
            var isVerified = HttpContext.Session.GetString("PhoneVerified");
            if (string.IsNullOrEmpty(isVerified) || isVerified != "true")
            {
              return Json(new { success = false, message = "Phone verification required" });
            }

            // Get user ID from session
            var userId = HttpContext.Session.GetString("ResetPasswordUserId");
            if (string.IsNullOrEmpty(userId))
            {
              return Json(new { success = false, message = "Session expired. Please try again." });
            }

            // Hash the new password
            string hashedPassword = _userService.HashPassword(newPassword);

            // Update the user's password in Firestore
            bool updated = await _userService.UpdateUserPasswordAsync(userId, hashedPassword);

            if (updated)
            {
              // Clear reset password session data
              HttpContext.Session.Remove("ResetPasswordVerificationId");
              HttpContext.Session.Remove("ResetPasswordPhoneNumber");
              HttpContext.Session.Remove("PhoneVerified");
              HttpContext.Session.Remove("ResetPasswordUserId");
              HttpContext.Session.Remove("FirebaseVerificationToken");

              return Json(new { success = true, message = "Password reset successfully" });
            }
            else
            {
              return Json(new { success = false, message = "Failed to reset password" });
            }
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error resetting password");
            return Json(new { success = false, message = "An error occurred: " + ex.Message });
          }
        }*/
    /*    [HttpPost]
        public async Task<IActionResult> ResetPassword(string newPassword, string firebaseToken)
        {
          try
          {
            _logger.LogInformation("Password reset requested");

            // First check if we have a user ID in session
            var userId = HttpContext.Session.GetString("ResetPasswordUserId");

            if (string.IsNullOrEmpty(userId))
            {
              _logger.LogWarning("No user ID in session, checking phone number");

              // Try to get user by phone number
              var phoneNumber = HttpContext.Session.GetString("ResetPasswordPhoneNumber");
              if (!string.IsNullOrEmpty(phoneNumber))
              {
                var user = await _userService.GetUserByPhoneNumberAsync(phoneNumber);
                if (user != null)
                {
                  userId = user.UserId;
                  _logger.LogInformation($"Found user by phone: {userId}");
                }
              }
            }

            // If still no user ID and we have a Firebase token
            if (string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(firebaseToken))
            {
              _logger.LogWarning($"Trying to find user by Firebase ID: {firebaseToken}");

              // For development/testing - create a temporary mapping
              // In production, you'd need to properly map Firebase UIDs to your user IDs
              var allUsers = await _userService.GetAllAsync();
              if (allUsers.Count > 0)
              {
                // Just use first user for demonstration
                userId = allUsers[0].UserId;
                _logger.LogWarning($"TEST MODE: Using first user ({userId}) regardless of Firebase token");
              }
            }

            if (string.IsNullOrEmpty(userId))
            {
              _logger.LogError("Failed to identify user for password reset");
              return Json(new { success = false, message = "User identification failed" });
            }

            // Update password
            string hashedPassword = _userService.HashPassword(newPassword);
            bool updated = await _userService.UpdateUserPasswordAsync(userId, hashedPassword);

            if (updated)
            {
              _logger.LogInformation($"Password updated successfully for user {userId}");
              return Json(new { success = true, message = "Password reset successfully" });
            }
            else
            {
              return Json(new { success = false, message = "Failed to update password" });
            }
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Error in password reset");
            return Json(new { success = false, message = ex.Message });
          }
        }
    */
    //[HttpPost]
    //public async Task<IActionResult> VerifyOtp(string otp)
    //{
    //  try
    //  {
    //    // Existing verification code...

    //    if (isVerified)
    //    {
    //      // Mark as verified in session
    //      HttpContext.Session.SetString("PhoneVerified", "true");

    //      // Get the phone number from session
    //      var phoneNumber = HttpContext.Session.GetString("ResetPasswordPhoneNumber");

    //      // Find the user by phone number and store their ID
    //      if (!string.IsNullOrEmpty(phoneNumber))
    //      {
    //        var user = await _userService.GetUserByPhoneNumberAsync(phoneNumber);
    //        if (user != null)
    //        {
    //          // Store user ID in session
    //          HttpContext.Session.SetString("ResetPasswordUserId", user.UserId);
    //          _logger.LogInformation($"User found and ID stored in session: {user.UserId}");
    //        }
    //        else
    //        {
    //          _logger.LogWarning($"No user found with phone number: {phoneNumber}");
    //        }
    //      }

    //      return Json(new { success = true, message = "Verification successful" });
    //    }
    //    else
    //    {
    //      return Json(new { success = false, message = "Invalid verification code" });
    //    }
    //  }
    //  catch (Exception ex)
    //  {
    //    // Error handling...
    //  }
    //}

    public IActionResult TwoStepsBasic() => View();
    public IActionResult TwoStepsCover() => View();
    public IActionResult VerifyEmailBasic() => View();
    public IActionResult VerifyEmailCover() => View();

    private (string userRole, bool isAdmin, bool isAffiliate, bool isCustomer) GetUserRoleInfo()
    {
      return (
          HttpContext.Session.GetString("UserRole") ?? string.Empty,
          HttpContext.Session.GetString("IsAdmin") == "1",
          HttpContext.Session.GetString("IsAffiliate") == "1",
          HttpContext.Session.GetString("IsCustomer") == "1"
      );
    }

    // Helper method to check if a phone number is Lebanese
    private bool IsLebanesePhoneNumber(string phoneNumber)
    {
      if (string.IsNullOrEmpty(phoneNumber))
        return false;

      // Remove any non-digit characters
      string digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());

      if (string.IsNullOrEmpty(digitsOnly))
        return false;

      // Check for standard Lebanese mobile prefixes
      // 03, 70, 71, 76, 78, 79, etc.
      if (digitsOnly.StartsWith("03") ||
          digitsOnly.StartsWith("3") ||
          digitsOnly.StartsWith("70") ||
          digitsOnly.StartsWith("71") ||
          digitsOnly.StartsWith("76") ||
          digitsOnly.StartsWith("78") ||
          digitsOnly.StartsWith("79"))
      {
        return true;
      }

      // Check for prefixes with country code
      if (digitsOnly.StartsWith("961") && digitsOnly.Length >= 4)
      {
        string prefix = digitsOnly.Substring(3, 1);
        if (prefix == "3")
          return true;

        if (digitsOnly.Length >= 5)
        {
          string prefix2 = digitsOnly.Substring(3, 2);
          if (prefix2 == "70" || prefix2 == "71" || prefix2 == "76" || prefix2 == "78" || prefix2 == "79")
            return true;
        }
      }

      return false;
    }

    // Helper method to format Lebanese phone numbers into a standardized format
    private string FormatLebanesePhoneNumber(string phoneNumber)
    {
      // First remove any non-digit characters except +
      string cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

      // Extract just the mobile portion (without country code)
      string mobileNumber;

      if (cleaned.StartsWith("+9613") || cleaned.StartsWith("+9617"))
      {
        // Number is already in full international format
        return cleaned;
      }
      else if (cleaned.StartsWith("+961"))
      {
        // Number has country code
        mobileNumber = cleaned.Substring(4);
      }
      else if (cleaned.StartsWith("961"))
      {
        // Has country code without +
        mobileNumber = cleaned.Substring(3);
      }
      else if (cleaned.StartsWith("0"))
      {
        // Starts with local 0
        mobileNumber = cleaned.Substring(1);
      }
      else
      {
        // Assume it's already just the mobile number
        mobileNumber = cleaned;
      }

      // If it starts with 3 or 7, it's likely the core mobile number
      if (mobileNumber.StartsWith("3") || mobileNumber.StartsWith("7"))
      {
        return "+961" + mobileNumber;
      }

      // If we couldn't properly format it, just return with +961 prefix
      return "+961" + mobileNumber;
    }
  }
}
