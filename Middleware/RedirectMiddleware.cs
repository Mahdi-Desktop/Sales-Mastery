using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Middleware
{
  public class RedirectMiddleware
  {
    private readonly RequestDelegate _next;

    public RedirectMiddleware(RequestDelegate next)
    {
      _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
      await _next(context);

      // Check if status code is 401 (Unauthorized) or 403 (Forbidden)
      if (context.Response.StatusCode == 401 || context.Response.StatusCode == 403)
      {
        // Redirect to LoginBasic instead of Login
        context.Response.Redirect("/Auth/LoginBasic");
      }
    }
  }
}
