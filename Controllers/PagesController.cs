using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers;

public class PagesController : Controller
{
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
  public IActionResult ProfileUser() => View();
  public IActionResult ProfileTeams() => View();
  public IActionResult ProfileProjects() => View();
}
