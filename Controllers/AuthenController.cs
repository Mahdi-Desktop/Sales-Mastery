using AspnetCoreMvcFull.Services.Interface;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class AuthController : Controller
  {
    private readonly IFirebaseAuthService _authService;

    public AuthController(IFirebaseAuthService authService)
    {
      _authService = authService;
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
      try
      {
        var user = await _authService.LoginAsync(email, password);

        // Create claims
        var claims = new[]
        {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Sign in
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
              IsPersistent = true
            });

        // Redirect based on role
        return user.Role switch
        {
          "Admin" => RedirectToAction("Index", "Admin"),
          "Affiliate" => RedirectToAction("Index", "Affiliate"),
          _ => RedirectToAction("Index", "Store")
        };
      }
      catch (Exception ex)
      {
        ModelState.AddModelError(string.Empty, ex.Message);
        return View("LoginBasic");
      }
    }

    [HttpPost]
    public async Task<IActionResult> SignUp(string email, string password, string role)
    {
      try
      {
        // In production, get creatorId from current user
        var creatorId = "system";
        await _authService.SignUpAsync(email, password, role, creatorId);
        return RedirectToAction("LoginBasic");
      }
      catch (Exception ex)
      {
        ModelState.AddModelError(string.Empty, ex.Message);
        return View("RegisterBasic");
      }
    }

    public async Task<IActionResult> Logout()
    {
      await HttpContext.SignOutAsync();
      return RedirectToAction("LoginBasic");
    }

    // View actions
    public IActionResult LoginBasic() => View();
    public IActionResult RegisterBasic() => View();
    // ... other view methods
  }
}
