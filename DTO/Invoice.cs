using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.DTO
{
  [FirestoreData]
  public class Invoice
  {
    [FirestoreDocumentId]
    public string InvoiceId { get; set; }

    [FirestoreProperty]
    public string UserId { get; set; }

    [FirestoreProperty]
    public string Status { get; set; } // "Paid", "Pending", "Overdue", etc.

    [FirestoreProperty]
    public decimal TotalAmount { get; set; }

    [FirestoreProperty]
    public Timestamp InvoiceDate { get; set; }

    [FirestoreProperty]
    public Timestamp DueDate { get; set; }

    [FirestoreProperty]
    public string InvoiceNumber { get; set; }

    [FirestoreProperty]
    public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    [FirestoreProperty]
    public string Notes { get; set; }

    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }

    [FirestoreProperty]
    public Timestamp UpdatedAt { get; set; }
  }

  [FirestoreData]
  public class InvoiceItem
  {
    [FirestoreProperty]
    public string ProductId { get; set; }

    [FirestoreProperty]
    public string ProductName { get; set; }

    [FirestoreProperty]
    public string ProductSKU { get; set; }

    [FirestoreProperty]
    public string Description { get; set; }

    [FirestoreProperty]
    public decimal Quantity { get; set; }

    [FirestoreProperty]
    public decimal UnitPrice { get; set; }

    [FirestoreProperty]
    public decimal Discount { get; set; }

    [FirestoreProperty]
    public decimal Tax { get; set; }

    [FirestoreProperty]
    public decimal Total { get; set; }
  }
}
