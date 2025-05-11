using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class OrderDetail
  {
    [FirestoreDocumentId]
    public string OrderDetailId { get; set; }

    [FirestoreProperty]
    public string OrderId { get; set; }  // Reference to Order document

    [FirestoreProperty]
    public string ProductId { get; set; }  // Reference to Product document

    [FirestoreProperty]
    public int Quantity { get; set; }

    [FirestoreProperty]
    public int Price { get; set; }

    [FirestoreProperty]
    public int SubTotal { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
    public string ProductName { get; internal set; }
    public string SKU { get; internal set; }
  }
}
