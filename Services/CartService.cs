using AspnetCoreMvcFull.DTO;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AspnetCoreMvcFull.Services
{
  public class CartService
  {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _cartSessionKey = "ShoppingCart";

    public CartService(IHttpContextAccessor httpContextAccessor)
    {
      _httpContextAccessor = httpContextAccessor;
    }

    private List<CartItem> GetCartItems()
    {
      var session = _httpContextAccessor.HttpContext.Session;
      string cartJson = session.GetString(_cartSessionKey);

      if (string.IsNullOrEmpty(cartJson))
        return new List<CartItem>();

      return JsonSerializer.Deserialize<List<CartItem>>(cartJson);
    }

    private void SaveCartItems(List<CartItem> items)
    {
      var session = _httpContextAccessor.HttpContext.Session;
      string cartJson = JsonSerializer.Serialize(items);
      session.SetString(_cartSessionKey, cartJson);
    }

    public void AddToCart(Product product, int quantity = 1)
    {
      var cart = GetCartItems();
      var existingItem = cart.FirstOrDefault(item => item.ProductId == product.ProductId);

      if (existingItem != null)
      {
        existingItem.Quantity += quantity;
      }
      else
      {
        cart.Add(new CartItem
        {
          ProductId = product.ProductId,
          Name = product.Name,
          ImageUrl = product.Image?.FirstOrDefault() ?? "/img/products/default.jpg",
          Price = product.Discount.HasValue && (int)product.Discount.Value > 0
        ? (product.Price - (product.Price * product.Discount.Value / 100))
        : product.Price,
          Quantity = quantity
        });
      }

      SaveCartItems(cart);
    }

    public void UpdateQuantity(string productId, int quantity)
    {
      var cart = GetCartItems();
      var item = cart.FirstOrDefault(i => i.ProductId == productId);

      if (item != null)
      {
        if (quantity > 0)
        {
          item.Quantity = quantity;
        }
        else
        {
          cart.Remove(item);
        }

        SaveCartItems(cart);
      }
    }

    // Add this method for compatibility with the controller
    public void UpdateItemQuantity(string productId, int quantity)
    {
      UpdateQuantity(productId, quantity);
    }

    public void RemoveFromCart(string productId)
    {
      var cart = GetCartItems();
      var item = cart.FirstOrDefault(i => i.ProductId == productId);

      if (item != null)
      {
        cart.Remove(item);
        SaveCartItems(cart);
      }
    }

    public void ClearCart()
    {
      _httpContextAccessor.HttpContext.Session.Remove(_cartSessionKey);
    }

    public List<CartItem> GetCart()
    {
      return GetCartItems();
    }

    public int GetCartItemCount()
    {
      var cart = GetCartItems();
      return cart.Sum(item => item.Quantity);
    }

    public double GetCartTotal()
    {
      var cart = GetCartItems();
      return cart.Sum(item => item.SubTotal);
    }
  }
}
