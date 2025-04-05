using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Customer
  {
    [FirestoreDocumentId]
    public string CustomerId { get; set; }

    [FirestoreProperty]
    public string FirstName { get; set; }

    [FirestoreProperty]
    public string LastName { get; set; }

    [FirestoreProperty]
    public string Email { get; set; }

    [FirestoreProperty]
    public string PhoneNumber { get; set; }

    [FirestoreProperty]
    public string ReferenceUserId { get; set; } // ID of admin/affiliate who created this customer

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
