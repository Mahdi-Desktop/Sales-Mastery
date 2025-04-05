/*using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class CustomersController : Controller
  {
    private readonly FirebaseService _firebaseService;
    private readonly string CollectionName = "customers";

    public CustomersController(FirebaseService firebaseService)
    {
      _firebaseService = firebaseService;
    }

    // GET: Customers
    public async Task<IActionResult> Index()
    {
      var customers = await _firebaseService.GetAllAsync<Customer>(CollectionName);
      return View(customers);
    }

    // GET: Customers/Details/5
    public async Task<IActionResult> Details(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var customer = await _firebaseService.GetByIdAsync<Customer>(id, CollectionName);

      if (customer == null)
      {
        return NotFound();
      }

      return View(customer);
    }

    // GET: Customers/Create
    public async Task<IActionResult> Create()
    {
      var users = await _firebaseService.GetAllAsync<User>("users");
      ViewBag.Users = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(users, "Id", "Email");
      return View();
    }

    // POST: Customers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,UserId")] Customer customer)
    {
      if (ModelState.IsValid)
      {
        customer.CreatedAt = DateTime.Now;
        customer.UpdatedAt = DateTime.Now;

        await _firebaseService.CreateAsync(customer, CollectionName);
        return RedirectToAction(nameof(Index));
      }

      var users = await _firebaseService.GetAllAsync<User>("users");
      ViewBag.Users = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(users, "Id", "Email", customer.UserId);
      return View(customer);
    }

    // GET: Customers/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var customer = await _firebaseService.GetByIdAsync<Customer>(id, CollectionName);

      if (customer == null)
      {
        return NotFound();
      }

      var users = await _firebaseService.GetAllAsync<User>("users");
      ViewBag.Users = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(users, "Id", "Email", customer.UserId);
      return View(customer);
    }

    // POST: Customers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Id,FirstName,LastName,Email,PhoneNumber,UserId,CreatedAt")] Customer customer)
    {
      if (id != customer.Id)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        customer.UpdatedAt = DateTime.Now;

        await _firebaseService.UpdateAsync(id, customer, CollectionName);
        return RedirectToAction(nameof(Index));
      }

      var users = await _firebaseService.GetAllAsync<User>("users");
      ViewBag.Users = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(users, "Id", "Email", customer.UserId);
      return View(customer);
    }

    // GET: Customers/Delete/5
    public async Task<IActionResult> Delete(string id)
    {
      if (id == null)
      {
        return NotFound();
      }

      var customer = await _firebaseService.GetByIdAsync<Customer>(id, CollectionName);

      if (customer == null)
      {
        return NotFound();
      }

      return View(customer);
    }

    // POST: Customers/Delete/5
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
