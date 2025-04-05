using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Services;
namespace AspnetCoreMvcFull.Controllers;

public class UsersController : Controller
{
  private readonly UserService _userService;
  private readonly InvoiceService _invoiceService;

  public UsersController(UserService userService, InvoiceService invoiceService)
  {
    _userService = userService;
    _invoiceService = invoiceService;
  }
  public IActionResult List() => View();
  //public IActionResult ViewAccount() => View();
  public async Task<IActionResult> ViewAccount(string id)
  {
    var user = await _userService.GetUserByIdAsync(id);
    if (user == null)
    {
      return NotFound();
    }

    // Load invoices for this user
    var invoices = await _invoiceService.GetInvoicesByUserIdAsync(id);
    user.InvoiceId = invoices.Select(i => i.InvoiceId).ToList();
    return View(user);
  }
  public IActionResult ViewBilling() => View();
  public IActionResult ViewConnections() => View();
  public IActionResult ViewNotifications() => View();
  public IActionResult ViewSecurity() => View();
}

/*using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.DTO;
namespace AspnetCoreMvcFull.Controllers
{
  public class UserController : Controller
  {
    private readonly FirebaseService _firebaseService;
    private readonly string CollectionName = "users";

    public UserController(FirebaseService firebaseService)
    {
      _firebaseService = firebaseService;
    }

    // GET: Users
    public async Task<IActionResult> Index()
    {
      var users = await _firebaseService.GetAllAsync<User>(CollectionName);
      return View(users);
    }

    // GET: Users/Details/5
    public async Task<IActionResult> Details(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var user = await _firebaseService.GetByIdAsync<User>(id, CollectionName);

      if (user == null)
      {
        return NotFound();
      }

      return View(user);
    }

    // GET: Users/Create
    public IActionResult Create()
    {
      return View();
    }

    // POST: Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,MiddleName,LastName,Email,PhoneNumber,Role")] User user)
    {
      if (ModelState.IsValid)
      {
        user.CreatedAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;

        await _firebaseService.CreateAsync(user, CollectionName);
        return RedirectToAction(nameof(Index));
      }
      return View(user);
    }

    // GET: Users/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var user = await _firebaseService.GetByIdAsync<User>(id, CollectionName);

      if (user == null)
      {
        return NotFound();
      }

      return View(user);
    }

    // POST: Users/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Id,FirstName,MiddleName,LastName,Email,PhoneNumber,Role,CreatedAt")] User user)
    {
      if (id != user.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        user.UpdatedAt = DateTime.Now;

        await _firebaseService.UpdateAsync(id, user, CollectionName);
        return RedirectToAction(nameof(Index));
      }
      return View(user);
    }

    // GET: Users/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var user = await _firebaseService.GetByIdAsync<User>(id, CollectionName);

      if (user == null)
      {
        return NotFound();
      }

      return View(user);
    }

    // POST: Users/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
      await _firebaseService.DeleteAsync(id, CollectionName);
      return RedirectToAction(nameof(Index));
    }
  }
}
*/
