using Google.Cloud.Firestore;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class ProductService : IProductService
  {
    private readonly FirestoreDb _firestoreDb;
    private readonly string _collectionName = "products";

    public ProductService(FirestoreDb firestoreDb)
    {
      _firestoreDb = firestoreDb;
    }

    public async Task AddProduct(Product product)
    {
      product.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
      product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(product.ProductId);
      await docRef.SetAsync(product);
    }

    public async Task<Product?> GetProductById(string productId)
    {
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(productId);
      DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

      return snapshot.Exists ? snapshot.ConvertTo<Product>() : null;
    }

    public async Task<List<Product>> GetAllProducts()
    {
      QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
      List<Product> products = new();

      foreach (DocumentSnapshot doc in snapshot.Documents)
      {
        products.Add(doc.ConvertTo<Product>());
      }

      return products;
    }

    public async Task UpdateProduct(Product product)
    {
      product.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(product.prodId);
      await docRef.SetAsync(product, SetOptions.MergeAll);
    }

    public async Task DeleteProduct(string productId)
    {
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(productId);
      await docRef.DeleteAsync();
    }
  }
}
