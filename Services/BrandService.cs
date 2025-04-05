using AspnetCoreMvcFull.Services;
using Google.Cloud.Firestore;
using AspnetCoreMvcFull.DTO;

public class BrandService : FirestoreService<Brand>
{
  private readonly ILogger<BrandService> _logger;
  private const string CollectionName = "brands";

  public BrandService(IConfiguration configuration, ILogger<BrandService> logger)
      : base(configuration, CollectionName)
  {
    _logger = logger;
  }

  public async Task<List<Brand>> GetAllBrandsAsync()
  {
    try
    {
      var snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();

      var brands = new List<Brand>();
      foreach (var document in snapshot.Documents)
      {
        var brand = document.ConvertTo<Brand>();
        brand.BrandId = document.Id;
        brands.Add(brand);
      }

      return brands;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting all brands");
      return new List<Brand>();
    }
  }
}
