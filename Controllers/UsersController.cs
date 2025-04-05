using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AspnetCoreMvcFull.Controllers;

public class UsersController : Controller
{
  private readonly FirestoreDb _firestoreDb;
  private readonly IConfiguration _configuration;
  private readonly UserService _userService;
  private readonly InvoiceService _invoiceService;
  private readonly AddressService _addressService;
  private readonly AffiliateService _affiliateService;
  private readonly CustomerService _customerService;
  private readonly ILogger<UsersController> _logger;

  public UsersController(
      UserService userService,
      InvoiceService invoiceService,
      AddressService addressService,
      AffiliateService affiliateService,
      CustomerService customerService,
      IConfiguration configuration,
      ILogger<UsersController> logger)
  {
    _userService = userService;
    _invoiceService = invoiceService;
    _addressService = addressService;
    _affiliateService = affiliateService;
    _customerService = customerService;
    _configuration = configuration;
    _logger = logger;
    _firestoreDb = FirestoreDb.Create(_configuration["Firebase:ProjectId"]);
  }

  public async Task<IActionResult> List()
  {
    var users = await _userService.GetAllAsync();
    return View(users);
  }

  [HttpGet]
  public async Task<JsonResult> GetUsers()
  {
    var users = await _userService.GetAllAsync();
    return Json(new { data = users });
  }

  [HttpGet]
  public async Task<JsonResult> GetUser(string id)
  {
    var user = await _userService.GetByIdAsync(id);
    return Json(user);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> AddUser(User user)
  {
    ModelState.Clear();

    user.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
    user.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
    user.CreatedBy = User.Identity.IsAuthenticated ? User.FindFirstValue(ClaimTypes.NameIdentifier) : "system";

    if (TryValidateModel(user))
    {
      try
      {
        string userId = await _userService.AddAsync(user);
        string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (user.Role == "Affiliate" && User.IsInRole("Admin"))
        {
          await _affiliateService.AddAffiliateAsync(userId, currentUserId);
        }
        else if (user.Role == "Customer")
        {
          await _customerService.AddCustomerAsync(userId, currentUserId);
        }

        return RedirectToAction(nameof(List));
      }
      catch (Exception ex)
      {
        ModelState.AddModelError("", "Error adding user: " + ex.Message);
      }
    }

    return View("List", await _userService.GetAllAsync());
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> UpdateUser(UserUpdateDto updateDto)
  {
    if (ModelState.IsValid)
    {
      var result = await _userService.UpdateUserWithVerificationAsync(updateDto);

      TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;

      if (!result.Success && result.Message == "Password verification failed")
      {
        ViewBag.EditUserData = updateDto;
        ViewBag.EditUserError = result.Message;
        return View("List", await _userService.GetAllAsync());
      }

      return RedirectToAction(nameof(List));
    }

    TempData["ErrorMessage"] = "Validation failed: " + string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
    return RedirectToAction(nameof(List));
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> DeleteUser(string id)
  {
    await _userService.DeleteAsync(id);
    return RedirectToAction(nameof(List));
  }

  public async Task<IActionResult> ViewAccount(string id)
  {
    if (string.IsNullOrEmpty(id)) return RedirectToAction("List");

    var user = await _userService.GetByIdAsync(id);
    if (user == null) return NotFound();

    ViewBag.Invoices = await _invoiceService.GetInvoicesByUserIdAsync(id);
    if (user.Role == "Affiliate")
    {
      ViewBag.ReferencedUsers = await _customerService.GetCustomersByReferrerAsync(id);
      ViewBag.CommissionHistory = await GetCommissionHistoryForAffiliate(id);
    }

    return View(user);
  }
  public async Task<IActionResult> ViewSecurity(string id)
  {
    if (string.IsNullOrEmpty(id)) return RedirectToAction("List");

    var user = await _userService.GetByIdAsync(id);
    if (user == null) return NotFound();

    return View(user);
  }


  private async Task<List<dynamic>> GetCommissionHistoryForAffiliate(string affiliateId)
  {
    try
    {
      var commissionSnapshot = await _firestoreDb.Collection("commissions")
          .WhereEqualTo("AffiliateId", affiliateId)
          .OrderByDescending("CreatedAt")
          .GetSnapshotAsync();

      return commissionSnapshot.Documents
          .Select(doc => doc.ConvertTo<Dictionary<string, object>>())
          .Select(commission => (dynamic)new
          {
            OrderId = commission["OrderId"].ToString(),
            Amount = Convert.ToDecimal(commission["Amount"]),
            CreatedAt = (Timestamp)commission["CreatedAt"]
          })
          .ToList();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error getting commission history for affiliate {affiliateId}");
      return new List<dynamic>();
    }
  }

  public async Task<IActionResult> ViewBilling(string id)
  {
    if (string.IsNullOrEmpty(id)) return RedirectToAction("List");
    var user = await _userService.GetByIdAsync(id);
    if (user == null) return NotFound();

    ViewBag.Invoices = await _invoiceService.GetInvoicesByUserIdAsync(id);
    ViewBag.BillingAddress = (await _addressService.GetAddressesByUserIdAsync(id)).FirstOrDefault();

    return View(user);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> ChangePassword(string userId, string currentPassword, string newPassword, string confirmPassword)
  {
    if (string.IsNullOrEmpty(userId) || newPassword != confirmPassword)
    {
      return Json(new { success = false, message = "Invalid input." });
    }

    try
    {
      var result = await _userService.ChangePasswordAsync(userId, currentPassword, newPassword);
      return Json(new { success = result.Success, message = result.Message });
    }
    catch (Exception ex)
    {
      return Json(new { success = false, message = ex.Message });
    }
  }
}
