/*using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Cloud.Firestore;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.DTO;

public class BrandCollectionMappingService : FirestoreService<BrandCollectionMapping>
{
  public BrandCollectionMappingService(IConfiguration configuration) : base(configuration) { }

  public async Task<List<BrandCollectionMapping>> GetAllMappingsAsync()
  {
    QuerySnapshot snapshot = await _db.Collection("BrandCollectionMappings").GetSnapshotAsync();
    List<BrandCollectionMapping> mappings = new List<BrandCollectionMapping>();
    foreach (DocumentSnapshot document in snapshot.Documents)
    {
      mappings.Add(document.ConvertTo<BrandCollectionMapping>());
    }
    return mappings;
  }

  public async Task<List<BrandCollectionMapping>> GetMappingsByBrandIdAsync(string brandId)
  {
    Query query = _db.Collection("BrandCollectionMappings").WhereEqualTo("BrandId", brandId);
    QuerySnapshot snapshot = await query.GetSnapshotAsync();
    List<BrandCollectionMapping> mappings = new List<BrandCollectionMapping>();
    foreach (DocumentSnapshot document in snapshot.Documents)
    {
      mappings.Add(document.ConvertTo<BrandCollectionMapping>());
    }
    return mappings;
  }
}
*/
