/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspnetCoreMvcFull.DTO;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Google.Cloud.Firestore;
namespace AspnetCoreMvcFull.Services
{


  public class BrandService : FirestoreService<Brand>
  {
    public BrandService(IConfiguration configuration) : base(configuration) { }

    public async Task<List<Brand>> GetAllBrandsAsync()
    {
      QuerySnapshot snapshot = await _db.Collection("Brands").GetSnapshotAsync();
      List<Brand> brands = new List<Brand>();
      foreach (DocumentSnapshot document in snapshot.Documents)
      {
        brands.Add(document.ConvertTo<Brand>());
      }
      return brands;
    }
  }

}
*/
