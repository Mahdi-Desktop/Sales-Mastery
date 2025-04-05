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
    public string CustomerId { get; set; }

    // Reference to Customer document

    [FirestoreProperty]
    public decimal TotalAmount { get; set; }

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
