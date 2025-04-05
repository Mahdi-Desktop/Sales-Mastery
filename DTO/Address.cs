using Google.Cloud.Firestore;
using System;


  namespace AspnetCoreMvcFull.DTO
  {
    [FirestoreData]
    public class Address
    {
      [FirestoreDocumentId]
      public string FirestoreId { get; set; }  // Firestore Auto-ID (String)

      [FirestoreProperty]
      public int AddressId { get; set; }  // Manually Assigned Integer ID

      [FirestoreProperty]
      public string Country { get; set; } = "Lebanon";

      [FirestoreProperty]
      public string City { get; set; }

      [FirestoreProperty]
      public string Governorate { get; set; }

      [FirestoreProperty]
      public string Town { get; set; }

      [FirestoreProperty]
      public string Street { get; set; }

      [FirestoreProperty]
      public string Landmark { get; set; }

      [FirestoreProperty]
      public string Building { get; set; }

      [FirestoreProperty]
      public string Floor { get; set; }

      // Relationships (Store Firestore Document IDs as Strings)
      [FirestoreProperty]
      public string UserId { get; set; }  // Firestore Document ID

      [FirestoreProperty]
      public string CustomerId { get; set; }  // Firestore Document ID

      [FirestoreProperty]
      public string CompanyId { get; set; }  // Firestore Document ID

      [FirestoreProperty]
      public string ShipmentId { get; set; }  // Firestore Document ID

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
  }

