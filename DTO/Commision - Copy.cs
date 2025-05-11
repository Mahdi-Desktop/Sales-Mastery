using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Commission
  {
    [FirestoreDocumentId]
    public string CommissionId { get; set; }

    [FirestoreProperty]
    public string AffiliateId { get; set; }

    [FirestoreProperty]
    public string OrderId { get; set; }

    [FirestoreProperty]
    public string ProductId { get; set; }

    [FirestoreProperty]
    public decimal Amount { get; set; }

    [FirestoreProperty]
    public bool IsPaid { get; set; }

    [FirestoreProperty]
    public Timestamp? PaidDate { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
