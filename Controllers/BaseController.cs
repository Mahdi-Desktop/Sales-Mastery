using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace AspnetCoreMvcFull.Controllers
{
  public class BaseController : Controller
  {
    protected readonly IConfiguration _configuration;

    public BaseController(IConfiguration configuration)
    {
      _configuration = configuration;
    }

    public override ViewResult View()
    {
      SetFirebaseConfig();
      return base.View();
    }

    public override ViewResult View(string viewName)
    {
      SetFirebaseConfig();
      return base.View(viewName);
    }

    public override ViewResult View(object model)
    {
      SetFirebaseConfig();
      return base.View(model);
    }

    public override ViewResult View(string viewName, object model)
    {
      SetFirebaseConfig();
      return base.View(viewName, model);
    }

    private void SetFirebaseConfig()
    {
      ViewBag.FirebaseApiKey = _configuration["Firebase:ApiKey"];
      ViewBag.FirebaseProjectId = _configuration["Firebase:ProjectId"];
      ViewBag.FirebaseStorageBucket = _configuration["Firebase:StorageBucket"];
      ViewBag.FirebaseMessagingSenderId = _configuration["Firebase:MessagingSenderId"];
      ViewBag.FirebaseAppId = _configuration["Firebase:AppId"];
    }
  }
}
