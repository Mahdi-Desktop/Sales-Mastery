using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace AspnetCoreMvcFull.Attributes
{
  public class RoleAuthorizeAttribute : TypeFilterAttribute
  {
    public RoleAuthorizeAttribute(string role) : base(typeof(RoleAuthorizeFilter))
    {
      Arguments = new object[] { role };
    }
  }

  public class RoleAuthorizeFilter : IAuthorizationFilter
  {
    private readonly string _role;

    public RoleAuthorizeFilter(string role)
    {
      _role = role;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
      var userRole = context.HttpContext.Session.GetString("Role");

      // No role means not logged in
      if (string.IsNullOrEmpty(userRole))
      {
        context.Result = new RedirectToActionResult("Login", "Auth", null);
        return;
      }

      // Check if user has the required role
      bool hasAccess = false;

      switch (_role)
      {
        case "Admin":
          hasAccess = userRole == "1";
          break;
        case "Affiliate":
          hasAccess = userRole == "1" || userRole == "2";
          break;
        case "Customer":
          hasAccess = userRole == "1" || userRole == "2" || userRole == "3";
          break;
        default:
          hasAccess = false;
          break;
      }

      if (!hasAccess)
      {
        context.Result = new RedirectToActionResult("NotAuthorized", "Pages", null);
      }
    }
  }
}
