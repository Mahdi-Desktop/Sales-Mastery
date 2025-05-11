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
          if (!string.IsNullOrEmpty(order.UserId))
          {
            var customerRef = _firestoreDb.Collection("customers").Document(order.UserId);
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
        var monthlySales = new Dictionary<string, double>();
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

/*    public async Task<List<Order>> GetOrdersByUserIdAsync(string UserId)
    {
      try
      {
        var orders = new List<Order>();
        var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("UserId", UserId);
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
        _logger.LogError(ex, $"Error getting orders for customer {UserId}");
        return new List<Order>();
      }
    }*/

    public async Task<string> CreateOrderAsync(string userId, List<CartItem> cartItems)
    {
      try
      {
        // Validate the order
        if (string.IsNullOrEmpty(userId) || cartItems == null || !cartItems.Any())
        {
          throw new ArgumentException("Invalid order data");
        }

        // Create order
        var order = new Order
        {
          UserId = userId,
          Status = "Pending",
          TotalAmount = cartItems.Sum(item => item.SubTotal),
          OrderDate = Timestamp.FromDateTime(DateTime.UtcNow),
          CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
          UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        // Save order to Firestore
        string orderId = await AddAsync(order);
        order.OrderId = orderId;

        // Create order details
        var batch = _firestoreDb.StartBatch();
        foreach (var item in cartItems)
        {
          var orderDetail = new OrderDetail
          {
            OrderId = orderId,
            ProductId = item.ProductId,
            ProductName = item.Name,
            Quantity = item.Quantity,
            Price = item.Price,
            SubTotal = item.SubTotal,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
          };

          var detailRef = _firestoreDb.Collection("orderDetails").Document();
          batch.Set(detailRef, orderDetail);
        }

        // Commit all details in one batch
        await batch.CommitAsync();

        // Update user's order list
        var userRef = _firestoreDb.Collection("users").Document(userId);
        var userSnapshot = await userRef.GetSnapshotAsync();
        if (userSnapshot.Exists)
        {
          // Use FieldValue.ArrayUnion to add the order ID to the user's orders list
          await userRef.UpdateAsync("OrderId", FieldValue.ArrayUnion(orderId));
        }

        // Process any affiliate commissions
        await ProcessCommissions(order, cartItems);

        return orderId;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating order");
        throw;
      }
    }
    private async Task ProcessCommissions(Order order, List<CartItem> cartItems)
    {
      try
      {
        // Get the customer
        var userRef = _firestoreDb.Collection("users").Document(order.UserId);
        var userSnapshot = await userRef.GetSnapshotAsync();

        if (!userSnapshot.Exists)
          return;

        var user = userSnapshot.ConvertTo<User>();

        // Check if user was referred by an affiliate
        if (string.IsNullOrEmpty(user.CreatedBy))
          return;

        // Process commission for each product
        var batch = _firestoreDb.StartBatch();

        foreach (var item in cartItems)
        {
          // Get product info
          var productRef = _firestoreDb.Collection("products").Document(item.ProductId);
          var productSnapshot = await productRef.GetSnapshotAsync();

          if (!productSnapshot.Exists)
            continue;

          var product = productSnapshot.ConvertTo<Product>();

          // Calculate commission based on product's commission rate
          int commissionRate = product.Commission;
          int commissionAmount = (item.SubTotal * commissionRate) / 100;


          if (commissionAmount <= 0)
            continue;

          // Create commission record
          var commission = new Commission
          {
            AffiliateId = user.CreatedBy,
            OrderId = order.OrderId,
            ProductId = item.ProductId,
            Amount = commissionAmount,
            IsPaid = false,
            CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
          };

          // Add to batch
          var commissionRef = _firestoreDb.Collection("commissions").Document();
          batch.Set(commissionRef, commission);
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



    public async Task UpdateOrderStatusAsync(string orderId, string status)
    {
      try
      {
        var order = await GetByIdAsync(orderId);
        if (order != null)
        {
          order.Status = status;
          await UpdateAsync(orderId, order);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating status for order {orderId}");
        throw;
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

    public async Task<List<Order>> GetOrdersByUserIdAsync(string userId)
    {
      try
      {
        var query = _collection.WhereEqualTo("UserId", userId);
        var snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents
            .Select(doc => doc.ConvertTo<Order>())
            .OrderByDescending(o => o.OrderDate)
            .ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting orders for user {userId}");
        throw;
      }
    }
  }
}
