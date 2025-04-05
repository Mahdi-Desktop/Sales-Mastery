using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers;

public class DashboardsController : Controller
{
  public IActionResult Index()
  {
    // Verify the user is logged in
    var userId = HttpContext.Session.GetString("UserId");
    if (string.IsNullOrEmpty(userId))
    {
      return RedirectToAction("LoginBasic", "Auth");
    }

    return View();
  }

  public IActionResult CRM() => View();
}
