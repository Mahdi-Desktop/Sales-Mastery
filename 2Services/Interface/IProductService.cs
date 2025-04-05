using AspnetCoreMvcFull.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IProductService
  {
    Task<Product?> GetProductById(string productId);
    Task<List<Product>> GetAllProducts();
    Task AddProduct(Product product);
    Task UpdateProduct(Product product);
    Task DeleteProduct(string productId);
  }
}
