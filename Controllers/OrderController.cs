using AspnetCoreMvcFull.Attributes;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class OrderController : Controller
  {
    private readonly OrderService _orderService;
    private readonly UserService _userService;
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;
    private readonly AffiliateService _affiliateService;
    private readonly CommissionService _commissionService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        OrderService orderService,
        UserService userService,
        CustomerService customerService,
        ProductService productService,
        AffiliateService affiliateService,
        CommissionService commissionService,
        ILogger<OrderController> logger)
    {
      _orderService = orderService;
      _userService = userService;
      _customerService = customerService;
      _productService = productService;
      _affiliateService = affiliateService;
      _commissionService = commissionService;
      _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
      return View();
    }

    public async Task<IActionResult> GetAllOrders()
    {
      try
      {
        var orders = await _orderService.GetAllOrdersAsync();
        return Json(new { success = true, data = orders });
      }
      catch (Exception ex)
      {
        return Json(new { success = false, message = ex.Message });
      }
    }

    [HttpGet]
    [RoleAuthorize("Customer")]
    public async Task<IActionResult> List()
    {
      try
      {
        // Get current user
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        // If not authenticated, redirect to login
        if (string.IsNullOrEmpty(userId))
        {
          return RedirectToAction("Login", "Account");
        }

        // Check user role
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

        List<Order> orders = new List<Order>();

        if (isAdmin)
        {
          // Admins can see all orders
          orders = await _orderService.GetAllAsync();
        }
        else if (isAffiliate)
        {
          // Affiliates can see orders from their referred customers
          // Get customers with referrer = current affiliate's userId
          var customers = await _customerService.GetCustomersByReferrerAsync(userId);
          foreach (var customer in customers)
          {
            // Use UserId instead of CustomerId
            var customerOrders = await _orderService.GetOrdersByUserIdAsync(customer.ReferenceUserId);
            orders.AddRange(customerOrders);
          }
        }
        else
        {
          // Regular users see their own orders
          orders = await _orderService.GetOrdersByUserIdAsync(userId);
        }

        return View(orders);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving order list");
        TempData["ErrorMessage"] = "An error occurred while retrieving orders.";
        return View(new List<Order>());
      }
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
      try
      {
        if (string.IsNullOrEmpty(id))
        {
          return BadRequest("Order ID is required");
        }

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
          return NotFound();
        }

        // Check authorization
        var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        if (!isAdmin && order.UserId != currentUserId)
        {
          var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

          if (isAffiliate)
          {
            // Check if this affiliate is the referrer for the order's user
            var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(currentUserId);
            if (affiliate == null)
            {
              return Forbid();
            }

            // Check if this user was referred by the affiliate
            var user = await _userService.GetByIdAsync(order.UserId);
            if (user == null || user.CreatedBy != currentUserId)
            {
              return Forbid();
            }
          }
          else
          {
            // Regular user can only see their own orders
            return Forbid();
          }
        }

        // Get order details
        var orderDetails = await _orderService.GetOrderDetailsAsync(id);
        ViewBag.OrderDetails = orderDetails;

        // Get commission information if user is an affiliate
        if (User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate"))
        {
          var commissions = await _orderService.GetOrderCommissionsAsync(id);
          ViewBag.Commissions = commissions;
          ViewBag.TotalCommission = commissions.Sum(c => c.Amount);
        }

        return View(order);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error retrieving order details for {id}");
        TempData["ErrorMessage"] = "An error occurred while retrieving order details.";
        return RedirectToAction(nameof(List));
      }
    }

    [HttpGet]
    public async Task<JsonResult> GetOrderDetails(string id)
    {
      try
      {
        if (string.IsNullOrEmpty(id))
        {
          return new JsonResult(new { success = false, message = "Order ID is required" });
        }

        var order = await _orderService.GetOrderByIdAsync(id);
        if (order == null)
        {
          return new JsonResult(new { success = false, message = "Order not found" });
        }

        // Check authorization
        var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

        // Get user info for the order
        var user = await _userService.GetByIdAsync(order.UserId);

        if (!isAdmin && order.UserId != currentUserId)
        {
          if (isAffiliate)
          {
            // Verify this affiliate is the referrer for the user
            if (user == null || user.CreatedBy != currentUserId)
            {
              return new JsonResult(new { success = false, message = "Not authorized" });
            }
          }
          else
          {
            // Regular user can only see their own orders
            return new JsonResult(new { success = false, message = "Not authorized" });
          }
        }

        // Get order details
        var orderDetails = await _orderService.GetOrderDetailsAsync(id);
        var detailsList = new List<object>();

        foreach (var detail in orderDetails)
        {
          // Get product name
          var product = await _productService.GetProductById(detail.ProductId);
          string productName = product?.Name ?? "Unknown Product";

          detailsList.Add(new
          {
            detail.OrderDetailId,
            detail.ProductId,
            ProductName = productName,
            detail.Price,
            detail.Quantity,
            detail.SubTotal
          });
        }

        // Get commissions if user is an affiliate
        var commissionsList = new List<object>();
        double totalCommission = 0;

        if (isAffiliate)
        {
          var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(currentUserId);
          if (affiliate != null)
          {
            var commissions = await _orderService.GetOrderCommissionsAsync(id);
            var affiliateCommissions = commissions.Where(c => c.AffiliateId == affiliate.AffiliateId).ToList();

            foreach (var commission in affiliateCommissions)
            {
              commissionsList.Add(new
              {
                commission.CommissionId,
                commission.ProductId,
                commission.Amount,
                commission.IsPaid
              });

              totalCommission += commission.Amount;
            }
          }
        }

        // Get user name
        string userName = "Unknown";
        if (user != null)
        {
          userName = $"{user.FirstName} {user.LastName}";
        }

        return new JsonResult(new
        {
          success = true,
          order,
          userName,
          orderDetails = detailsList,
          commissions = commissionsList,
          totalCommission
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting order details for {id}");
        return new JsonResult(new { success = false, message = "An error occurred while retrieving order details." });
      }
    }

    [HttpGet]
    [RoleAuthorize("Admin")]
    public async Task<IActionResult> Create()
    {
      try
      {
        // Load available products for order creation
        var products = await _productService.GetAllProducts();
        ViewBag.Products = products;

        // Load available users
        var users = await _userService.GetAllAsync();
        ViewBag.Users = users;

        return View();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading order creation form");
        TempData["ErrorMessage"] = "An error occurred while loading the order form.";
        return RedirectToAction(nameof(List));
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
      try
      {
        if (!ModelState.IsValid)
        {
          // Re-populate the dropdown lists
          var products = await _productService.GetAllProducts();
          ViewBag.Products = products;
          var users = await _userService.GetAllAsync();
          ViewBag.Users = users;

          return View(model);
        }

        // Convert order items to cart items
        var cartItems = new List<CartItem>();
        foreach (var item in model.OrderItems)
        {
          var product = await _productService.GetProductById(item.ProductId);
          if (product != null && item.Quantity > 0)
          {
            // Convert the double price to double explicitly
            cartItems.Add(new CartItem
            {
              ProductId = item.ProductId,
              Name = product.Name,
              ImageUrl = product.Image?.FirstOrDefault() ?? "",
              Price = product.Price,  // Explicit conversion
              Quantity = item.Quantity
            });

          }
        }

        if (cartItems.Count == 0)
        {
          ModelState.AddModelError("", "No valid products were selected for this order.");
          return View(model);
        }

        // Create order
        string orderId = await _orderService.CreateOrderAsync(model.UserId, cartItems);

        // Redirect to order details
        TempData["SuccessMessage"] = "Order created successfully.";
        return RedirectToAction("Details", new { id = orderId });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating order");
        ModelState.AddModelError("", "An error occurred while creating the order. Please try again.");

        // Re-populate the dropdown lists
        var products = await _productService.GetAllProducts();
        ViewBag.Products = products;
        var users = await _userService.GetAllAsync();
        ViewBag.Users = users;

        return View(model);
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(string orderId, string status)
    {
      try
      {
        if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(status))
        {
          return BadRequest("Order ID and status are required");
        }

        // Validate that the user has permission to update this order
        var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        if (string.IsNullOrEmpty(currentUserId) && !isAdmin)
        {
          return Unauthorized();
        }

        await _orderService.UpdateOrderStatusAsync(orderId, status);
        TempData["SuccessMessage"] = "Order status updated successfully.";
        return RedirectToAction("Details", new { id = orderId });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating status for order {orderId}");
        TempData["ErrorMessage"] = "An error occurred while updating the order status.";
        return RedirectToAction("Details", new { id = orderId });
      }
    }

    [HttpGet]
    public async Task<JsonResult> GetOrdersData(int draw = 1, int start = 0, int length = 10, string search = "")
    {
      try
      {
        // Get current user
        var currentUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        // If not authenticated, return error
        if (string.IsNullOrEmpty(currentUserId))
        {
          return new JsonResult(new { error = "Not authenticated" });
        }

        // Check user role
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

        List<Order> allOrders = new List<Order>();

        if (isAdmin)
        {
          // Admins can see all orders
          allOrders = await _orderService.GetAllAsync();
        }
        else if (isAffiliate)
        {
          // Get customers referred by this affiliate
          var customers = await _customerService.GetCustomersByReferrerAsync(currentUserId);
          foreach (var customer in customers)
          {
            var userOrders = await _orderService.GetOrdersByUserIdAsync(customer.ReferenceUserId);
            allOrders.AddRange(userOrders);
          }
        }
        else
        {
          // Regular users see their own orders
          allOrders = await _orderService.GetOrdersByUserIdAsync(currentUserId);
        }

        // Apply search if provided
        var filteredOrders = allOrders;
        if (!string.IsNullOrEmpty(search))
        {
          filteredOrders = allOrders.Where(o =>
              o.OrderId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
              o.Status.Contains(search, StringComparison.OrdinalIgnoreCase)
          ).ToList();
        }

        // Calculate total records
        int totalRecords = allOrders.Count;
        int totalRecordsFiltered = filteredOrders.Count;

        // Paginate the results
        var pagedOrders = filteredOrders.Skip(start).Take(length).ToList();

        // Prepare data for DataTables
        var orderData = new List<object>();
        foreach (var order in pagedOrders)
        {
          // Get user name
          var user = await _userService.GetByIdAsync(order.UserId);
          string userName = "Unknown";
          if (user != null)
          {
            userName = $"{user.FirstName} {user.LastName}";
          }

          // Get order details for amount calculation
          var orderDetails = await _orderService.GetOrderDetailsAsync(order.OrderId);
          double totalAmount = orderDetails.Sum(d => d.SubTotal);

          // Determine payment status based on order status
          string paymentStatus = "Unpaid";
          if (order.Status == "Completed" || order.Status == "Delivered")
          {
            paymentStatus = "Paid";
          }
          else if (order.Status == "Refunded")
          {
            paymentStatus = "Refunded";
          }

          // Convert Firestore timestamp to local date
          DateTime orderDate = order.OrderDate.ToDateTime();

          orderData.Add(new
          {
            id = order.OrderId,
            order_id = order.OrderId,
            date = orderDate.ToString("MMM dd, yyyy"),
            customer = userName,
            payment = paymentStatus,
            status = order.Status,
            method = "Credit Card", // This is a placeholder
            total = totalAmount.ToString("C")
          });
        }

        // Calculate statistics
        var statistics = await GetOrderStatistics();

        return new JsonResult(new
        {
          draw = draw,
          recordsTotal = totalRecords,
          recordsFiltered = totalRecordsFiltered,
          data = orderData,
          statistics
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving order data");
        return new JsonResult(new { error = "An error occurred while retrieving orders." });
      }
    }

    // Replace the existing GetOrderStatistics method with this enhanced version
    private async Task<object> GetOrderStatistics()
    {
      try
      {
        // Get all orders based on user role
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

        List<Order> allOrders = new List<Order>();

        if (isAdmin)
        {
          // Admins see stats for all orders
          allOrders = await _orderService.GetAllAsync();
        }
        else if (isAffiliate)
        {
          // Get customers referred by this affiliate
          var customers = await _customerService.GetCustomersByReferrerAsync(userId);
          foreach (var customer in customers)
          {
            var userOrders = await _orderService.GetOrdersByUserIdAsync(customer.ReferenceUserId);
            allOrders.AddRange(userOrders);
          }
        }
        else
        {
          // Regular users see stats for their own orders
          allOrders = await _orderService.GetOrdersByUserIdAsync(userId);
        }

        // Count orders by status
        int pendingPayment = allOrders.Count(o => o.Status == "Pending");
        int completed = allOrders.Count(o => o.Status == "Completed" || o.Status == "Delivered");
        int refunded = allOrders.Count(o => o.Status == "Refunded");
        int failed = allOrders.Count(o => o.Status == "Failed" || o.Status == "Cancelled");

        // Calculate total sales
        double totalSales = allOrders
            .Where(o => o.Status == "Completed" || o.Status == "Delivered")
            .Sum(o => o.TotalAmount);

        // Monthly sales data (last 6 months)
        var monthlySales = new Dictionary<string, double>();
        var today = DateTime.UtcNow;

        for (int i = 0; i < 6; i++)
        {
          var month = today.AddMonths(-i);
          var monthLabel = month.ToString("MMM yyyy");

          var monthStart = new DateTime(month.Year, month.Month, 1);
          var monthEnd = monthStart.AddMonths(1).AddDays(-1);

          var monthlyRevenue = allOrders
              .Where(o =>
                  (o.Status == "Completed" || o.Status == "Delivered") &&
                  o.OrderDate.ToDateTime() >= monthStart &&
                  o.OrderDate.ToDateTime() <= monthEnd)
              .Sum(o => o.TotalAmount);

          monthlySales.Add(monthLabel, monthlyRevenue);
        }

        // Get top products
        var orderDetails = new List<OrderDetail>();
        foreach (var order in allOrders.Where(o => o.Status == "Completed" || o.Status == "Delivered"))
        {
          var details = await _orderService.GetOrderDetailsAsync(order.OrderId);
          orderDetails.AddRange(details);
        }

        var topProducts = orderDetails
            .GroupBy(d => d.ProductId)
            .Select(g => new {
              ProductId = g.Key,
              QuantitySold = g.Sum(d => d.Quantity),
              Revenue = g.Sum(d => d.SubTotal)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        // Get product names for top products
        var topProductsWithNames = new List<object>();
        foreach (var product in topProducts)
        {
          var productInfo = await _productService.GetProductById(product.ProductId);
          topProductsWithNames.Add(new
          {
            product.ProductId,
            ProductName = productInfo?.Name ?? "Unknown Product",
            product.QuantitySold,
            Revenue = product.Revenue.ToString("C")
          });
        }

        return new
        {
          pendingPayment,
          completed,
          refunded,
          failed,
          totalSales = totalSales.ToString("C"),
          monthlySales,
          topProducts = topProductsWithNames
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving order statistics");
        return new
        {
          pendingPayment = 0,
          completed = 0,
          refunded = 0,
          failed = 0,
          totalSales = "$0.00",
          monthlySales = new Dictionary<string, string>(),
          topProducts = new List<object>()
        };
      }
    }

    [HttpPost]
    public async Task<JsonResult> Delete(string id)
    {
      try
      {
        if (string.IsNullOrEmpty(id))
        {
          return new JsonResult(new { success = false, message = "Order ID is required" });
        }

        // Check if the user has permission to delete this order
        var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        if (!isAdmin)
        {
          // Only admins can delete orders
          return new JsonResult(new { success = false, message = "You don't have permission to delete orders" });
        }

        // Delete the order and its related data
        bool success = await _orderService.DeleteOrderWithRelatedDataAsync(id);

        if (success)
        {
          return new JsonResult(new { success = true });
        }
        else
        {
          return new JsonResult(new { success = false, message = "Failed to delete the order." });
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting order {id}");
        return new JsonResult(new { success = false, message = "An error occurred while deleting the order." });
      }
    }
  }
}

