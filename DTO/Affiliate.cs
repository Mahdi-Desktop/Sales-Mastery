using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Affiliate
  {
    [FirestoreDocumentId]
    public string AffiliateId { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; } // Reference to User document

    [FirestoreProperty]
    public string ReferenceUserId { get; set; } // ID of admin who created this affiliate

    [FirestoreProperty]
    public decimal CommissionRate { get; set; } // Default percentage commission (may be overridden by brand rates)

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
