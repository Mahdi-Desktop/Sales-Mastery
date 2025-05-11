using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;

namespace AspnetCoreMvcFull.Filters
{
  public class FirebaseConfigFilter : IActionFilter
  {
    private readonly IConfiguration _configuration;

    public FirebaseConfigFilter(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
      // Do nothing before the action executes
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
      if (context.Controller is Controller controller)
      {
        controller.ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
        controller.ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
        controller.ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
        controller.ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
        controller.ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];
      }
    }
  }
}
