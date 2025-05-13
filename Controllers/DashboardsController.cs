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

    // Check user role and redirect to appropriate dashboard
    var isAdmin = HttpContext.Session.GetString("IsAdmin") == "1";
    var isAffiliate = HttpContext.Session.GetString("IsAffiliate") == "1";
    var isCustomer = HttpContext.Session.GetString("IsCustomer") == "1";

    // Redirect customers to their specific dashboard
    if (isCustomer && !isAdmin && !isAffiliate)
    {
      return RedirectToAction("Index", "Customer");
    }

    // Redirect affiliates to their specific dashboard if they're not also admins
    if (isAffiliate && !isAdmin)
    {
      return RedirectToAction("Dashboard", "Affiliate");
    }

    // Admins see the admin dashboard
    if (isAdmin)
    {
      return RedirectToAction("Dashboard", "Admin");
    }

    // If not an admin, customer, or affiliate, redirect to login
    return RedirectToAction("LoginBasic", "Auth");
  }

  //public IActionResult CRM() => View();
}
