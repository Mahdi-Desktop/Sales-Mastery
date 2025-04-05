using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class CustomerOrder
  {
    [FirestoreDocumentId]
    public string CustomerOrderId { get; set; }

    [FirestoreProperty]
    public string CustomerId { get; set; }  // Reference to Customer document

    [FirestoreProperty]
    public string OrderId { get; set; }  // Reference to Order document

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
