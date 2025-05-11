using Google.Cloud.Firestore;
using Mono.TextTemplating;
using System;
using System.Reflection.Emit;


  namespace AspnetCoreMvcFull.DTO
  {
    [FirestoreData]
    public class Address
    {
      [FirestoreDocumentId]
      public string AddressId { get; set; }  // Firestore Auto-ID (String)
    [FirestoreProperty]
    public string AddressType { get; set; } // e.g., "Home", "Work", "Billing", "Shipping"

    [FirestoreProperty]
    public string Line1 { get; set; }

    [FirestoreProperty]
    public string Line2 { get; set; }


    [FirestoreProperty]
      public string Country { get; set; } = "Lebanon";
    [FirestoreProperty]
    public string State { get; set; }
    [FirestoreProperty]
    public string ZipCode { get; set; } = "0000";

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
      public Timestamp CreatedAt { get; set; } 
    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; }

    [FirestoreProperty]
    public bool IsPrimary { get; set; }




    // Formatted address for display
    public string FormattedAddress
    {
      get
      {
        var address = Line1;
        if (!string.IsNullOrEmpty(Line2))
          address += ", " + Line2;

        address += ", " + City;

        if (!string.IsNullOrEmpty(State))
          address += ", " + State;

        address += " " + ZipCode;

        if (!string.IsNullOrEmpty(Country))
          address += ", " + Country;

        return address;
      }
    }
  }
  }

