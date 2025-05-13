using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services;
using Microsoft.Extensions.Configuration;
using Google.Cloud.Firestore;

namespace AspnetCoreMvcFull.Controllers;

public class PagesController : Controller
{
  private readonly UserService _userService;
  private readonly AddressService _addressService;
  private readonly IConfiguration _configuration;

  public PagesController(UserService userService, AddressService addressService, IConfiguration configuration)
  {
    _userService = userService;
    _addressService = addressService;
    _configuration = configuration;
  }
  public IActionResult AccountSettings()
  {
    // Get user ID from session
    string userId = HttpContext.Session.GetString("UserId");

    // Pass user ID to view
    ViewBag.UserId = userId;

    // Also pass Firebase configuration
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseAuthDomain = _configuration["Firebase:AuthDomain"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];

    return View();
  }
  public IActionResult AccountSettingsBilling()
  {
    // Get user ID from session
    string userId = HttpContext.Session.GetString("UserId");

    // Pass user ID to view
    ViewBag.UserId = userId;

    // Also pass Firebase configuration
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseAuthDomain = _configuration["Firebase:AuthDomain"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];

    return View();
  }
  public IActionResult AccountSettingsConnections()
  {
    // Get user ID from session
    string userId = HttpContext.Session.GetString("UserId");

    // Pass user ID to view
    ViewBag.UserId = userId;

    // Also pass Firebase configuration
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseAuthDomain = _configuration["Firebase:AuthDomain"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];

    return View();
  }
  public IActionResult AccountSettingsNotifications()
  {
    // Get user ID from session
    string userId = HttpContext.Session.GetString("UserId");

    // Pass user ID to view
    ViewBag.UserId = userId;

    // Also pass Firebase configuration
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseAuthDomain = _configuration["Firebase:AuthDomain"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];

    return View();
  }
  public IActionResult AccountSettingsSecurity()
  {
    // Get user ID from session
    string userId = HttpContext.Session.GetString("UserId");

    // Pass user ID to view
    ViewBag.UserId = userId;

    // Also pass Firebase configuration
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseAuthDomain = _configuration["Firebase:AuthDomain"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];

    return View();
  }
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
    // If no ID is provided, try to get the current user's ID from session
    if (string.IsNullOrEmpty(id))
    {
      id = HttpContext.Session.GetString("UserId");
    }

    // If still no user ID, redirect to login
    if (string.IsNullOrEmpty(id))
    {
      return RedirectToAction("LoginBasic", "Auth", new { returnUrl = Url.Action("ProfileUser", "Pages") });
    }

    // Pass only the userId to the view, we'll load the data using JavaScript
    ViewBag.UserId = id;
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseAuthDomain = _configuration["Firebase:AuthDomain"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];

    return View();
  }

  public IActionResult ProfileTeams() => View();
  public IActionResult ProfileProjects() => View();

  [HttpGet]
  public IActionResult CheckServiceAccount()
  {
    try
    {
      var credentialsPath = _configuration["Firebase:ServiceAccountKeyPath"];
      return Content($"Service account path: {credentialsPath}, Exists: {System.IO.File.Exists(credentialsPath)}");
    }
    catch (Exception ex)
    {
      return Content($"Error: {ex.Message}");
    }
  }

  [HttpGet]
  public async Task<IActionResult> TestFirestoreConnection()
  {
    try
    {
      var firestoreDb = HttpContext.RequestServices.GetRequiredService<FirestoreDb>();
      var collection = firestoreDb.Collection("users");
      var snapshot = await collection.Limit(1).GetSnapshotAsync();
      return Content($"Success! Found {snapshot.Count} docs");
    }
    catch (Exception ex)
    {
      return Content($"Error: {ex.Message}");
    }
  }

  [HttpGet]
  public IActionResult TestAuth()
  {
    try
    {
      var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
      var exists = path != null ? System.IO.File.Exists(path) : false;
      var localPath = "firebase-adminsdk.json";
      var localExists = System.IO.File.Exists(localPath);

      return Content($"Env Path: {path}, File exists: {exists}\n" +
                    $"Local Path: {localPath}, File exists: {localExists}\n" +
                    $"Current directory: {Environment.CurrentDirectory}");
    }
    catch (Exception ex)
    {
      return Content($"Error: {ex.Message}");
    }
  }
}
