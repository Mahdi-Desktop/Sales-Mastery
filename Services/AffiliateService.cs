using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using AspnetCoreMvcFull.DTO;
public class AffiliateService : FirestoreService<Affiliate>
{
  private readonly ILogger<AffiliateService> _logger;
  private const string CollectionName = "affiliates";

  public AffiliateService(IConfiguration configuration, ILogger<AffiliateService> logger)
      : base(configuration, CollectionName)
  {
    _logger = logger;
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

      var brandData = brand.ConvertTo<Brand>();

      // Calculate commission based on brand's commission rate
      decimal commissionRate = brandData.CommissionRate;
      return productPrice * (commissionRate / 100m);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, $"Error calculating commission for product {productId}");
      return 0;
    }
  }

  public async Task<Affiliate> GetAffiliateByUserIdAsync(string userId)
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
  }

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
