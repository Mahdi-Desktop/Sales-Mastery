using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Extensions;

namespace AspnetCoreMvcFull.Controllers
{
  public class ShopController : Controller
  {
    private readonly ProductService _productService;
    private readonly CartService _cartService;
    private readonly OrderService _orderService;
    private readonly InvoiceService _invoiceService;
    private readonly ILogger<ShopController> _logger;
    private readonly CheckoutService _checkoutService;
    private readonly UserService _userService;
    private readonly AddressService _addressService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ShopController(
        ProductService productService,
        CartService cartService,
        OrderService orderService,
        InvoiceService invoiceService,
        CheckoutService checkoutService,
        UserService userService,
        AddressService addressService,
        ILogger<ShopController> logger,
        IHttpContextAccessor httpContextAccessor)
    {
      _productService = productService;
      _cartService = cartService;
      _orderService = orderService;
      _invoiceService = invoiceService;
      _checkoutService = checkoutService;
      _userService = userService;
      _addressService = addressService;
      _logger = logger;
      _httpContextAccessor = httpContextAccessor;
    }

    // Helper method to get current user ID from session
    private string GetCurrentUserId()
    {
      return HttpContext.Session.GetString("UserId");
    }

    public async Task<IActionResult> Index(
    string search = null,
    double? minPrice = null,
    double? maxPrice = null,
    string category = null,
    string sortBy = "name",
    bool ascending = true)
    {
      try
      {
        // Get all products first
        var allProducts = await _productService.GetAllProducts();

        // Apply filters in memory
        var filteredProducts = allProducts;

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
          filteredProducts = filteredProducts
              .Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                         p.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                         p.SKU.Contains(search, StringComparison.OrdinalIgnoreCase))
              .ToList();
        }

        // Apply price filter
        if (minPrice.HasValue)
        {
          filteredProducts = filteredProducts.Where(p => p.Price >= minPrice.Value).ToList();
        }

        if (maxPrice.HasValue)
        {
          filteredProducts = filteredProducts.Where(p => p.Price <= maxPrice.Value).ToList();
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(category))
        {
          filteredProducts = filteredProducts.Where(p => p.CategoryId == category).ToList();
        }

        // Apply sorting
        switch (sortBy.ToLower())
        {
          case "price":
            filteredProducts = ascending
                ? filteredProducts.OrderBy(p => p.Price).ToList()
                : filteredProducts.OrderByDescending(p => p.Price).ToList();
            break;
          case "newest":
            filteredProducts = filteredProducts.OrderByDescending(p => p.CreatedAt).ToList();
            break;
          default: // Default to sorting by name
            filteredProducts = ascending
                ? filteredProducts.OrderBy(p => p.Name).ToList()
                : filteredProducts.OrderByDescending(p => p.Name).ToList();
            break;
        }

        // Get all categories for filter
        var categories = await _productService.GetAllCategories();

        // Get price range
        var minProductPrice = allProducts.Any() ? allProducts.Min(p => p.Price) : 0;
        var maxProductPrice = allProducts.Any() ? allProducts.Max(p => p.Price) : 1000;

        // Prepare view model
        ViewBag.Categories = categories;
        ViewBag.MinPrice = minProductPrice;
        ViewBag.MaxPrice = maxProductPrice;
        ViewBag.SelectedMinPrice = minPrice ?? minProductPrice;
        ViewBag.SelectedMaxPrice = maxPrice ?? maxProductPrice;
        ViewBag.SelectedCategory = category;
        ViewBag.SearchQuery = search;
        ViewBag.SortBy = sortBy;
        ViewBag.SortAscending = ascending;
        ViewBag.CartItemCount = _cartService.GetCartItemCount();

