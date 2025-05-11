using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Controllers.Api
{
  [Route("api/[controller]")]
  [ApiController]
  public class CartController : ControllerBase
  {
    private readonly CartService _cartService;

    public CartController(CartService cartService)
    {
      _cartService = cartService;
    }

    [HttpPost("sync")]
    public IActionResult SyncCart([FromBody] List<CartItem> cartItems)
    {
      // Clear the existing session cart
      _cartService.ClearCart();

      // Add each item from Firebase to the session cart
      foreach (var item in cartItems)
      {
        var product = new Product
        {
          ProductId = item.ProductId,
          Name = item.Name,
          Price = item.Price,
          // Set other properties as needed
        };

        _cartService.AddToCart(product, item.Quantity);
      }

      return Ok(new
      {
        success = true,
        itemCount = _cartService.GetCartItemCount(),
        total = _cartService.GetCartTotal()
      });
    }
  }
}
