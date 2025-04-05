using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class BrandCollectionMapping
  {
    [FirestoreDocumentId]
    public string Id { get; set; }

    [FirestoreProperty]
    public string BrandId { get; set; }  // Reference to Brand document

    [FirestoreProperty]
    public string CollectionId { get; set; }  // Reference to Collection document

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
