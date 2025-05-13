using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Common method to pass Firebase config and check admin access
        private IActionResult CheckAccessAndSetupView()
        {
            // Verify the user is logged in
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("LoginBasic", "Auth");
            }

            // Verify the user is an admin
            var isAdmin = HttpContext.Session.GetString("IsAdmin") == "1";
            if (!isAdmin)
            {
                return RedirectToAction("Index", "Dashboards");
            }

            // Pass Firebase config to view
            ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"] ?? "AIzaSyACWsakIQomRmJZShEOrXJ2z-XQOSr9Q5g";
            ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"] ?? "asp-sales";
            ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"] ?? "asp-sales.firebasestorage.app";
            ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"] ?? "277356792073";
            ViewBag.FirebaseAppId = _configuration["Firebase:AppId"] ?? "1:277356792073:web:5d676341f60b446cd96bd8";

            // Pass user context to view
            ViewBag.UserId = userId;
            ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
            ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
            ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

            return null; // Continue with the action
        }

        public IActionResult Dashboard()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult Orders()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult OrderDetails(string id)
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            ViewBag.OrderId = id;
            return View();
        }

        public IActionResult Affiliates()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult AffiliateDetails(string id)
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            ViewBag.AffiliateId = id;
            return View();
        }

        public IActionResult Customers()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult CustomerDetails(string id)
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            ViewBag.CustomerId = id;
            return View();
        }

        public IActionResult Products()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult Commissions()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult Reports()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }

        public IActionResult Settings()
        {
            var result = CheckAccessAndSetupView();
            if (result != null) return result;

            return View();
        }
    }
}
