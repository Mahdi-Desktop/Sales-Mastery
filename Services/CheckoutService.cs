using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class CheckoutService : FirestoreService<Order>
  {
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(IConfiguration configuration, ILogger<CheckoutService> logger)
        : base(configuration, "orders") // Use "orders" as the collection name
    {
      _logger = logger;
    }

    public async Task<string> ProcessOrderAsync(string userId, List<CartItem> cartItems,
        string address, string city, string governorate, string phone)
    {
      try
      {
        // Calculate order total
        int subtotal = cartItems.Sum(item => item.SubTotal);
        int shippingCost = 400; // Fixed shipping cost
        int total = subtotal + shippingCost;

        // Create order
        var order = new Order
        {
          UserId = userId,
          Status = "Pending",
          TotalAmount = total,
          OrderDate = Timestamp.FromDateTime(DateTime.UtcNow),
          CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
          UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        // Add order to Firestore - using base class method
        string orderId = await AddAsync(order);

        // Create order details for each cart item
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

          // Update product stock
          var productRef = _firestoreDb.Collection("products").Document(item.ProductId);
          batch.Update(productRef, new Dictionary<string, object>
                    {
                        { "Stock", FieldValue.Increment(-item.Quantity) }
                    });
        }

        // Create invoice for the order
        var invoice = new Invoice
        {
          UserId = userId,
          Status = "Pending",
          TotalAmount = total,
          InvoiceDate = Timestamp.FromDateTime(DateTime.UtcNow),
          DueDate = Timestamp.FromDateTime(DateTime.UtcNow.AddDays(7)),
          InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
          Notes = "Cash on delivery",
          CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
          UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
          Items = cartItems.Select(item => new InvoiceItem
          {
            ProductId = item.ProductId,
            ProductName = item.Name,
            Quantity = item.Quantity,
            UnitPrice = item.Price,
            Total = item.SubTotal
          }).ToList()
        };

        var invoiceRef = _firestoreDb.Collection("invoices").Document();
        batch.Set(invoiceRef, invoice);

        // Link invoice to order
        batch.Update(_firestoreDb.Collection("orders").Document(orderId), new Dictionary<string, object>
                {
                    { "InvoiceId", invoiceRef.Id }
                });

        // Create shipping address
        var shippingAddress = new Address
        {
          UserId = userId,
          Street = address,
          City = city,
          Governorate = governorate,
          Country = "Lebanon", // Default country
          CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
        };

        var addressRef = _firestoreDb.Collection("addresses").Document();
        batch.Set(addressRef, shippingAddress);

        // Update user's orders and invoices lists
        var userRef = _firestoreDb.Collection("users").Document(userId);
        batch.Update(userRef, new Dictionary<string, object>
                {
                    { "OrderId", FieldValue.ArrayUnion(orderId) },
                    { "InvoiceId", FieldValue.ArrayUnion(invoiceRef.Id) }
                });

        // Commit all changes
        await batch.CommitAsync();

        return orderId;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error processing order");
        throw;
      }
    }
  }
}
