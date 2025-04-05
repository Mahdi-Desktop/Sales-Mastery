using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class SubAffiliate
  {
    [FirestoreDocumentId]
    public string SubAffiliateId { get; set; }

    [FirestoreProperty]
    public string AffiliateId { get; set; } // Reference to the parent affiliate

    [FirestoreProperty]
    public string UserId { get; set; } // Reference to the user who is a sub-affiliate

    [FirestoreProperty]
    public decimal CommissionRate { get; set; } // Commission rate for sub-affiliate

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
