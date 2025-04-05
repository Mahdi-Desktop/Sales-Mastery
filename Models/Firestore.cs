using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.Models
{
  public class Firestore
  {
    [FirestoreData]
    public class User
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string FirstName { get; set; }

      [FirestoreProperty]
      public string MiddleName { get; set; }

      [FirestoreProperty]
      public string LastName { get; set; }

      [FirestoreProperty]
      public string Email { get; set; }

      [FirestoreProperty]
      public string PhoneNumber { get; set; }

      [FirestoreProperty]
      public string Role { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Address
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string Country { get; set; } = "Lebanon";

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

      [FirestoreProperty]
      public string UserId { get; set; }

      [FirestoreProperty]
      public string CustomerId { get; set; }

      [FirestoreProperty]
      public string CompanyId { get; set; }

      [FirestoreProperty]
      public string ShipmentId { get; set; }
    }

    [FirestoreData]
    public class Category
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string Name { get; set; }

      [FirestoreProperty]
      public string Description { get; set; }
    }

    [FirestoreData]
    public class Brand
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string Name { get; set; }

      [FirestoreProperty]
      public string Description { get; set; }
    }

    [FirestoreData]
    public class Collection
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string Name { get; set; }

      [FirestoreProperty]
      public string Description { get; set; }
    }

    [FirestoreData]
    public class Product
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string Sku { get; set; }

      [FirestoreProperty]
      public string Name { get; set; }

      [FirestoreProperty]
      public decimal Price { get; set; }

      [FirestoreProperty]
      public decimal? Discount { get; set; }

      [FirestoreProperty]
      public int? QuantityInStock { get; set; }

      [FirestoreProperty]
      public string CategoryId { get; set; }

      [FirestoreProperty]
      public string BrandId { get; set; }

      [FirestoreProperty]
      public string CollectionId { get; set; }

      [FirestoreProperty]
      public decimal? Commission { get; set; }

      [FirestoreProperty]
      public int? QuantityOrdered { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Customer
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string FirstName { get; set; }

      [FirestoreProperty]
      public string LastName { get; set; }

      [FirestoreProperty]
      public string Email { get; set; }

      [FirestoreProperty]
      public string PhoneNumber { get; set; }

      [FirestoreProperty]
      public string UserId { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Order
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string CustomerId { get; set; }

      [FirestoreProperty]
      public decimal TotalAmount { get; set; }

      [FirestoreProperty]
      public string Status { get; set; }

      [FirestoreProperty]
      public DateTime OrderDate { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class OrderDetail
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string OrderId { get; set; }

      [FirestoreProperty]
      public string ProductId { get; set; }

      [FirestoreProperty]
      public int Quantity { get; set; }

      [FirestoreProperty]
      public decimal Price { get; set; }

      [FirestoreProperty]
      public decimal SubTotal { get; set; }
    }

    [FirestoreData]
    public class Invoice
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string OrderId { get; set; }

      [FirestoreProperty]
      public DateTime InvoiceDate { get; set; }

      [FirestoreProperty]
      public decimal TotalAmount { get; set; }

      [FirestoreProperty]
      public string Status { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Shipment
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string OrderId { get; set; }

      [FirestoreProperty]
      public DateTime ShipmentDate { get; set; }

      [FirestoreProperty]
      public string ShippingAddress { get; set; }

      [FirestoreProperty]
      public string TrackingNumber { get; set; }

      [FirestoreProperty]
      public string Status { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class Affiliate
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string UserId { get; set; }

      [FirestoreProperty]
      public decimal CommissionRate { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    [FirestoreData]
    public class SubAffiliate
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string AffiliateId { get; set; }

      [FirestoreProperty]
      public string UserId { get; set; }

      [FirestoreProperty]
      public decimal CommissionRate { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }

    // For the many-to-many relationships
    [FirestoreData]
    public class BrandCollectionMapping
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string BrandId { get; set; }

      [FirestoreProperty]
      public string CollectionId { get; set; }
    }

    [FirestoreData]
    public class BrandCategoryMapping
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string BrandId { get; set; }

      [FirestoreProperty]
      public string CategoryId { get; set; }
    }

    [FirestoreData]
    public class CustomerOrder
    {
      [FirestoreProperty]
      public string Id { get; set; }

      [FirestoreProperty]
      public string CustomerId { get; set; }

      [FirestoreProperty]
      public string OrderId { get; set; }

      [FirestoreProperty]
      public DateTime CreatedAt { get; set; }

      [FirestoreProperty]
      public DateTime UpdatedAt { get; set; }
    }
  }
}

