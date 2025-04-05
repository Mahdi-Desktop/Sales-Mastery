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


  public class CategoryService : FirestoreService<Category>
  {
    public CategoryService(IConfiguration configuration) : base(configuration) { }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
      QuerySnapshot snapshot = await _db.Collection("Categories").GetSnapshotAsync();
      List<Category> categories = new List<Category>();
      foreach (DocumentSnapshot document in snapshot.Documents)
      {
        categories.Add(document.ConvertTo<Category>());
      }
      return categories;
    }
  }

}
*/
