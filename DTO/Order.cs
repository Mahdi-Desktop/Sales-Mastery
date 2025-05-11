/*using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Order
  {
    [FirestoreDocumentId]
    public string OrderId { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; }

    [FirestoreProperty]
    public string Status { get; set; } // "Pending", "Processing", "Shipped", "Delivered", "Cancelled"

    [FirestoreProperty]
    public decimal SubTotal { get; set; }

    [FirestoreProperty]
    public decimal Tax { get; set; }

    [FirestoreProperty]
    public decimal ShippingCost { get; set; }

    [FirestoreProperty]
    public decimal Total { get; set; }

    [FirestoreProperty]
    public string PaymentMethod { get; set; }

    [FirestoreProperty]
    public bool IsPaid { get; set; }

    [FirestoreProperty]
    public Timestamp OrderDate { get; set; }

    [FirestoreProperty]
    public ShippingAddress ShippingAddress { get; set; }

    [FirestoreProperty]
    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    [FirestoreProperty]
    public string InvoiceId { get; set; }

    [FirestoreProperty]
    public string TrackingNumber { get; set; }
  }

  [FirestoreData]
  public class OrderItem
  {
    [FirestoreProperty]
    public string ProductId { get; set; }

    [FirestoreProperty]
    public string ProductName { get; set; }

    [FirestoreProperty]
    public decimal Price { get; set; }

    [FirestoreProperty]
    public int Quantity { get; set; }

    [FirestoreProperty]
    public decimal SubTotal { get; set; }
  }

  [FirestoreData]
  public class ShippingAddress
  {
    [FirestoreProperty]
    public string FirstName { get; set; }

    [FirestoreProperty]
    public string LastName { get; set; }

    [FirestoreProperty]
    public string Email { get; set; }

    [FirestoreProperty]
    public string Phone { get; set; }

    [FirestoreProperty]
    public string Address { get; set; }

    [FirestoreProperty]
    public string City { get; set; }

    [FirestoreProperty]
    public string State { get; set; }

    [FirestoreProperty]
    public string ZipCode { get; set; }
  }
}
*/

using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Order
  {
    [FirestoreDocumentId]
    public string OrderId { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; }

    // Reference to Customer document

    [FirestoreProperty]
    public int TotalAmount { get; set; }

    [FirestoreProperty]
    public string Status { get; set; }  // 'Pending', 'Processing', 'Shipped', 'Delivered', etc.

    [FirestoreProperty]
    public Timestamp OrderDate { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    public int ItemCount { get; internal set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
  }
}

