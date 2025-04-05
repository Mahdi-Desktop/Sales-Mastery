using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AspnetCoreMvcFull.DTO;
public class AffiliateController : Controller
{
  private readonly UserService _userService;
  private readonly AffiliateService _affiliateService;
  private readonly CustomerService _customerService;
  private readonly OrderService _orderService;
  private readonly BrandService _brandService;
  private readonly ILogger<AffiliateController> _logger;
  private readonly IConfiguration _configuration;

  public AffiliateController(
      IConfiguration configuration, // Add this
      UserService userService,
      AffiliateService affiliateService,
      CustomerService customerService,
      OrderService orderService,
      BrandService brandService,
      ILogger<AffiliateController> logger)
  {
    _configuration = configuration; // Assign it here
    _userService = userService;
    _affiliateService = affiliateService;
    _customerService = customerService;
    _orderService = orderService;
    _brandService = brandService;
    _logger = logger;
  }


  public async Task<IActionResult> Dashboard()
  {
    // Get current affiliate user
    string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userId))
      return RedirectToAction("Login", "Account");

    var user = await _userService.GetUserByIdAsync(userId);
    if (user == null || user.Role != "Affiliate")
      return RedirectToAction("AccessDenied", "Account");

    // Get affiliate details
    var affiliate = await _affiliateService.GetAffiliateByUserIdAsync(userId);
    if (affiliate == null)
      return RedirectToAction("AccessDenied", "Account");

    // Get all brands and their commission rates for this affiliate
    var brands = await _brandService.GetAllBrandsAsync();
    ViewBag.Brands = brands;

    // Get all customers referred by this affiliate
    var customers = await _customerService.GetCustomersByReferrerAsync(userId);
    ViewBag.Customers = customers;

    // Get customer data for display
    var customerUsers = new List<User>();
    foreach (var customer in customers)
    {
      var customerUser = await _userService.GetUserByIdAsync(customer.ReferenceUserId);
      if (customerUser != null)
        customerUsers.Add(customerUser);
    }
    ViewBag.CustomerUsers = customerUsers;

    // Get commission history
    var commissions = await GetCommissionsForAffiliate(affiliate.AffiliateId);
    ViewBag.Commissions = commissions;
    ViewBag.TotalCommissions = commissions.Sum(c => c.Amount);
    ViewBag.UnpaidCommissions = commissions.Where(c => !c.IsPaid).Sum(c => c.Amount);

    return View(user);
  }

  private async Task<List<Commission>> GetCommissionsForAffiliate(string affiliateId)
  {
    try
    {
      var db = FirestoreDb.Create(_configuration["Firebase:ProjectId"]);
      var commissionsRef = db.Collection("commissions");
      var query = commissionsRef.WhereEqualTo("AffiliateId", affiliateId);
      var snapshot = await query.GetSnapshotAsync();

      var commissions = new List<Commission>();
      foreach (var doc in snapshot.Documents)
      {
        var commission = doc.ConvertTo<Commission>();
        commission.CommissionId = doc.Id;
        commissions.Add(commission);
      }

      return commissions;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error getting commissions for affiliate {affiliateId}");
      return new List<Commission>();
    }
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> AddCustomer(User user)
  {
    // Security check - only affiliates can access this
    if (!User.IsInRole("Affiliate"))
      return Json(new { success = false, message = "Access denied" });

    string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(currentUserId))
      return Json(new { success = false, message = "Unauthorized" });

    // Force role to be Customer
    user.Role = "Customer";

    // Validate and set defaults
    if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName) ||
        string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Password))
    {
      return Json(new { success = false, message = "Required fields are missing" });
    }

    try
    {
      var existingUser = await _userService.GetUserByEmailAsync(user.Email);
      if (existingUser != null)
      {
        return Json(new { success = false, message = "Email is already in use" });
      }
      // Set created info
      user.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
      user.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
      user.CreatedBy = currentUserId;

      // Add the user
      string userId = await _userService.AddAsync(user);

      // Add customer record linking to this affiliate
      await _customerService.AddCustomerAsync(userId, currentUserId);

      return Json(new { success = true });
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error adding customer");
      return Json(new { success = false, message = ex.Message });
    }
  }

  [HttpGet]
  public async Task<IActionResult> CustomerDetails(string id)
  {
    // Security check - only affiliates can access this
    if (!User.IsInRole("Affiliate"))
      return RedirectToAction("AccessDenied", "Account");

    string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(currentUserId))
      return RedirectToAction("Login", "Account");

    // Get the customer
    var customer = await _customerService.GetCustomerByUserIdAsync(id);
    if (customer == null || customer.ReferenceUserId != currentUserId)
      return NotFound(); // Not found or not this affiliate's customer

    // Get the user details
    var user = await _userService.GetUserByIdAsync(id);
    if (user == null)
      return NotFound();

    // Get customer's orders
    var orders = await GetCustomerOrders(id);
    ViewBag.Orders = orders;

    // Calculate total spent and commissions
    decimal totalSpent = orders.Sum(o => o.Order.TotalAmount);
    decimal totalCommissions = orders.Sum(o => o.Commission);

    ViewBag.TotalSpent = totalSpent;
    ViewBag.TotalCommissions = totalCommissions;

    return View(user);
  }

  private async Task<List<dynamic>> GetCustomerOrders(string customerId)
  {
    try
    {
      var db = FirestoreDb.Create(_configuration["Firebase:ProjectId"]);
      var ordersRef = db.Collection("orders");
      var query = ordersRef.WhereEqualTo("CustomerId", customerId);
      var snapshot = await query.GetSnapshotAsync();

      var result = new List<dynamic>();
      foreach (var doc in snapshot.Documents)
      {
        var order = doc.ConvertTo<Order>();
        order.OrderId = doc.Id;

        // Get order details
        var detailsQuery = db.Collection("orderDetails").WhereEqualTo("OrderId", order.OrderId);
        var detailsSnapshot = await detailsQuery.GetSnapshotAsync();
        var details = new List<OrderDetail>();
        foreach (var detailDoc in detailsSnapshot.Documents)
        {
          var detail = detailDoc.ConvertTo<OrderDetail>();
          detail.OrderDetailId = detailDoc.Id;
          details.Add(detail);
        }

        // Get commissions for this order
        var commissionsQuery = db.Collection("commissions").WhereEqualTo("OrderId", order.OrderId);
        var commissionsSnapshot = await commissionsQuery.GetSnapshotAsync();
        decimal totalCommission = 0;
        if (commissionsSnapshot.Count > 0)
        {
          totalCommission = commissionsSnapshot.Documents
              .Select(d => d.ConvertTo<Commission>())
              .Sum(c => c.Amount);
        }

        result.Add(new
        {
          Order = order,
          Details = details,
          Commission = totalCommission
        });
      }

      return result;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error getting orders for customer {customerId}");
      return new List<dynamic>();
    }
  }
}
