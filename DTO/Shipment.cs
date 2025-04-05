using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Shipment
  {
    [FirestoreDocumentId]
    public string ShipmentId { get; set; }

    [FirestoreProperty]
    public string OrderId { get; set; }  // Reference to Order document

    [FirestoreProperty]
    public Timestamp ShipmentDate { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public string ShippingAddress { get; set; }

    [FirestoreProperty]
    public string TrackingNumber { get; set; }

    [FirestoreProperty]
    public string Status { get; set; }  // 'Processing', 'Shipped', 'Delivered', etc.

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
