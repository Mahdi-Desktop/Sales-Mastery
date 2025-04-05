using Google.Cloud.Firestore;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class OrderService : IOrderService
  {
    private readonly FirestoreDb _firestoreDb;
    private readonly string _collectionName = "orders";

    public OrderService(FirestoreDb firestoreDb)
    {
      _firestoreDb = firestoreDb;
    }

    public async Task AddOrder(Order order)
    {
      order.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
      order.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(order.OrderId);
      await docRef.SetAsync(order);
    }

    public async Task<Order?> GetOrderById(string orderId)
    {
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(orderId);
      DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

      return snapshot.Exists ? snapshot.ConvertTo<Order>() : null;
    }

    public async Task<List<Order>> GetAllOrders()
    {
      QuerySnapshot snapshot = await _firestoreDb.Collection(_collectionName).GetSnapshotAsync();
      List<Order> orders = new();

      foreach (DocumentSnapshot doc in snapshot.Documents)
      {
        orders.Add(doc.ConvertTo<Order>());
      }

      return orders;
    }

    public async Task UpdateOrder(Order order)
    {
      order.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(order.OrderId);
      await docRef.SetAsync(order, SetOptions.MergeAll);
    }

    public async Task DeleteOrder(string orderId)
    {
      DocumentReference docRef = _firestoreDb.Collection(_collectionName).Document(orderId);
      await docRef.DeleteAsync();
    }
  }
}
