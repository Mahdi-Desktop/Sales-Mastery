using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class DashboardService : FirestoreService<object>
  {
    private readonly ILogger<DashboardService> _logger;
    // Change from private to public to allow access from the controller
    public readonly OrderService OrderService;
    public readonly ProductService ProductService;
    public readonly AffiliateService AffiliateService;
    public readonly CustomerService CustomerService;
    public readonly InvoiceService InvoiceService;
    public readonly UserService UserService;

    public DashboardService(
        IConfiguration configuration,
        ILogger<DashboardService> logger,
        OrderService orderService,
        ProductService productService,
        AffiliateService affiliateService,
        CustomerService customerService,
        InvoiceService invoiceService,
        UserService userService)
        : base(configuration, "dashboard_analytics") // Collection for storing analytics data if needed
    {
      _logger = logger;
      OrderService = orderService;
      ProductService = productService;
      AffiliateService = affiliateService;
      CustomerService = customerService;
      InvoiceService = invoiceService;
      UserService = userService;
    }

    #region Order Analytics

    public async Task<List<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Convert DateTime to Firestore Timestamp
        var startTimestamp = Timestamp.FromDateTime(startDate.ToUniversalTime());
        var endTimestamp = Timestamp.FromDateTime(endDate.ToUniversalTime());

        // Get all orders
        var orders = await OrderService.GetAllOrdersAsync();

        // Filter by date range
        return orders
            .Where(o => o.OrderDate >= startTimestamp &&
                       o.OrderDate <= endTimestamp)
            .ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting orders for date range: {startDate} to {endDate}");
        return new List<Order>();
      }
    }

    public async Task<object> GetRevenueChartDataAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        var orders = await GetOrdersByDateRangeAsync(startDate, endDate);

        // Group by day for the chart
        var dailyData = orders
             .GroupBy(o => o.OrderDate.ToDateTime().Date)
             .OrderBy(g => g.Key)
             .Select(g => new {
               Date = g.Key,
               TotalRevenue = g.Sum(o => o.TotalAmount),
               AffiliateEarnings = 0, // Initialize with integer
               NetProfit = g.Sum(o => o.TotalAmount) // All integers
             })
             .ToList();

        // Process each day to add commission data
        for (int i = 0; i < dailyData.Count; i++)
        {
          var day = dailyData[i];
          var dayOrders = orders.Where(o => o.OrderDate.ToDateTime().Date == day.Date).ToList();

          int dayCommissions = 0;
          foreach (var order in dayOrders)
          {
            var orderCommissions = await OrderService.GetOrderCommissionsAsync(order.OrderId);
            dayCommissions += orderCommissions.Sum(c => c.Amount);
          }

          // Update with integers only
          dailyData[i] = new
          {
            Date = day.Date,
            TotalRevenue = day.TotalRevenue,
            AffiliateEarnings = dayCommissions, // Integer
            NetProfit = day.TotalRevenue - dayCommissions // Integer
          };
        }

        // Format data for the chart
        return new
        {
          dates = dailyData.Select(d => d.Date.ToString("yyyy-MM-dd")).ToArray(),
          totalRevenue = dailyData.Select(d => d.TotalRevenue).ToArray(),
          affiliateEarnings = dailyData.Select(d => d.AffiliateEarnings).ToArray(),
          netProfit = dailyData.Select(d => d.NetProfit).ToArray()
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error generating revenue chart data");
        return new
        {
          dates = new string[0],
          totalRevenue = new double[0],
          affiliateEarnings = new double[0],
          netProfit = new double[0]
        };
      }
    }

    #endregion

    #region Product Analytics

    public async Task<List<double>> GetProductProfitMarginsAsync(List<Order> orders)
    {
      try
      {
        var margins = new List<double>();
        var productIds = new HashSet<string>();

        // Gather all product IDs from orders
        foreach (var order in orders)
        {
          var details = await OrderService.GetOrderDetailsAsync(order.OrderId);
          foreach (var detail in details)
          {
            productIds.Add(detail.ProductId);
          }
        }

        // Calculate margins for each product
        foreach (var productId in productIds)
        {
          var product = await ProductService.GetProductById(productId);
          if (product != null)
          {
            // Use product.Price as double directly
            double price = product.Price;
            double costPrice = price * 0.7; // Estimate cost as 70% of price
            double margin = (price - costPrice) / price * 100;
            margins.Add(Math.Round(margin, 2));
          }
        }

        return margins;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error calculating product profit margins");
        return new List<double>();
      }
    }

    public async Task<List<object>> GetTopProductsAsync(DateTime startDate, DateTime endDate, int limit = 5)
    {
      try
      {
        var orders = await GetOrdersByDateRangeAsync(startDate, endDate);
        var productStats = new Dictionary<string, ProductStats>();

        // Get order details for all orders in range
        foreach (var order in orders)
        {
          var details = await OrderService.GetOrderDetailsAsync(order.OrderId);
          foreach (var detail in details)
          {
            if (!productStats.ContainsKey(detail.ProductId))
            {
              var product = await ProductService.GetProductById(detail.ProductId);
              productStats[detail.ProductId] = new ProductStats
              {
                ProductId = detail.ProductId,
                Name = product?.Name ?? detail.ProductName,
                Image = product?.Image?.FirstOrDefault()
              };
            }

            productStats[detail.ProductId].Quantity += detail.Quantity;
            productStats[detail.ProductId].Revenue += detail.SubTotal;
          }
        }

        // Get previous period for growth calculation
        var daysDifference = (endDate - startDate).Days;
        var previousStartDate = startDate.AddDays(-daysDifference);
        var previousEndDate = startDate.AddDays(-1);
        var previousOrders = await GetOrdersByDateRangeAsync(previousStartDate, previousEndDate);
        var previousStats = new Dictionary<string, double>();

        // Calculate previous period revenue
        foreach (var order in previousOrders)
        {
          var details = await OrderService.GetOrderDetailsAsync(order.OrderId);
          foreach (var detail in details)
          {
            if (!previousStats.ContainsKey(detail.ProductId))
            {
              previousStats[detail.ProductId] = 0;
            }
            previousStats[detail.ProductId] += detail.SubTotal;
          }
        }

        // Calculate growth percentages
        foreach (var product in productStats.Values)
        {
          if (previousStats.TryGetValue(product.ProductId, out var previousRevenue) && previousRevenue > 0)
          {
            product.Growth = Math.Round((product.Revenue - previousRevenue) / previousRevenue * 100, 2);
          }
          else
          {
            product.Growth = 100; // 100% growth if no previous sales
          }
        }

        // Sort by revenue and take top products
        return productStats.Values
            .OrderByDescending(p => p.Revenue)
            .Take(limit)
            .Cast<object>()
            .ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting top products");
        return new List<object>();
      }
    }

    public async Task<object> GetProfitMarginChartDataAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        var orders = await GetOrdersByDateRangeAsync(startDate, endDate);

        // Group by week for the chart
        var weeks = new List<DateTime>();
        for (var date = startDate; date <= endDate; date = date.AddDays(7))
        {
          weeks.Add(date);
        }

        var margins = new List<double>();
        foreach (var weekStart in weeks)
        {
          var weekEnd = weekStart.AddDays(6) > endDate ? endDate : weekStart.AddDays(6);
          var weekOrders = orders.Where(o =>
              o.OrderDate.ToDateTime() >= weekStart &&
              o.OrderDate.ToDateTime() <= weekEnd).ToList();

          // Calculate average margin for the week
          var weekMargins = await GetProductProfitMarginsAsync(weekOrders);
          margins.Add(weekMargins.Any() ? weekMargins.Average() : 0);
        }

        return new
        {
          categories = weeks.Select(w => w.ToString("MMM dd")).ToArray(),
          margins = margins.Select(m => Math.Round(m, 2)).ToArray()
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error generating profit margin chart data");
        return new
        {
          categories = new string[0],
          margins = new double[0]
        };
      }
    }

    #endregion

    #region Affiliate Analytics

    public async Task<int> GetActiveAffiliatesCountAsync(DateTime endDate)
    {
      try
      {
        // Get all affiliates
        var affiliates = await AffiliateService.GetAllAffiliatesAsync();

        // For active affiliates, we'll count those with at least one referred customer
        // or one commission in the last 30 days
        var startDate = endDate.AddDays(-30);

        // Get orders in the last 30 days
        var recentOrders = await GetOrdersByDateRangeAsync(startDate, endDate);

        // Get all commissions from these orders
        var commissionsByAffiliate = new HashSet<string>();
        foreach (var order in recentOrders)
        {
          var orderCommissions = await OrderService.GetOrderCommissionsAsync(order.OrderId);
          foreach (var commission in orderCommissions)
          {
            commissionsByAffiliate.Add(commission.AffiliateId);
          }
        }

        // Count "active" affiliates
        return commissionsByAffiliate.Count;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting active affiliates count");
        return 0;
      }
    }

    // Fix for the first error (around line 339-436)
    public async Task<List<object>> GetTopAffiliatesAsync(DateTime startDate, DateTime endDate, int limit = 5)
    {
      try
      {
        // Get all orders in date range
        var orders = await GetOrdersByDateRangeAsync(startDate, endDate);

        // Calculate commission by affiliate
        var affiliateStats = new Dictionary<string, AffiliateStats>();

        foreach (var order in orders)
        {
          var orderCommissions = await OrderService.GetOrderCommissionsAsync(order.OrderId);

          foreach (var commission in orderCommissions)
          {
            if (!affiliateStats.ContainsKey(commission.AffiliateId))
            {
              var affiliate = await AffiliateService.GetAffiliateByIdAsync(commission.AffiliateId);
              var user = affiliate != null ?
                  await UserService.GetUserByIdAsync(affiliate.UserId) : null;

              affiliateStats[commission.AffiliateId] = new AffiliateStats
              {
                AffiliateId = commission.AffiliateId,
                Name = user != null ? $"{user.FirstName} {user.LastName}" : "Unknown",
                Commission = 0,
                Customers = 0,
                Growth = 0
              };
            }

            affiliateStats[commission.AffiliateId].Commission += commission.Amount;
          }
        }

        // Get customer counts
        foreach (var affiliateId in affiliateStats.Keys.ToList())
        {
          // Get customers referred by this affiliate
          var customers = await AffiliateService.GetCustomersByAffiliateIdAsync(affiliateId);
          affiliateStats[affiliateId].Customers = customers.Count;
        }

        // Calculate growth compared to previous period
        var daysDifference = (endDate - startDate).Days;
        var previousStartDate = startDate.AddDays(-daysDifference);
        var previousEndDate = startDate.AddDays(-1);

        var previousOrders = await GetOrdersByDateRangeAsync(previousStartDate, previousEndDate);
        var previousStats = new Dictionary<string, double>();

        // Fix for the second and third errors (around line 396-401)
        // Calculate previous period commissions
        foreach (var order in previousOrders)
        {
          var prevCommissions = await OrderService.GetOrderCommissionsAsync(order.OrderId);
          foreach (var comm in prevCommissions)
          {
            if (!previousStats.ContainsKey(comm.AffiliateId))
            {
              previousStats[comm.AffiliateId] = 0;
            }
            previousStats[comm.AffiliateId] += comm.Amount;
          }
        }

        // Calculate growth percentages
        foreach (var affiliate in affiliateStats.Values)
        {
          if (previousStats.TryGetValue(affiliate.AffiliateId, out var previousCommission) && previousCommission > 0)
          {
            affiliate.Growth = Math.Round((affiliate.Commission - previousCommission) / previousCommission * 100, 2);
          }
          else
          {
            affiliate.Growth = 100; // 100% growth if no previous commissions
          }
        }

        // Return top affiliates by commission amount
        return affiliateStats.Values
            .OrderByDescending(a => a.Commission)
            .Take(limit)
            .Cast<object>()
            .ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting top affiliates");
        return new List<object>();
      }
    }

    public async Task<object> GetCommissionStatsAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Get all orders in date range
        var orders = await GetOrdersByDateRangeAsync(startDate, endDate);

        double totalCommissions = 0;
        double paidCommissions = 0;
        double pendingCommissions = 0;

        foreach (var order in orders)
        {
          var orderCommissions = await OrderService.GetOrderCommissionsAsync(order.OrderId);
          foreach (var commission in orderCommissions)
          {
            totalCommissions += commission.Amount;

            if (commission.IsPaid)
            {
              paidCommissions += commission.Amount;
            }
            else
            {
              pendingCommissions += commission.Amount;
            }
          }
        }

        // Calculate next payout date (example: 1st of next month)
        var today = DateTime.UtcNow;
        var nextPayoutDate = new DateTime(today.Year, today.Month, 1).AddMonths(1);

        return new
        {
          total = totalCommissions,
          paid = paidCommissions,
          pending = pendingCommissions,
          nextPayoutDate = nextPayoutDate.ToString("yyyy-MM-dd")
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting commission stats");
        return new
        {
          total = 0m,
          paid = 0m,
          pending = 0m,
          nextPayoutDate = DateTime.UtcNow.AddMonths(1).ToString("yyyy-MM-dd")
        };
      }
    }

    // Renamed to avoid ambiguity with Customer Growth Chart
    public async Task<int[]> GetAffiliateGrowthChartDataAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Calculate daily growth for the past 7 days (for sparkline chart)
        var result = new int[7];
        for (int i = 0; i < 7; i++)
        {
          var day = endDate.AddDays(-i);
          var prevDay = day.AddDays(-1);

          var dayCount = await GetActiveAffiliatesCountAsync(day);
          var prevDayCount = await GetActiveAffiliatesCountAsync(prevDay);

          if (prevDayCount > 0)
          {
            result[6 - i] = (int)Math.Round((dayCount - prevDayCount) / (double)prevDayCount * 100);
          }
          else
          {
            result[6 - i] = 0;
          }
        }

        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting affiliate growth chart data");
        return new int[7];
      }
    }

    public async Task<object> GetCommissionTrendDataAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Get data for past 6 months
        var months = new List<DateTime>();
        var currentMonth = new DateTime(endDate.Year, endDate.Month, 1);

        for (int i = 0; i < 6; i++)
        {
          months.Add(currentMonth.AddMonths(-i));
        }

        months.Reverse(); // Chronological order

        var totalCommissions = new List<double>();
        var paidCommissions = new List<double>();

        foreach (var month in months)
        {
          var monthEnd = month.AddMonths(1).AddDays(-1);

          // Get orders for the month
          var monthOrders = await GetOrdersByDateRangeAsync(month, monthEnd);

          int monthTotal = 0;
          int monthPaid = 0;

          foreach (var order in monthOrders)
          {
            var monthOrderCommissions = await OrderService.GetOrderCommissionsAsync(order.OrderId);
            foreach (var commission in monthOrderCommissions)
            {
              monthTotal += commission.Amount;

              if (commission.IsPaid)
              {
                monthPaid += commission.Amount;
              }
            }
          }

          totalCommissions.Add(monthTotal);
          paidCommissions.Add(monthPaid);
        }

        return new
        {
          months = months.Select(m => m.ToString("MMM yyyy")).ToArray(),
          total = totalCommissions.ToArray(),
          paid = paidCommissions.ToArray()
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting commission trend data");
        return new
        {
          months = new string[0],
          total = new double[0],
          paid = new double[0]
        };
      }
    }

    #endregion

    #region Customer Analytics

    public async Task<List<Customer>> GetCustomersByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Convert DateTime to Firestore Timestamp
        var startTimestamp = Timestamp.FromDateTime(startDate.ToUniversalTime());
        var endTimestamp = Timestamp.FromDateTime(endDate.ToUniversalTime());

        // Get all customers
        var customersCollection = _firestoreDb.Collection("customers");
        var query = customersCollection
            .WhereGreaterThanOrEqualTo("CreatedAt", startTimestamp)
            .WhereLessThanOrEqualTo("CreatedAt", endTimestamp);

        var snapshot = await query.GetSnapshotAsync();

        var customers = new List<Customer>();
        foreach (var doc in snapshot.Documents)
        {
          var customer = doc.ConvertTo<Customer>();
          customer.CustomerId = doc.Id;
          customers.Add(customer);
        }

        return customers;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting customers for date range: {startDate} to {endDate}");
        return new List<Customer>();
      }
    }

    public async Task<int> GetVisitorsCountAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // In a real implementation, this would come from analytics data
        // For this example, we'll estimate visitors as 5x the number of customers
        var customers = await GetCustomersByDateRangeAsync(startDate, endDate);
        return customers.Count * 5;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting visitors count");
        return 0;
      }
    }

    public async Task<object> GetAcquisitionChannelsAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // In a real implementation, this would come from analytics data
        // For this example, we'll simulate channel distribution

        // Get customers created in the period
        var customers = await GetCustomersByDateRangeAsync(startDate, endDate);

        // Count referrals - using the CreatedBy field instead of AffiliateId
        var referrals = customers.Count(c => !string.IsNullOrEmpty(c.ReferenceUserId));

        // Simulate other channels (in a real system, this would come from tracking data)
        int totalCustomers = customers.Count;
        int organic = totalCustomers > 0 ? (int)Math.Ceiling(totalCustomers * 0.35) : 0;
        int social = totalCustomers > 0 ? (int)Math.Ceiling(totalCustomers * 0.25) : 0;
        int email = totalCustomers > 0 ? (int)Math.Ceiling(totalCustomers * 0.15) : 0;

        // Ensure we don't exceed the total
        if (referrals + organic + social + email > totalCustomers)
        {
          // Adjust to make sure they sum to the total
          int excess = (referrals + organic + social + email) - totalCustomers;
          if (organic > excess) organic -= excess;
          else if (social > excess) social -= excess;
          else if (email > excess) email -= excess;
        }

        // Calculate percentages
        int totalChannelSum = referrals + organic + social + email;

        double affiliatePercent = totalChannelSum > 0 ? Math.Round((double)referrals / totalChannelSum * 100, 1) : 0;
        double organicPercent = totalChannelSum > 0 ? Math.Round((double)organic / totalChannelSum * 100, 1) : 0;
        double socialPercent = totalChannelSum > 0 ? Math.Round((double)social / totalChannelSum * 100, 1) : 0;
        double emailPercent = totalChannelSum > 0 ? Math.Round((double)email / totalChannelSum * 100, 1) : 0;

        return new
        {
          affiliate = affiliatePercent,
          organic = organicPercent,
          social = socialPercent,
          email = emailPercent
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting acquisition channels");
        return new
        {
          affiliate = 25.0m,
          organic = 35.0m,
          social = 25.0m,
          email = 15.0m
        };
      }
    }

    // Renamed to avoid ambiguity with Affiliate Growth Chart
    public async Task<int[]> GetCustomerGrowthChartDataAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Calculate daily growth for the past 7 days (for sparkline chart)
        var result = new int[7];
        for (int i = 0; i < 7; i++)
        {
          var day = endDate.AddDays(-i);
          var prevDay = day.AddDays(-1);

          var dayCustomers = await GetCustomersByDateRangeAsync(day.Date, day.Date.AddDays(1).AddSeconds(-1));
          var prevDayCustomers = await GetCustomersByDateRangeAsync(prevDay.Date, prevDay.Date.AddDays(1).AddSeconds(-1));

          if (prevDayCustomers.Count > 0)
          {
            result[6 - i] = (int)Math.Round((dayCustomers.Count - prevDayCustomers.Count) / (double)prevDayCustomers.Count * 100);
          }
          else if (dayCustomers.Count > 0)
          {
            result[6 - i] = 100; // 100% growth if there were no customers the previous day
          }
          else
          {
            result[6 - i] = 0;
          }
        }

        return result;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting customer growth chart data");
        return new int[7];
      }
    }

    #endregion

    #region Invoice Analytics

    public async Task<object> GetInvoiceStatsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Convert DateTime to Firestore Timestamp
        var startTimestamp = Timestamp.FromDateTime(startDate.ToUniversalTime());
        var endTimestamp = Timestamp.FromDateTime(endDate.ToUniversalTime());

        // Get all invoices
        var invoicesRef = _firestoreDb.Collection("invoices");
        var query = invoicesRef
            .WhereGreaterThanOrEqualTo("CreatedAt", startTimestamp)
            .WhereLessThanOrEqualTo("CreatedAt", endTimestamp);

        var snapshot = await query.GetSnapshotAsync();

        int paidCount = 0;
        int pendingCount = 0;
        int overdueCount = 0;
        double paidAmount = 0;
        double pendingAmount = 0;
        double overdueAmount = 0;
        List<int> paymentDays = new List<int>();

        foreach (var doc in snapshot.Documents)
        {
          var invoice = doc.ConvertTo<Invoice>();

          switch (invoice.Status?.ToLower())
          {
            case "paid":
              paidCount++;
              paidAmount += invoice.TotalAmount;

              // Calculate payment days if available
              // Instead of using PaidAt (which doesn't exist), we'll use UpdatedAt as an estimate
              if (invoice.UpdatedAt != null && invoice.CreatedAt != null)
              {
                var createdDate = invoice.CreatedAt.ToDateTime();
                var updatedDate = invoice.UpdatedAt.ToDateTime(); // Use UpdatedAt as a proxy for payment date
                paymentDays.Add((updatedDate - createdDate).Days);
              }
              break;

            case "pending":
              pendingCount++;
              pendingAmount += invoice.TotalAmount;
              break;

            case "overdue":
              overdueCount++;
              overdueAmount += invoice.TotalAmount;
              break;
          }
        }

        // Calculate average payment days
        // Calculate average payment days
        double avgPaymentDays = paymentDays.Count > 0 ? paymentDays.Average() : 0;

        return new
        {
          paid = new { count = paidCount, amount = paidAmount },
          pending = new { count = pendingCount, amount = pendingAmount },
          overdue = new { count = overdueCount, amount = overdueAmount },
          avgPaymentDays = Math.Round(avgPaymentDays, 1)
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting invoice statistics");
        return new
        {
          paid = new { count = 0, amount = 0m },
          pending = new { count = 0, amount = 0m },
          overdue = new { count = 0, amount = 0m },
          avgPaymentDays = 0.0
        };
      }
    }

    #endregion
  }

  #region Helper Classes

  #endregion
}


