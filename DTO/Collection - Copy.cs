using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Collection
  {
    [FirestoreDocumentId]
    public string CollectionId { get; set; }

    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string CategoryId { get; set; }  // Reference to the related Category document

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
  }
}
