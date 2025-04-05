/*using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Cloud.Firestore;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.DTO;


public class BrandCategoryMappingService : FirestoreService<BrandCategoryMapping>
{
  public BrandCategoryMappingService(IConfiguration configuration) : base(configuration) { }

  public async Task<List<BrandCategoryMapping>> GetAllMappingsAsync()
  {
    QuerySnapshot snapshot = await _db.Collection("BrandCategoryMappings").GetSnapshotAsync();
    List<BrandCategoryMapping> mappings = new List<BrandCategoryMapping>();
    foreach (DocumentSnapshot document in snapshot.Documents)
    {
      mappings.Add(document.ConvertTo<BrandCategoryMapping>());
    }
    return mappings;
  }

  public async Task<List<BrandCategoryMapping>> GetMappingsByBrandIdAsync(string brandId)
  {
    Query query = _db.Collection("BrandCategoryMappings").WhereEqualTo("BrandId", brandId);
    QuerySnapshot snapshot = await query.GetSnapshotAsync();
    List<BrandCategoryMapping> mappings = new List<BrandCategoryMapping>();
    foreach (DocumentSnapshot document in snapshot.Documents)
    {
      mappings.Add(document.ConvertTo<BrandCategoryMapping>());
    }
    return mappings;
  }
}
*/
