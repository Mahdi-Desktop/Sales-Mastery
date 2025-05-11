using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Controllers.Api
{
  [Route("api/dashboard")]
  [ApiController]
  public class DashboardController : ControllerBase
  {
    private readonly DashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        DashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
      _dashboardService = dashboardService;
      _logger = logger;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(DateTime startDate, DateTime endDate)
    {
      try
      {
        // Get orders within date range
        var orders = await _dashboardService.GetOrdersByDateRangeAsync(startDate, endDate);

        // Get previous period for comparison
        var daysDifference = (endDate - startDate).Days;
        var previousPeriodStart = startDate.AddDays(-daysDifference - 1);
        var previousPeriodEnd = startDate.AddDays(-1);
        var previousOrders = await _dashboardService.GetOrdersByDateRangeAsync(previousPeriodStart, previousPeriodEnd);

        // Calculate metrics
        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var prevTotalRevenue = previousOrders.Sum(o => o.TotalAmount);
        var revenueGrowth = prevTotalRevenue > 0 ? ((totalRevenue - prevTotalRevenue) / prevTotalRevenue * 100) : 100;

        // For the commission amount, we need to calculate it if it's not stored directly
        decimal affiliateEarnings = 0;
        decimal prevAffiliateEarnings = 0;

        foreach (var order in orders)
        {
          var orderCommissions = await _dashboardService.OrderService.GetOrderCommissionsAsync(order.OrderId);
          affiliateEarnings += orderCommissions.Sum(c => c.Amount);
        }

        foreach (var order in previousOrders)
        {
          var prevOrderCommissions = await _dashboardService.OrderService.GetOrderCommissionsAsync(order.OrderId);
          prevAffiliateEarnings += prevOrderCommissions.Sum(c => c.Amount);
        }

        var affiliateEarningsGrowth = prevAffiliateEarnings > 0 ? ((affiliateEarnings - prevAffiliateEarnings) / prevAffiliateEarnings * 100) : 100;

        var netProfit = (totalRevenue - affiliateEarnings);
        var prevNetProfit = (prevTotalRevenue - prevAffiliateEarnings);
        var netProfitGrowth = prevNetProfit > 0 ? ((netProfit - prevNetProfit) / prevNetProfit * 100) : 100;

        // Get affiliates data
        var activeAffiliates = await _dashboardService.GetActiveAffiliatesCountAsync(endDate);
        var prevActiveAffiliates = await _dashboardService.GetActiveAffiliatesCountAsync(previousPeriodEnd);
        var affiliateGrowth = prevActiveAffiliates > 0 ? ((activeAffiliates - prevActiveAffiliates) / (float)prevActiveAffiliates * 100) : 100;

        // Get customers data
        var customers = await _dashboardService.GetCustomersByDateRangeAsync(startDate, endDate);
        var prevCustomers = await _dashboardService.GetCustomersByDateRangeAsync(previousPeriodStart, previousPeriodEnd);
        var customerGrowth = prevCustomers.Count > 0 ? ((customers.Count - prevCustomers.Count) / (float)prevCustomers.Count * 100) : 100;

        // Calculate conversion rate
        var visitors = await _dashboardService.GetVisitorsCountAsync(startDate, endDate);
        var conversionRate = visitors > 0 ? (orders.Count / (float)visitors * 100) : 0;
        var prevVisitors = await _dashboardService.GetVisitorsCountAsync(previousPeriodStart, previousPeriodEnd);
        var prevConversionRate = prevVisitors > 0 ? (previousOrders.Count / (float)prevVisitors * 100) : 0;
        var conversionGrowth = prevConversionRate > 0 ? ((conversionRate - prevConversionRate) / prevConversionRate * 100) : 100;

        // Get profit margin data
        var profitMargins = await _dashboardService.GetProductProfitMarginsAsync(orders);
        var avgProfitMargin = profitMargins.Any() ? profitMargins.Average() : 0;
        var bestProfitMargin = profitMargins.Any() ? profitMargins.Max() : 0;
        var lowestProfitMargin = profitMargins.Any() ? profitMargins.Min() : 0;

        // Get acquisition channels data
        var acquisitionChannels = await _dashboardService.GetAcquisitionChannelsAsync(startDate, endDate);

        // Get invoice data
        var invoices = await _dashboardService.GetInvoiceStatsByDateRangeAsync(startDate, endDate);

        // Get commission data
        var commissions = await _dashboardService.GetCommissionStatsAsync(startDate, endDate);

        // Get top products
        var topProducts = await _dashboardService.GetTopProductsAsync(startDate, endDate, 5);

        // Get top affiliates
        var topAffiliates = await _dashboardService.GetTopAffiliatesAsync(startDate, endDate, 5);

        // Build revenue chart data
        var revenueChartData = await _dashboardService.GetRevenueChartDataAsync(startDate, endDate);

        // Build growth charts data - use the renamed methods to avoid ambiguity
        var affiliateGrowthChart = await _dashboardService.GetAffiliateGrowthChartDataAsync(startDate, endDate);
        var customerGrowthChart = await _dashboardService.GetCustomerGrowthChartDataAsync(startDate, endDate);

        // Build profit margin chart data
        var profitMarginChart = await _dashboardService.GetProfitMarginChartDataAsync(startDate, endDate);

        // Build commission trend data
        var commissionTrend = await _dashboardService.GetCommissionTrendDataAsync(startDate, endDate);

        // Return dashboard summary data
        return Ok(new
        {
          totalRevenue,
          affiliateEarnings,
          netProfit,
          activeAffiliates,
          affiliateGrowth,
          totalCustomers = customers.Count,
          customerGrowth,
          totalOrders = orders.Count,
          orderGrowth = previousOrders.Count > 0 ? ((orders.Count - previousOrders.Count) / (float)previousOrders.Count * 100) : 100,
          conversionRate,
          conversionGrowth,
          avgProfitMargin,
          bestProfitMargin,
          lowestProfitMargin,
          acquisitionChannels,
          invoices,
          commissions,
          topProducts,
          topAffiliates,
          revenueChart = revenueChartData,
          affiliateGrowthChart,
          customerGrowthChart,
          profitMarginChart,
          commissionTrend
        });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving dashboard data");
        return StatusCode(500, new { error = ex.Message });
      }
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 10)
    {
      try
      {
        var orders = await _dashboardService.GetOrdersByDateRangeAsync(startDate, endDate);

        // Enrich orders with additional information for the dashboard
        var enrichedOrders = new List<object>();

        foreach (var order in orders)
        {
          // Get customer information
          string customerName = "Unknown";
          string customerAvatar = null;

          if (!string.IsNullOrEmpty(order.UserId))
          {
            var user = await _dashboardService.UserService.GetUserByIdAsync(order.UserId);
            if (user != null)
            {
              customerName = $"{user.FirstName} {user.LastName}";
            }
          }

          // Get affiliate information
          string affiliateName = null;

          // Get order details for product count
          var details = await _dashboardService.OrderService.GetOrderDetailsAsync(order.OrderId);

          // Get commissions
          var orderCommissions = await _dashboardService.OrderService.GetOrderCommissionsAsync(order.OrderId);
          decimal commissionAmount = orderCommissions.Sum(c => c.Amount);

          // Add enriched order to the list
          enrichedOrders.Add(new
          {
            id = order.OrderId,
            customerName,
            customerAvatar,
            affiliateName,
            date = order.OrderDate.ToDateTime(),
            productCount = details.Count,
            amount = order.TotalAmount,
            commission = commissionAmount,
            status = order.Status
          });
        }

        // Apply pagination
        var pagedOrders = enrichedOrders
            .OrderByDescending(o => ((dynamic)o).date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Ok(pagedOrders);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error retrieving orders for dashboard");
        return StatusCode(500, new { error = ex.Message });
      }
    }
  }
}
