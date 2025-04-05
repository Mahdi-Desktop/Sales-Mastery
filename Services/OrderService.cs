using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class OrderService : FirestoreService<Order>
  {
    private readonly ILogger<OrderService> _logger;
    private const string CollectionName = "orders";

    public OrderService(IConfiguration configuration, ILogger<OrderService> logger)
        : base(configuration, CollectionName)
    {
      _logger = logger;
    }
    // Add this method to the OrderService class
    public async Task<List<Order>> GetAllOrdersAsync(int limit = 0, string lastDocumentId = null)
    {
      try
      {
        var orders = await GetAllAsync();

        // Enrich orders with additional information
        foreach (var order in orders)
        {
          // Get order details for each order
          var details = await GetOrderDetailsAsync(order.OrderId);

          // Calculate additional properties if needed
          order.ItemCount = details.Count;

          // Fetch customer information if needed (optional)
          if (!string.IsNullOrEmpty(order.CustomerId))
          {
            var customerRef = _firestoreDb.Collection("customers").Document(order.CustomerId);
            var customerSnapshot = await customerRef.GetSnapshotAsync();
            if (customerSnapshot.Exists)
            {
              var customer = customerSnapshot.ConvertTo<Customer>();
              //var Odetails = customerSnapshot.ConvertTo<OrderDetail>();
              order.CustomerName = $"{customer.FirstName} {customer.LastName}";
              order.CustomerEmail = customer.Email;
            }
          }
        }

        // Optionally apply sorting here
        orders = orders.OrderByDescending(o => o.OrderDate).ToList();

        return orders;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all orders");
        return new List<Order>();
      }
    }

    public async Task<OrderStatistics> GetOrderStatisticsAsync()
    {
      try
      {
        var statistics = new OrderStatistics();

        // Get all orders
        var orders = await GetAllAsync();

        // Count by status
        statistics.TotalOrders = orders.Count;
        statistics.PendingPayment = orders.Count(o => o.Status == "Pending");
        statistics.Completed = orders.Count(o => o.Status == "Completed" || o.Status == "Delivered");
        statistics.Refunded = orders.Count(o => o.Status == "Refunded");
        statistics.Failed = orders.Count(o => o.Status == "Failed" || o.Status == "Cancelled");

        // Calculate total revenue
        statistics.TotalRevenue = orders
            .Where(o => o.Status == "Completed" || o.Status == "Delivered")
            .Sum(o => o.TotalAmount);

        // Calculate monthly revenue (last 6 months)
        var monthlySales = new Dictionary<string, decimal>();
        var today = DateTime.UtcNow;

        for (int i = 0; i < 6; i++)
        {
          var month = today.AddMonths(-i);
          var monthLabel = month.ToString("MMM yyyy");

          var monthStart = new DateTime(month.Year, month.Month, 1);
          var monthEnd = monthStart.AddMonths(1).AddDays(-1);

          var monthlyRevenue = orders
              .Where(o =>
                  (o.Status == "Completed" || o.Status == "Delivered") &&
                  o.OrderDate.ToDateTime() >= monthStart &&
                  o.OrderDate.ToDateTime() <= monthEnd)
              .Sum(o => o.TotalAmount);

          monthlySales.Add(monthLabel, monthlyRevenue);
        }

        statistics.MonthlySales = monthlySales;

        // Most sold products
        var orderDetails = new List<OrderDetail>();
        foreach (var order in orders.Where(o => o.Status == "Completed" || o.Status == "Delivered"))
        {
          var details = await GetOrderDetailsAsync(order.OrderId);
          orderDetails.AddRange(details);
        }

        var productSales = orderDetails
            .GroupBy(d => d.ProductId)
            .Select(g => new ProductSales
            {
              ProductId = g.Key,
              QuantitySold = g.Sum(d => d.Quantity),
              Revenue = g.Sum(d => d.SubTotal)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(5)
            .ToList();

        statistics.TopProducts = productSales;

        return statistics;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting order statistics");
        return new OrderStatistics();
      }
    }

    public async Task<Order> GetOrderByIdAsync(string orderId)
    {
      try
      {
        var order = await GetByIdAsync(orderId);
        return order;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting order with ID {orderId}");
        throw;
      }
    }

    public async Task<List<OrderDetail>> GetOrderDetailsAsync(string orderId)
    {
      try
      {
        var details = new List<OrderDetail>();
        var query = _firestoreDb.Collection("orderDetails").WhereEqualTo("OrderId", orderId);
        var snapshot = await query.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
          var detail = document.ConvertTo<OrderDetail>();
          detail.OrderDetailId = document.Id;
          details.Add(detail);
        }

        return details;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting details for order {orderId}");
        return new List<OrderDetail>();
      }
    }

    public async Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId)
    {
      try
      {
        var orders = new List<Order>();
        var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("CustomerId", customerId);
        var snapshot = await query.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
          var order = document.ConvertTo<Order>();
          order.OrderId = document.Id;
          orders.Add(order);
        }

        return orders;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting orders for customer {customerId}");
        return new List<Order>();
      }
    }

    public async Task<string> CreateOrderAsync(Order order, List<OrderDetail> orderDetails)
    {
      try
      {
        // Validate the order
        if (order == null || string.IsNullOrEmpty(order.CustomerId) || orderDetails == null || !orderDetails.Any())
        {
          throw new ArgumentException("Invalid order data");
        }

        // Set timestamps
        order.OrderDate = Timestamp.FromDateTime(DateTime.UtcNow);
        order.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        order.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        // Default status if not set
        if (string.IsNullOrEmpty(order.Status))
        {
          order.Status = "Pending";
        }

        // Add order to get ID
        string orderId = await AddAsync(order);

        // Calculate order total and process order details
        decimal totalAmount = 0;
        var batch = _firestoreDb.StartBatch();

        foreach (var detail in orderDetails)
        {
          // Set order ID reference
          detail.OrderId = orderId;

          // Calculate subtotal
          detail.SubTotal = detail.Price * detail.Quantity;
          totalAmount += detail.SubTotal;

          // Set timestamp
          detail.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

          // Add to batch
          var detailRef = _firestoreDb.Collection("orderDetails").Document();
          detail.OrderDetailId = detailRef.Id;
          batch.Set(detailRef, detail);
        }

        // Update order with total amount
        order.OrderId = orderId;
        order.TotalAmount = totalAmount;
        await UpdateAsync(orderId, order);

        // Commit all details in one batch
        await batch.CommitAsync();

        // Process any affiliate commissions (could be implemented later)
        await ProcessCommissions(order, orderDetails);

        return orderId;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating order");
        throw;
      }
    }

    private async Task ProcessCommissions(Order order, List<OrderDetail> orderDetails)
    {
      try
      {
        // This would handle calculating and recording affiliate commissions
        // Simplified example:

        // 1. Check if customer was referred by an affiliate
        var customerRef = _firestoreDb.Collection("customers").Document(order.CustomerId);
        var customerSnapshot = await customerRef.GetSnapshotAsync();

        if (!customerSnapshot.Exists)
        {
          return;
        }

        var customer = customerSnapshot.ConvertTo<Customer>();

        if (string.IsNullOrEmpty(customer.ReferenceUserId))
        {
          return; // No affiliate reference
        }

        // 2. Get the affiliate record
        var affiliateQuery = _firestoreDb.Collection("affiliates")
            .WhereEqualTo("UserId", customer.ReferenceUserId);
        var affiliateSnapshot = await affiliateQuery.GetSnapshotAsync();

        if (affiliateSnapshot.Count == 0)
        {
          return; // No affiliate found
        }

        var affiliate = affiliateSnapshot.Documents[0].ConvertTo<Affiliate>();
        affiliate.AffiliateId = affiliateSnapshot.Documents[0].Id;

        // 3. Calculate and record commissions for each product
        var batch = _firestoreDb.StartBatch();

        foreach (var detail in orderDetails)
        {
          // Get product to check brand commission rate
          var productRef = _firestoreDb.Collection("products").Document(detail.ProductId);
          var productSnapshot = await productRef.GetSnapshotAsync();

          if (!productSnapshot.Exists)
          {
            continue;
          }

          var product = productSnapshot.ConvertTo<Product>();

          // Get brand for commission rate
          var brandRef = _firestoreDb.Collection("brands").Document(product.BrandId);
          var brandSnapshot = await brandRef.GetSnapshotAsync();

          decimal commissionRate = affiliate.CommissionRate; // Default to affiliate rate

          if (brandSnapshot.Exists)
          {
            var brand = brandSnapshot.ConvertTo<Brand>();
            commissionRate = brand.CommissionRate; // Use brand-specific rate if available
          }

          // Calculate commission
          decimal commissionAmount = detail.SubTotal * (commissionRate / 100m);

          if (commissionAmount > 0)
          {
            // Create commission record
            var commission = new Commission
            {
              AffiliateId = affiliate.AffiliateId,
              OrderId = order.OrderId,
              ProductId = detail.ProductId,
              Amount = commissionAmount,
              IsPaid = false,
              CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
            };

            // Add to batch
            var commissionRef = _firestoreDb.Collection("commissions").Document();
            batch.Set(commissionRef, commission);
          }
        }

        // Commit all commission records
        await batch.CommitAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error processing commissions for order {order.OrderId}");
        // Don't throw - we want the order to succeed even if commission processing fails
      }
    }

    public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
    {
      try
      {
        var order = await GetByIdAsync(orderId);
        if (order == null)
        {
          return false;
        }

        order.Status = status;
        order.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        await UpdateAsync(orderId, order);
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating status for order {orderId}");
        return false;
      }
    }

    public async Task<List<Commission>> GetOrderCommissionsAsync(string orderId)
    {
      try
      {
        var commissions = new List<Commission>();
        var query = _firestoreDb.Collection("commissions").WhereEqualTo("OrderId", orderId);
        var snapshot = await query.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
          var commission = document.ConvertTo<Commission>();
          commission.CommissionId = document.Id;
          commissions.Add(commission);
        }

        return commissions;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting commissions for order {orderId}");
        return new List<Commission>();
      }
    }
    public async Task<bool> DeleteOrderWithRelatedDataAsync(string orderId)
    {
      try
      {
        // First delete related order details
        var orderDetails = await GetOrderDetailsAsync(orderId);
        foreach (var detail in orderDetails)
        {
          await _firestoreDb.Collection("orderDetails").Document(detail.OrderDetailId).DeleteAsync();
        }

        // Then delete any commissions related to this order
        var query = _firestoreDb.Collection("commissions").WhereEqualTo("OrderId", orderId);
        var snapshot = await query.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
          await _firestoreDb.Collection("commissions").Document(document.Id).DeleteAsync();
        }

        // Finally delete the order itself
        await DeleteAsync(orderId);

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting order {orderId} with related data");
        return false;
      }
    }

  }
}
