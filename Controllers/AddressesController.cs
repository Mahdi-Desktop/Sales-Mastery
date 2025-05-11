using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;

namespace AspnetCoreMvcFull.Controllers
{
  public class AddressesController : Controller
  {
    private readonly AddressService _addressService;

    public AddressesController(AddressService addressService)
    {
      _addressService = addressService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAddress(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      var address = await _addressService.GetAddressByIdAsync(id);
      if (address == null)
      {
        return NotFound();
      }

      return Json(address);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAddress([Bind("UserId,Country,City,Governorate,Town,Street,Building,Floor,Landmark")] Address address)
    {
      if (ModelState.IsValid)
      {
        try
        {
          // Set creation timestamp
          address.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

          // Add address
          await _addressService.AddAddressAsync(address);

          return Json(new { success = true });
        }
        catch (Exception ex)
        {
          return Json(new { success = false, message = ex.Message });
        }
      }

      return Json(new { success = false, message = "Invalid address data" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateAddress([Bind("AddressId,UserId,Country,City,Governorate,Town,Street,Building,Floor,Landmark")] Address address)
    {
      if (string.IsNullOrEmpty(address.AddressId))
      {
        return Json(new { success = false, message = "Address ID is required" });
      }

      if (ModelState.IsValid)
      {
        try
        {
          // Update address
          await _addressService.UpdateAddressAsync(address);

          return Json(new { success = true });
        }
        catch (Exception ex)
        {
          return Json(new { success = false, message = ex.Message });
        }
      }

      return Json(new { success = false, message = "Invalid address data" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAddress(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return Json(new { success = false, message = "Address ID is required" });
      }

      try
      {
        // Delete address
        await _addressService.DeleteAddressAsync(id);

        return Json(new { success = true });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }
  }
}
