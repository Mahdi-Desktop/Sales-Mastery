using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;

namespace AspnetCoreMvcFull.Attributes
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
  public class RoleRequirementAttribute : Attribute, IAuthorizationFilter
  {
    private readonly string[] _allowedRoles;

    public RoleRequirementAttribute(params string[] allowedRoles)
    {
      _allowedRoles = allowedRoles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
      // Check if user is authenticated at all
      if (context.HttpContext.Session.GetString("UserId") == null)
      {
        // Redirect to login if not authenticated
        context.Result = new RedirectToActionResult("Login", "Auth", null);
        return;
      }

      // Check if the user's role is in the allowed roles list
      var isAdmin = context.HttpContext.Session.GetString("IsAdmin") == "1";
      var isAffiliate = context.HttpContext.Session.GetString("IsAffiliate") == "1";
      var isCustomer = context.HttpContext.Session.GetString("IsCustomer") == "1";

      bool hasAccess = false;

      foreach (var role in _allowedRoles)
      {
        if ((role == "Admin" && isAdmin) ||
            (role == "Affiliate" && isAffiliate) ||
            (role == "Customer" && isCustomer))
        {
          hasAccess = true;
          break;
        }
      }

      if (!hasAccess)
      {
        // Redirect to not authorized page
        context.Result = new RedirectToActionResult("MiscNotAuthorized", "Pages", null);
      }
    }
  }
}
