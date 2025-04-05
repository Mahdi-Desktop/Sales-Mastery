using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  [Route("api/[controller]")]
  [ApiController]
  public class Test2Controller : ControllerBase
  {
    private readonly FirestoreDb _db;
    private readonly UserService _userService;
    private readonly InvoiceService _invoiceService;

    // Constructor receives FirestoreDb via dependency injection
    public Test2Controller(FirestoreDb db, UserService userService, InvoiceService invoiceService)
    {
      _db = db ?? throw new ArgumentNullException(nameof(db));
      _userService = userService;
      _invoiceService = invoiceService;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
      Console.WriteLine("Firebase Test Controller is working");
      return Ok("Firebase Test Controller is working");
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
      try
      {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
      }
      catch (System.Exception ex)
      {
        return StatusCode(500, $"Error: {ex.Message}");
      }
    }

/*    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices()
    {
      try
      {
        var invoices = await _invoiceService.GetAllInvoicesAsync();
        return Ok(invoices);
      }
      catch (System.Exception ex)
      {
        return StatusCode(500, $"Error: {ex.Message}");
      }
    }*/
  }
}
