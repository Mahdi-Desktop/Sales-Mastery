using Google.Cloud.Firestore;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class User
  {
    [FirestoreDocumentId]
    public string? UserId { get; set; }

    [FirestoreProperty]
    public required string FirstName { get; set; }

    [FirestoreProperty]
    public string? MiddleName { get; set; }

    [FirestoreProperty]
    public required string LastName { get; set; }

    //FirstName MiddleName LastName Email Password PhoneNumber Role CreatedBy CreatedAt UpdatedAt

    [FirestoreProperty]
    public required string Email { get; set; }

    [FirestoreProperty]
    public  required string Password { get; set; }

    [FirestoreProperty]
    public  required string PhoneNumber { get; set; }

    [FirestoreProperty]
    public required string Role { get; set; } // 'Admin', 'Affiliate', 'Customer'

    [FirestoreProperty]
    public string? CreatedBy { get; set; } // ID of admin/affiliate who created this user

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; }

    // Relationships
    [FirestoreProperty]
    public List<string> CustomerId { get; set; } = new List<string>();

    [FirestoreProperty]
    public List<string> OrderId { get; set; } = new List<string>();

    [FirestoreProperty]
    public List<string> InvoiceId { get; set; } = new List<string>();

    public string DisplayName => $"{FirstName} {LastName}";
  }
}
