using System;
using System.Collections.Generic;

namespace AspnetCoreMvcFull.DTO
{
  public class OrderStatistics
  {
    public int TotalOrders { get; set; }
    public int PendingPayment { get; set; }
    public int Completed { get; set; }
    public int Refunded { get; set; }
    public int Failed { get; set; }
    public double TotalRevenue { get; set; }
    public Dictionary<string, double> MonthlySales { get; set; } = new Dictionary<string, double>();
    public List<ProductSales> TopProducts { get; set; } = new List<ProductSales>();
  }

  public class ProductSales
  {
    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public int QuantitySold { get; set; }
    public double Revenue { get; set; }
  }
}
