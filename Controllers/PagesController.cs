using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Controllers;

public class PagesController : Controller
{
  private readonly UserService _userService;
  private readonly AddressService _addressService;

  public PagesController(UserService userService, AddressService addressService)
  {
    _userService = userService;
    _addressService = addressService;
  }
  public IActionResult AccountSettings() => View();
  public IActionResult AccountSettingsBilling() => View();
  public IActionResult AccountSettingsConnections() => View();
  public IActionResult AccountSettingsNotifications() => View();
  public IActionResult AccountSettingsSecurity() => View();
  public IActionResult FAQ() => View();
  public IActionResult MiscComingSoon() => View();
  public IActionResult MiscError() => View();
  public IActionResult MiscNotAuthorized()
  {
    // If user is logged in, redirect to dashboard
    if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserId")))
    {
      return RedirectToAction("Index", "Dashboards");
    }

    return View("MiscNotAuthorized");
  }

  public IActionResult MiscUnderMaintenance() => View();
  public IActionResult Pricing() => View();
  public IActionResult ProfileConnections() => View();
  /*  public IActionResult ProfileUser() => View();*/
  public async Task<IActionResult> ProfileUser(string id)
  {
    if (string.IsNullOrEmpty(id))
    {
      // If no ID is provided, try to get the current user's ID from session
      id = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(id))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }
    }

    // You need to inject UserService and AddressService in the controller constructor
    var user = await _userService.GetUserByIdAsync(id);
    if (user == null)
    {
      return NotFound();
    }

    // Load addresses and set in ViewBag
    ViewBag.Addresses = await _addressService.GetAddressesByUserIdAsync(id);

    return View(user);
  }

  public IActionResult ProfileTeams() => View();
  public IActionResult ProfileProjects() => View();
}
