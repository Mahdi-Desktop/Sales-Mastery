using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers
{
  public class CustomerController : Controller
  {
    public IActionResult Index()
    {
      // Verify the user is logged in
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      // Verify this is a customer
      var isCustomer = HttpContext.Session.GetString("IsCustomer") == "1";
      if (!isCustomer)
      {
        return RedirectToAction("Index", "Dashboards");
      }

      // Pass user context to view
      ViewBag.UserId = HttpContext.Session.GetString("UserId");
      ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
      ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
      ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

      // Pass Firebase config to view
      ViewBag.FirebaseApiKey = "AIzaSyACWsakIQomRmJZShEOrXJ2z-XQOSr9Q5g";
      ViewBag.FirebaseProjectId = "asp-sales";
      ViewBag.FirebaseStorageBucket = "asp-sales.firebasestorage.app";
      ViewBag.FirebaseMessagingSenderId = "277356792073";
      ViewBag.FirebaseAppId = "1:277356792073:web:5d676341f60b446cd96bd8";

      return View();
    }
  }
}
