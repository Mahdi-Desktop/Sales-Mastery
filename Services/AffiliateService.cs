using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using Microsoft.Extensions.DependencyInjection;
//using AspnetCoreMvcFull.Services.Interfaces;
public class AffiliateService : FirestoreService<Affiliate>
{
  private readonly ILogger<AffiliateService> _logger;
  private const string CollectionName = "affiliates";
  private readonly UserService _userService;
  //private readonly IServiceProvider _serviceProvider;
  public AffiliateService(
    IConfiguration configuration,
    ILogger<AffiliateService> logger,
    UserService userService
    )
      : base(configuration, CollectionName)
  {
    _logger = logger;
    _userService = userService;
    //_serviceProvider = serviceProvider;
  }


  public async Task<List<Affiliate>> GetAllAffiliatesAsync()
  {
    return await GetAllAsync();
  }

  public async Task<Affiliate> GetAffiliateByIdAsync(string id)
  {
    return await GetByIdAsync(id);
  }

  public async Task<Affiliate> GetAffiliateByUserIdAsync(string userId)
  {
    QuerySnapshot snapshot = await _collection
        .WhereEqualTo("UserId", userId)
        .GetSnapshotAsync();

    if (snapshot.Documents.Count > 0)
    {
      var affiliate = snapshot.Documents[0].ConvertTo<Affiliate>();
      affiliate.AffiliateId = snapshot.Documents[0].Id;
      return affiliate;
    }

    return null;
  }

  public async Task<string> CreateAffiliateAsync(Affiliate affiliate)
  {
    affiliate.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
    affiliate.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

    return await AddAsync(affiliate);
  }

  public async Task UpdateAffiliateAsync(string id, Affiliate affiliate)
  {
    affiliate.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
    await UpdateAsync(id, affiliate);
  }

  public async Task DeleteAffiliateAsync(string id)
  {
    await DeleteAsync(id);
  }

  /*  public async Task<List<AffiliateWithUserDetails>> GetAffiliatesWithUserDetailsAsync()
    {
      var affiliates = await GetAllAsync();
      var result = new List<AffiliateWithUserDetails>();

      foreach (var affiliate in affiliates)
      {
        var user = await _userService.GetUserByIdAsync(affiliate.UserId);
        if (user != null)
        {
          result.Add(new AffiliateWithUserDetails
          {
            Affiliate = affiliate,
            User = user
          });
        }
      }

      return result;
    }
  */
  // Then modify all methods that use _userService to use GetUserService() instead
  public async Task<List<AffiliateWithUserDetails>> GetAffiliatesWithUserDetailsAsync()
  {
    var affiliates = await GetAllAsync();
    var result = new List<AffiliateWithUserDetails>();

    foreach (var affiliate in affiliates)
    {
      var user = await _userService.GetUserByIdAsync(affiliate.UserId);
      if (user != null)
      {
        result.Add(new AffiliateWithUserDetails
        {
          Affiliate = affiliate,
          User = user
        });
      }
    }

    return result;
  }
  public async Task<List<Customer>> GetCustomersByAffiliateIdAsync(string affiliateId)
  {
    try
    {
      var query = _firestoreDb.Collection("customers").WhereEqualTo("AffiliateId", affiliateId);
      var snapshot = await query.GetSnapshotAsync();

      var customers = new List<Customer>();
      foreach (var document in snapshot.Documents)
      {
        var customer = document.ConvertTo<Customer>();
        customer.CustomerId = document.Id;
        customers.Add(customer);
      }

      return customers;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error getting customers for affiliate {affiliateId}");
      return new List<Customer>();
    }
  }


  public async Task<string> AddAffiliateAsync(string userId, string adminUserId)
  {
    try
    {
      var affiliate = new Affiliate
      {
        UserId = userId,
        ReferenceUserId = adminUserId,
        CommissionRate = 0, // This is a default - will be overridden by brand-specific rates
        CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
        UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow)
      };

      return await AddAsync(affiliate);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error adding affiliate for user {userId}");
      throw;
    }
  }

  public async Task<decimal> CalculateCommission(string productId, decimal productPrice, ProductService productService)
  {
    try
    {
      // Get the product details to find the brand
      var product = await productService.GetProductById(productId);
      if (product == null) return 0;

      // Get the brand to determine commission rate
      var brand = await _firestoreDb.Collection("brands").Document(product.BrandId).GetSnapshotAsync();
      if (!brand.Exists) return 0;

      // Use fully qualified name to resolve ambiguity
      var brandData = brand.ConvertTo<AspnetCoreMvcFull.DTO.Brand>();

      // Calculate commission based on brand's commission rate
      int commissionRate = brandData.CommissionRate;
      return productPrice * (commissionRate / 100m);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error calculating commission for product {productId}");
      return 0;
    }
  }
  /*  public async Task<Affiliate> GetAffiliateByUserIdAsync(string userId)
    {
      try
      {
        var query = _collection.WhereEqualTo("UserId", userId);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count == 0)
          return null;

        var document = snapshot.FirstOrDefault();
        var affiliate = document.ConvertTo<Affiliate>();
        affiliate.AffiliateId = document.Id;
        return affiliate;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting affiliate for user {userId}");
        return null;
      }
    }*/

  public async Task<List<Affiliate>> GetAffiliatesByAdminAsync(string adminUserId)
  {
    try
    {
      var query = _collection.WhereEqualTo("ReferenceUserId", adminUserId);
      var snapshot = await query.GetSnapshotAsync();

      var affiliates = new List<Affiliate>();
      foreach (var document in snapshot.Documents)
      {
        var affiliate = document.ConvertTo<Affiliate>();
        affiliate.AffiliateId = document.Id;
        affiliates.Add(affiliate);
      }

      return affiliates;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error getting affiliates for admin {adminUserId}");
      return new List<Affiliate>();
    }
  }
}
