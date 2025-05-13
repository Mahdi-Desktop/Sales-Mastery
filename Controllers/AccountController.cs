using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.DTO;
using System.Text.Json;
using System;

namespace AspnetCoreMvcFull.Controllers
{
  public class AccountController : Controller
  {
    private readonly UserService _userService;
    private readonly AddressService _addressService;
    private readonly AffiliateService _affiliateService;
    private readonly ILogger<AccountController> _logger;
    
    public AccountController(
        UserService userService,
        AddressService addressService,
        AffiliateService affiliateService,
        ILogger<AccountController> logger)
    {
      _userService = userService;
      _addressService = addressService;
      _affiliateService = affiliateService;
      _logger = logger;
    }

    // Account Settings - Main Profile
    public async Task<IActionResult> Settings()
    {
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      try
      {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
          return NotFound();
        }

        // Get user's addresses
        var addresses = await _addressService.GetAddressesByUserIdAsync(userId);
        ViewBag.PrimaryAddress = addresses.FirstOrDefault();

        // Check if user is an affiliate
        if (user.Role == "2")  // Affiliate role
        {
          var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(userId);
          ViewBag.AffiliateInfo = affiliate;
        }

        return View("AccountSettings", user);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error retrieving user account information for {userId}");
        TempData["ErrorMessage"] = "An error occurred while loading your account information.";
        return View("AccountSettings");
      }
    }

    // Account Settings - Security
    public async Task<IActionResult> Security()
    {
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      try
      {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
          return NotFound();
        }

        return View("AccountSettingsSecurity", user);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error retrieving user security information for {userId}");
        TempData["ErrorMessage"] = "An error occurred while loading your security information.";
        return View("AccountSettingsSecurity");
      }
    }

    // Profile view
    public async Task<IActionResult> Profile()
    {
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      try
      {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
          return NotFound();
        }

        // Get additional data
        var addresses = await _addressService.GetAddressesByUserIdAsync(userId);
        ViewBag.Addresses = addresses;

        // If affiliate, get affiliate data
        if (user.Role == "2")
        {
          var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(userId);
          ViewBag.AffiliateInfo = affiliate;
        }

        return View("ProfileUser", user);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error retrieving user profile for {userId}");
        TempData["ErrorMessage"] = "An error occurred while loading your profile.";
        return View("ProfileUser");
      }
    }

    // Update account information
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAccount(User model)
    {
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      try
      {
        var existingUser = await _userService.GetUserByIdAsync(userId);
        if (existingUser == null)
        {
          return NotFound();
        }

        // Update user fields
        existingUser.FirstName = model.FirstName;
        existingUser.LastName = model.LastName;
        existingUser.PhoneNumber = model.PhoneNumber;

        // Keep sensitive information from existing user
        existingUser.Password = null;  // Don't update password here
        existingUser.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

        await _userService.UpdateAsync(userId, existingUser);

        TempData["SuccessMessage"] = "Your account has been updated successfully!";
        return RedirectToAction("Settings");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating user account for {userId}");
        TempData["ErrorMessage"] = "An error occurred while updating your account.";
        return RedirectToAction("Settings");
      }
    }


    // Update security settings (password)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSecurity(string currentPassword, string newPassword, string confirmPassword)
    {
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      // Validate passwords
      if (newPassword != confirmPassword)
      {
        TempData["ErrorMessage"] = "New password and confirm password do not match.";
        return RedirectToAction("Security");
      }

      try
      {
        var result = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);

        if (result.Success)
        {
          TempData["SuccessMessage"] = "Your password has been updated successfully!";
        }
        else
        {
          TempData["ErrorMessage"] = result.Message;
        }

        return RedirectToAction("Security");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating security settings for {userId}");
        TempData["ErrorMessage"] = "An error occurred while updating your security settings.";
        return RedirectToAction("Security");
      }
    }
  }
}
