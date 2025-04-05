using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Interfaces;
using global::AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class AuthController : Controller
  {
    private readonly AuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AuthService authService, ILogger<AuthController> logger)
    {
      _authService = authService;
      _logger = logger;
    }

    [HttpGet]
    public IActionResult LoginBasic()
    {
      return View();
    }
    [HttpPost]
    public async Task<IActionResult> LoginBasic(LoginRequest loginRequest)
    {
      if (!ModelState.IsValid)
      {
        return View(loginRequest);
      }

      try
      {
        var (success, user, message) = await _authService.LoginAsync(loginRequest.Email, loginRequest.Password);

        if (success && user != null)
        {
          // Store user information in session
          var userJson = JsonSerializer.Serialize(user);
          HttpContext.Session.SetString("CurrentUser", userJson);
          HttpContext.Session.SetString("UserId", user.UserId);

          // Store role information
          HttpContext.Session.SetString("UserRole", user.Role);

          // Set role-specific boolean flags for navigation visibility
          HttpContext.Session.SetString("IsAdmin", "0");
          HttpContext.Session.SetString("IsAffiliate", "0");
          HttpContext.Session.SetString("IsCustomer", "0");

          // Set the appropriate role to 1 (true) based on user.Role
          switch (user.Role)
          {
            case "1": // Admin role
              HttpContext.Session.SetString("IsAdmin", "1");
              break;

            case "2": // Affiliate role
              HttpContext.Session.SetString("IsAffiliate", "1");
              break;

            case "3": // Customer role
              HttpContext.Session.SetString("IsCustomer", "1");
              break;
          }

          return RedirectToAction("Index", "Dashboards");
        }
        else
        {
          ModelState.AddModelError(string.Empty, message);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during login attempt");
        ModelState.AddModelError(string.Empty, "An unexpected error occurred during login.");
      }

      return View(loginRequest);
    }



    [HttpGet]
    public IActionResult Logout()
    {
      // Clear the session
      HttpContext.Session.Clear();

      // Redirect to the Not Authorized page
      return RedirectToAction("NotAuthorized", "Pages", "Misc");
    }
  }
}


/*  public class AuthController : Controller
  {
    private readonly IFirebaseAuthService _firebaseAuthService;

    public AuthController(IFirebaseAuthService firebaseAuthService)
    {
      _firebaseAuthService = firebaseAuthService;
    }

    [HttpGet]
    public IActionResult LoginBasic() => View();

    [HttpPost]
    public async Task<IActionResult> LoginBasic(User users)
    {
      if (!ModelState.IsValid)
      {
        return View(users);
      }

      try
      {
        var user = await _firebaseAuthService.Login(users.Email, users.Password);
        if (user != null)
        {
          // Set session or cookie as needed
          HttpContext.Session.SetString("token", user);
          return RedirectToAction("Index", "Dashboards");
        }
      }
      catch
      {
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
      }

      return View(users);
    }

    [HttpGet]
    public IActionResult RegisterBasic() => View();

    [HttpPost]
    public async Task<IActionResult> RegisterBasic(SignUpusers signUpusers)
    {
      if (!ModelState.IsValid)
      {
        return View(signUpusers);
      }

      try
      {
        var userId = await _firebaseAuthService.SignUp(signUpusers.Email, signUpusers.Password);
        if (userId != null)
        {
          return RedirectToAction("LoginBasic");
        }
      }
      catch
      {
        ModelState.AddModelError(string.Empty, "Registration failed.");
      }

      return View(signUpusers);
    }
  
  public IActionResult ForgotPasswordBasic() => View();
  public IActionResult ForgotPasswordCover() => View();
  //public IActionResult LoginBasic() => View();
  public IActionResult LoginCover() => View();
  //public IActionResult RegisterBasic() => View();
  public IActionResult RegisterCover() => View();
  public IActionResult RegisterMultiSteps() => View();
  public IActionResult ResetPasswordBasic() => View();
  public IActionResult ResetPasswordCover() => View();
  public IActionResult TwoStepsBasic() => View();
  public IActionResult TwoStepsCover() => View();
  public IActionResult VerifyEmailBasic() => View();
  public IActionResult VerifyEmailCover() => View();
}*/
