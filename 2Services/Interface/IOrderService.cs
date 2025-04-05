using AspnetCoreMvcFull.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IOrderService
  {
    Task<Order?> GetOrderById(string orderId);
    Task<List<Order>> GetAllOrders();
    Task AddOrder(Order order);
    Task UpdateOrder(Order order);
    Task DeleteOrder(string orderId);
  }
}
