using AspnetCoreMvcFull.DTO;
using System.Collections.Generic;
using System.Linq;

namespace AspnetCoreMvcFull.Extensions
{
  public static class ModelExtensions
  {
    public static bool HasDiscount(this Product product)
    {
      return product.Discount.HasValue && product.Discount.Value > 0;
    }

    // For lines 14-24 - likely in the DiscountPrice method
    public static decimal DiscountPrice(this Product product)
    {
      int price = product.Price;
      if (!product.HasDiscount())
        return price;

      var discountValue = product.Discount ?? 0;
      var discountAmount = (price * discountValue) / 100;
      return price - discountAmount;
    }



    public static int DiscountPercentage(this Product product)
    {
      return product.Discount.GetValueOrDefault();
    }

    // For lines 32-35 - likely in the CurrentPrice method
    public static decimal CurrentPrice(this Product product)
    {
      // Add explicit cast from double to decimal
      return product.HasDiscount() ? product.DiscountPrice() : product.Price;
    }
    // For display formatting
    public static string FormattedPrice(this Product product)
    {
      return $"${product.Price / 100.0:F2}";
    }
    public static int StockQuantity(this Product product)
    {
      return product.Stock;
    }

    public static string ImageUrl(this Product product)
    {
      return product.Image?.FirstOrDefault() ?? "/img/products/default.jpg";
    }

    public static IEnumerable<string> Categories(this Product product)
    {
      // This is a placeholder, replace with actual category retrieval logic
      // In a real implementation, you would fetch categories by product.CategoryId
      yield return product.CategoryId;
    }

    public static IEnumerable<string> Tags(this Product product)
    {
      // This is a placeholder, in a real implementation you would have actual tags
      yield break;
    }

    // Order Extensions
    public static decimal SubTotal(this Order order)
    {
      return order.TotalAmount - 10; // Assuming $10 delivery fee is included in TotalAmount
    }

    public static decimal Tax(this Order order)
    {
      return 0; // No tax in current implementation
    }

    public static decimal ShippingCost(this Order order)
    {
      return 10; // Fixed $10 shipping
    }

    public static decimal Total(this Order order)
    {
      return order.TotalAmount;
    }

    public static string InvoiceId(this Order order)
    {
      // In a real implementation, you would fetch this from the order's document data
      // This is a placeholder
      return null;
    }

    // Invoice Extensions
    public static decimal Amount(this Invoice invoice)
    {
      return invoice.TotalAmount;
    }

    public static Google.Cloud.Firestore.Timestamp IssueDate(this Invoice invoice)
    {
      return invoice.InvoiceDate;
    }

    public static string OrderId(this Invoice invoice)
    {
      // In a real implementation, you would fetch this from the invoice's document data
      // This is a placeholder
      return null;
    }

    public static Google.Cloud.Firestore.Timestamp PaymentDate(this Invoice invoice)
    {
      // In a real implementation, you would have a separate payment date
      // For now, use the invoice date as a placeholder
      return invoice.InvoiceDate;
    }
  }
}
