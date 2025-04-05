using Google.Cloud.Firestore;
using System;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Product
  {
    [FirestoreDocumentId]
    public string ProductId { get; set; }

    [FirestoreProperty]
    public string Name { get; set; }

    [FirestoreProperty]
    public string SKU { get; set; }

    [FirestoreProperty]
    public decimal Price { get; set; }

    [FirestoreProperty]
    public int Stock { get; set; }

    [FirestoreProperty]
    public int? Discount { get; set; }

    [FirestoreProperty]
    public string CategoryId { get; set; }

    [FirestoreProperty]
    public string Description { get; set; }

    [FirestoreProperty]
    public string BrandId { get; set; }

    [FirestoreProperty]
    public List<string> Image { get; set; }

    [FirestoreProperty]
    public int Commission { get; set; } //reference for CommissionRae form brands only used for affilatie

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; }


  }
}
    /*    [FirestoreDocumentId]
        public string ProductId { get; set; }

        [FirestoreProperty]
        public string Name { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public string SKU { get; set; }

        [FirestoreProperty]
        public decimal Price { get; set; }

        [FirestoreProperty]
        public decimal? Discount { get; set; }

        [FirestoreProperty]
        public int Stock { get; set; }

        [FirestoreProperty]
        public string CategoryId { get; set; }  // Reference to Category document

        [FirestoreProperty]
        public string BrandId { get; set; }  // Reference to Brand document

        [FirestoreProperty]
        public string CollectionId { get; set; }  // Reference to Collection document

        [FirestoreProperty]
        public decimal Commission { get; set; }  // Commission rate inherited from the brand

        [FirestoreProperty]
        public int? QuantityOrdered { get; set; }

        [FirestoreProperty]
        public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

        [FirestoreProperty]
        public Timestamp UpdatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

        public object Image { get;  set; }
        public string? BrandName { get;  set; }
      }*/
  
