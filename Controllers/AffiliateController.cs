using Microsoft.AspNetCore.Mvc;

public class AffiliateController : Controller
{
  private readonly IConfiguration _configuration;

  public AffiliateController(IConfiguration configuration)
  {
    _configuration = configuration;
  }

  // Common method to pass Firebase config to all views
  private void SetFirebaseConfig()
  {
    ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
    ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
    ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
    ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
    ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];
  }

  public IActionResult Dashboard()
  {
    SetFirebaseConfig();
    ViewBag.UserId = HttpContext.Session.GetString("UserId");
    ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
    ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
    ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

    return View();
  }

  public IActionResult Customers()
  {
    SetFirebaseConfig();
    ViewBag.UserId = HttpContext.Session.GetString("UserId");
    ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
    ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
    ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

    return View();
  }

  public IActionResult Commissions()
  {
    SetFirebaseConfig();
    ViewBag.UserId = HttpContext.Session.GetString("UserId");
    ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
    ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
    ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

    return View();
  }

  public IActionResult Manage()
  {
    SetFirebaseConfig();
    ViewBag.UserId = HttpContext.Session.GetString("UserId");
    ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
    ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
    ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

    return View();
  }

  public IActionResult Create()
  {
    SetFirebaseConfig();
    ViewBag.UserId = HttpContext.Session.GetString("UserId");
    ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
    ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
    ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

    return View();
  }

  public IActionResult Settings()
  {
    SetFirebaseConfig();
    ViewBag.UserId = HttpContext.Session.GetString("UserId");
    ViewBag.IsAdmin = HttpContext.Session.GetString("IsAdmin");
    ViewBag.IsAffiliate = HttpContext.Session.GetString("IsAffiliate");
    ViewBag.IsCustomer = HttpContext.Session.GetString("IsCustomer");

    return View();
  }
}
