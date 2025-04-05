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
    // Example Order controller method
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
          // Customers where ReferenceUserId equals the current affiliate's userId
          var customers = await _customerService.GetCustomersByReferrerAsync(userId);
          foreach (var customer in customers)
          {
            var customerOrders = await _orderService.GetOrdersByCustomerIdAsync(customer.CustomerId);
            orders.AddRange(customerOrders);
          }
        }
        else
        {
          // Regular users see their own orders (through their associated customer record)
          var customer = await _customerService.GetCustomerByUserIdAsync(userId);
          if (customer != null)
          {
            orders = await _orderService.GetOrdersByCustomerIdAsync(customer.CustomerId);
          }
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
        var ReferenceUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        if (!isAdmin)
        {
          var customer = await _customerService.GetByIdAsync(order.CustomerId);
          var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

          if (isAffiliate)
          {
            var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(ReferenceUserId);
            if (affiliate == null)
            {
              return Forbid();
            }

            // Check if this customer belongs to the affiliate
            var customersByAffiliate = await _customerService.GetCustomersByReferrerAsync(ReferenceUserId);
            if (!customersByAffiliate.Any(c => c.CustomerId == order.CustomerId))
            {
              return Forbid();
            }
          }
          else
          {
            // Regular user can only see their own orders
            if (customer?.ReferenceUserId != ReferenceUserId)
            {
              return Forbid();
            }
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
        var ReferenceUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        var isAffiliate = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Affiliate");

        // Get customer info
        var customer = await _customerService.GetByIdAsync(order.CustomerId);

        if (!isAdmin)
        {
          if (isAffiliate)
          {
            // Verify this affiliate has access to this customer's orders
            var customersByAffiliate = await _customerService.GetCustomersByReferrerAsync(ReferenceUserId);
            if (!customersByAffiliate.Any(c => c.CustomerId == order.CustomerId))
            {
              return new JsonResult(new { success = false, message = "Not authorized" });
            }
          }
          else
          {
            // Regular user can only see their own orders
            if (customer?.ReferenceUserId != ReferenceUserId)
            {
              return new JsonResult(new { success = false, message = "Not authorized" });
            }
          }
        }

        // Get order details
        var orderDetails = await _orderService.GetOrderDetailsAsync(id);
        var detailsList = new List<object>();

        foreach (var detail in orderDetails)
        {
          // Get product name
          var product = await _productService.GetByIdAsync(detail.ProductId);
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
        decimal totalCommission = 0;

        if (isAffiliate)
        {
          var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(ReferenceUserId);
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

        // Get customer name
        string customerName = "Unknown";
        if (customer != null)
        {
          var user = await _userService.GetByIdAsync(customer.ReferenceUserId);
          if (user != null)
          {
            customerName = $"{user.FirstName} {user.LastName}";
          }
        }

        return new JsonResult(new
        {
          success = true,
          order,
          customerName,
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
    public async Task<IActionResult> Create(string customerId = null)
    {
      try
      {
        // Load available products for order creation
        var products = await _productService.GetAllAsync();
        ViewBag.Products = products;

        // If customerId is provided, pre-select the customer
        if (!string.IsNullOrEmpty(customerId))
        {
          var customer = await _customerService.GetByIdAsync(customerId);
          if (customer != null)
          {
            ViewBag.SelectedCustomerId = customerId;
            var user = await _userService.GetByIdAsync(customer.ReferenceUserId);
            if (user != null)
            {
              ViewBag.SelectedCustomerName = $"{user.FirstName} {user.LastName}";
            }
          }
        }
        else
        {
          // Load available customers for order creation
          var customers = await _customerService.GetAllAsync();
          ViewBag.Customers = customers;

          // For each customer, get the user information
          var customerUsers = new Dictionary<string, string>();
          foreach (var customer in customers)
          {
            var user = await _userService.GetByIdAsync(customer.ReferenceUserId);
            if (user != null)
            {
              customerUsers[customer.CustomerId] = $"{user.FirstName} {user.LastName}";
            }
          }
          ViewBag.CustomerUsers = customerUsers;
        }

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
          var products = await _productService.GetAllAsync();
          ViewBag.Products = products;
          var customers = await _customerService.GetAllAsync();
          ViewBag.Customers = customers;

          return View(model);
        }

        // Create order
        var order = new Order
        {
          CustomerId = model.CustomerId,
          OrderDate = Timestamp.FromDateTime(DateTime.UtcNow),
          Status = "Pending",
          CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
          UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        // Create order details from selected products
        var orderDetails = new List<OrderDetail>();
        foreach (var item in model.OrderItems)
        {
          var product = await _productService.GetByIdAsync(item.ProductId);
          if (product != null && item.Quantity > 0)
          {
            var orderDetail = new OrderDetail
            {
              ProductId = item.ProductId,
              Quantity = item.Quantity,
              Price = product.Price,
              SubTotal = product.Price * item.Quantity,
              CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
            };
            orderDetails.Add(orderDetail);
          }
        }

        if (orderDetails.Count == 0)
        {
          ModelState.AddModelError("", "No valid products were selected for this order.");
          return View(model);
        }

        // Save order
        string orderId = await _orderService.CreateOrderAsync(order, orderDetails);

        // Redirect to order details
        TempData["SuccessMessage"] = "Order created successfully.";
        return RedirectToAction("Details", new { id = orderId });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating order");
        ModelState.AddModelError("", "An error occurred while creating the order. Please try again.");

        // Re-populate the dropdown lists
        var products = await _productService.GetAllAsync();
        ViewBag.Products = products;
        var customers = await _customerService.GetAllAsync();
        ViewBag.Customers = customers;

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
        var ReferenceUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        var isAdmin = User.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");

        if (string.IsNullOrEmpty(ReferenceUserId) && !isAdmin)
        {
          return Unauthorized();
        }

        bool success = await _orderService.UpdateOrderStatusAsync(orderId, status);
        if (success)
        {
          TempData["SuccessMessage"] = "Order status updated successfully.";
          return RedirectToAction("Details", new { id = orderId });
        }
        else
        {
          TempData["ErrorMessage"] = "Failed to update order status.";
          return RedirectToAction("Details", new { id = orderId });
        }
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
        var ReferenceUserId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

        // If not authenticated, return error
        if (string.IsNullOrEmpty(ReferenceUserId))
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
          // Affiliates can see orders from their customers
          var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(ReferenceUserId);
          if (affiliate != null)
          {
            var customers = await _customerService.GetCustomersByReferrerAsync(ReferenceUserId);
            foreach (var customer in customers)
            {
              var customerOrders = await _orderService.GetOrdersByCustomerIdAsync(customer.CustomerId);
              allOrders.AddRange(customerOrders);
            }
          }
        }
        else
        {
          // Regular users see their own orders
          var customer = await _customerService.GetCustomerByUserIdAsync(ReferenceUserId);
          if (customer != null)
          {
            allOrders = await _orderService.GetOrdersByCustomerIdAsync(customer.CustomerId);
          }
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
          // Get customer name
          var customer = await _customerService.GetByIdAsync(order.CustomerId);
          string customerName = "Unknown";
          if (customer != null)
          {
            var user = await _userService.GetByIdAsync(customer.ReferenceUserId);
            if (user != null)
            {
              customerName = $"{user.FirstName} {user.LastName}";
            }
          }

          // Get order details for amount calculation
          var orderDetails = await _orderService.GetOrderDetailsAsync(order.OrderId);
          decimal totalAmount = orderDetails.Sum(d => d.SubTotal);

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
            customer = customerName,
            payment = paymentStatus,
            status = order.Status,
            method = "Credit Card", // You might want to add payment method to your order model
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
          // Affiliates see stats only for their referred customers
          var customers = await _customerService.GetCustomersByReferrerAsync(userId);
          foreach (var customer in customers)
          {
            var customerOrders = await _orderService.GetOrdersByCustomerIdAsync(customer.CustomerId);
            allOrders.AddRange(customerOrders);
          }
        }
        else
        {
          // Regular users see stats for their own orders
          var customer = await _customerService.GetCustomerByUserIdAsync(userId);
          if (customer != null)
          {
            allOrders = await _orderService.GetOrdersByCustomerIdAsync(customer.CustomerId);
          }
        }

        // Count orders by status
        int pendingPayment = allOrders.Count(o => o.Status == "Pending");
        int completed = allOrders.Count(o => o.Status == "Completed" || o.Status == "Delivered");
        int refunded = allOrders.Count(o => o.Status == "Refunded");
        int failed = allOrders.Count(o => o.Status == "Failed" || o.Status == "Cancelled");

        // Calculate total sales
        decimal totalSales = allOrders
            .Where(o => o.Status == "Completed" || o.Status == "Delivered")
            .Sum(o => o.TotalAmount);

        // Monthly sales data (last 6 months)
        var monthlySales = new Dictionary<string, decimal>();
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
          var productInfo = await _productService.GetByIdAsync(product.ProductId);
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
