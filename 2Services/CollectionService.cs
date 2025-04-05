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

  public class CollectionService : FirestoreService<Collection>
  {
    public CollectionService(IConfiguration configuration) : base(configuration) { }

    public async Task<List<Collection>> GetAllCollectionsAsync()
    {
      QuerySnapshot snapshot = await _db.Collection("Collections").GetSnapshotAsync();
      List<Collection> collections = new List<Collection>();
      foreach (DocumentSnapshot document in snapshot.Documents)
      {
        collections.Add(document.ConvertTo<Collection>());
      }
      return collections;
    }
  }

}
*/
