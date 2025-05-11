  using Microsoft.AspNetCore.Mvc;
  using Microsoft.Extensions.Configuration;

namespace AspnetCoreMvcFull.Controllers
{
  [Route("api/[controller]")]
    [ApiController]
    public class ConfigController : ControllerBase
    {
      private readonly IConfiguration _configuration;

      public ConfigController(IConfiguration configuration)
      {
        _configuration = configuration;
      }

    [HttpGet("firebase")]
    public IActionResult GetFirebaseConfig()
    {
      var firebaseConfig = new
      {
        ApiKey = _configuration["Firebase:ApiKey"],
        AuthDomain = _configuration["Firebase:AuthDomain"],
        ProjectId = _configuration["Firebase:ProjectId"],
        StorageBucket = _configuration["Firebase:StorageBucket"],
        MessagingSenderId = _configuration["Firebase:MessagingSenderId"],
        AppId = _configuration["Firebase:AppId"]
      };

      return Ok(firebaseConfig);
    }
  }
  }
