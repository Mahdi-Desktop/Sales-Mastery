
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
    public class CommissionService : FirestoreService<Commission>
    {
      private readonly ILogger<CommissionService> _logger;
      private const string CollectionName = "commissions";

      public CommissionService(IConfiguration configuration, ILogger<CommissionService> logger)
          : base(configuration, CollectionName)
      {
        _logger = logger;
      }

      public async Task<List<Commission>> GetCommissionsByAffiliateIdAsync(string affiliateId)
      {
        try
        {
          var commissions = new List<Commission>();
          var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("AffiliateId", affiliateId);
          var snapshot = await query.GetSnapshotAsync();

          foreach (var document in snapshot.Documents)
          {
            var commission = document.ConvertTo<Commission>();
            commission.CommissionId = document.Id;
            commissions.Add(commission);
          }

          return commissions;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error getting commissions for affiliate {affiliateId}");
          return new List<Commission>();
        }
      }

      public async Task<List<Commission>> GetCommissionsByOrderIdAsync(string orderId)
      {
        try
        {
          var commissions = new List<Commission>();
          var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("OrderId", orderId);
          var snapshot = await query.GetSnapshotAsync();

          foreach (var document in snapshot.Documents)
          {
            var commission = document.ConvertTo<Commission>();
            commission.CommissionId = document.Id;
            commissions.Add(commission);
          }

          return commissions;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error getting commissions for order {orderId}");
          return new List<Commission>();
        }
      }

      public async Task<decimal> GetTotalCommissionsByAffiliateIdAsync(string affiliateId)
      {
        try
        {
          var commissions = await GetCommissionsByAffiliateIdAsync(affiliateId);
          return commissions.Sum(c => c.Amount);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error calculating total commissions for affiliate {affiliateId}");
          return 0;
        }
      }

      public async Task<decimal> GetUnpaidCommissionsByAffiliateIdAsync(string affiliateId)
      {
        try
        {
          var commissions = await GetCommissionsByAffiliateIdAsync(affiliateId);
          return commissions.Where(c => !c.IsPaid).Sum(c => c.Amount);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error calculating unpaid commissions for affiliate {affiliateId}");
          return 0;
        }
      }

      public async Task<bool> MarkCommissionAsPaidAsync(string commissionId)
      {
        try
        {
          var commission = await GetByIdAsync(commissionId);
          if (commission == null)
          {
            return false;
          }

          commission.IsPaid = true;
          commission.PaidDate = Timestamp.FromDateTime(DateTime.UtcNow);

          await UpdateAsync(commissionId, commission);
          return true;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error marking commission {commissionId} as paid");
          return false;
        }
      }

      public async Task<bool> MarkAllCommissionsAsPaidAsync(string affiliateId)
      {
        try
        {
          var commissions = await GetCommissionsByAffiliateIdAsync(affiliateId);
          var unpaidCommissions = commissions.Where(c => !c.IsPaid).ToList();

          if (!unpaidCommissions.Any())
          {
            return true; // No unpaid commissions to update
          }

          var batch = _firestoreDb.StartBatch();
          var timestamp = Timestamp.FromDateTime(DateTime.UtcNow);

          foreach (var commission in unpaidCommissions)
          {
            commission.IsPaid = true;
            commission.PaidDate = timestamp;

            var docRef = _firestoreDb.Collection(CollectionName).Document(commission.CommissionId);
            batch.Update(docRef, new Dictionary<string, object>
          {
            { "IsPaid", true },
            { "PaidDate", timestamp }
          });
          }

          await batch.CommitAsync();
          return true;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error marking all commissions as paid for affiliate {affiliateId}");
          return false;
        }
      }

      public async Task<string> CreateCommissionAsync(Commission commission)
      {
        try
        {
          if (commission == null)
          {
            throw new ArgumentNullException(nameof(commission));
          }

          // Set created timestamp
          commission.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

          // Default values
          if (commission.IsPaid)
          {
            commission.PaidDate = Timestamp.FromDateTime(DateTime.UtcNow);
          }
          else
          {
            commission.PaidDate = null;
          }

          return await AddAsync(commission);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error creating commission");
          throw;
        }
      }

      public async Task<CommissionSummary> GetAffiliateSummaryAsync(string affiliateId)
      {
        try
        {
          var commissions = await GetCommissionsByAffiliateIdAsync(affiliateId);

          return new CommissionSummary
          {
            AffiliateId = affiliateId,
            TotalCommissions = commissions.Sum(c => c.Amount),
            PaidCommissions = commissions.Where(c => c.IsPaid).Sum(c => c.Amount),
            UnpaidCommissions = commissions.Where(c => !c.IsPaid).Sum(c => c.Amount),
            TotalOrders = commissions.Select(c => c.OrderId).Distinct().Count()
          };
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, $"Error calculating commission summary for affiliate {affiliateId}");
          return new CommissionSummary { AffiliateId = affiliateId };
        }
      }
    }

  }