        return View(filteredProducts);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading shop page");
        return View("Error");
      }
    }

    public async Task<IActionResult> ProductDetails(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      try
      {
        var product = await _productService.GetProductById(id);
        if (product == null)
        {
          return NotFound();
        }

        // Get related products (same category)
        var relatedProducts = await _productService.GetProductsByCategory(product.CategoryId);
        // Remove current product from related products
        relatedProducts = relatedProducts.Where(p => p.ProductId != id).Take(4).ToList();

        ViewBag.RelatedProducts = relatedProducts;
        ViewBag.CartItemCount = _cartService.GetCartItemCount();

        return View(product);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error loading product details for ID: {id}");
        return View("Error");
      }
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(string productId, int quantity = 1)
    {
      try
      {
        var product = await _productService.GetProductById(productId);
        if (product == null)
        {
          return Json(new { success = false, message = "Product not found" });
        }

        // Check stock
        if (product.Stock < quantity)
        {
          return Json(new { success = false, message = "Not enough stock available" });
        }

        _cartService.AddToCart(product, quantity);

        return Json(new
        {
          success = true,
          message = $"{product.Name} added to cart",
          cartCount = _cartService.GetCartItemCount()
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error adding product {productId} to cart");
        return Json(new { success = false, message = "An error occurred" });
      }
    }

    public IActionResult Cart()
    {
      var cartItems = _cartService.GetCart();
      ViewBag.CartTotal = _cartService.GetCartTotal();
      ViewBag.CartItemCount = _cartService.GetCartItemCount();

      return View(cartItems);
    }

    [HttpPost]
    public IActionResult UpdateCart(string productId, int quantity)
    {
      try
      {
        _cartService.UpdateItemQuantity(productId, quantity);

        return Json(new
        {
          success = true,
          message = "Cart updated",
          cartCount = _cartService.GetCartItemCount(),
          cartTotal = _cartService.GetCartTotal().ToString("C")
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating cart");
        return Json(new { success = false, message = "An error occurred" });
      }
    }

    [HttpPost]
    public IActionResult RemoveFromCart(string productId)
    {
      try
      {
        _cartService.RemoveFromCart(productId);

        return Json(new
        {
          success = true,
          message = "Item removed from cart",
          cartCount = _cartService.GetCartItemCount(),
          cartTotal = _cartService.GetCartTotal().ToString("C")
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error removing product {productId} from cart");
        return Json(new { success = false, message = "An error occurred" });
      }
    }

    public IActionResult Checkout()
    {
   

      return View();
    }

/*    [HttpGet]
    public IActionResult Checkout()
    {
      // Check if user is logged in
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("Login", "Account", new { returnUrl = "/Shop/Checkout" });
      }

      return View();
    }*/

    [HttpGet]
    public IActionResult OrderConfirmation(string orderId)
    {
      // Check if user is logged in
      var userId = HttpContext.Session.GetString("UserId");
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("Login", "Account");
      }

      ViewBag.OrderId = orderId;
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> ProcessCheckout(string address, string city, string governorate, string phone)
    {
      try
      {
        // Get the current user ID from session
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
          return RedirectToAction("LoginBasic", "Auth", new { returnUrl = Url.Action("Checkout") });
        }

        // Get cart items
        var cartItems = _cartService.GetCart();
        if (!cartItems.Any())
        {
          return RedirectToAction("Cart");
        }

        // Process the order
        string orderId = await _checkoutService.ProcessOrderAsync(userId, cartItems, address, city, governorate, phone);

        // Clear the cart
        _cartService.ClearCart();

        // Redirect to order confirmation
        return RedirectToAction("OrderConfirmation", new { orderId = orderId });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing checkout");
        ModelState.AddModelError(string.Empty, "An error occurred while processing your order. Please try again.");
        return View("Checkout");
      }
    }

/*    public async Task<IActionResult> OrderConfirmation(string orderId)
    {
      if (string.IsNullOrEmpty(orderId))
      {
        return RedirectToAction("Index");
      }

      try
      {
        var order = await _orderService.GetByIdAsync(orderId);
        if (order == null)
        {
          return NotFound();
        }

        // Get order details
        var orderDetails = await _orderService.GetOrderDetailsAsync(orderId);
        ViewBag.OrderDetails = orderDetails;

        // Get customer information
        var user = await _userService.GetByIdAsync(order.UserId);
        if (user != null)
        {
          ViewBag.CustomerName = $"{user.FirstName} {user.LastName}";
          ViewBag.CustomerEmail = user.Email;
        }

        // Get invoice
        var invoices = await _invoiceService.GetInvoicesByUserIdAsync(order.UserId);
        var invoice = invoices.FirstOrDefault();
        if (invoice != null)
        {
          ViewBag.Invoice = invoice;
        }

        return View(order);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error retrieving order confirmation for order ID: {orderId}");
        return View("Error");
      }
    }*/

    public async Task<IActionResult> Invoice(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      try
      {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
          return NotFound();
        }

        // Get customer information
        var user = await _userService.GetByIdAsync(invoice.UserId);
        if (user != null)
        {
          ViewBag.CustomerName = $"{user.FirstName} {user.LastName}";
          ViewBag.CustomerEmail = user.Email;
          ViewBag.CustomerPhone = user.PhoneNumber;
        }

        // Get customer address
        var addresses = await _addressService.GetAddressesByUserIdAsync(invoice.UserId);
        var address = addresses.FirstOrDefault();
        if (address != null)
        {
          ViewBag.Address = address;
        }

        return View(invoice);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error retrieving invoice with ID: {id}");
        return View("Error");
      }
    }

   /* public async Task<IActionResult> OrderHistory()
    {
      // Get the current user ID from session
      var userId = GetCurrentUserId();
      if (string.IsNullOrEmpty(userId))
      {
        return RedirectToAction("LoginBasic", "Auth");
      }

      try
      {
        var orders = await _orderService.GetOrdersByUserIdAsync(userId);
        return View(orders);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving order history");
        return View("Error");
      }
    }*/
    public IActionResult OrderHistory() {
      return View();
    }
  }
}
