/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspnetCoreMvcFull.DTO;

namespace AspnetCoreMvcFull.Services
{
  public class CustomerService
  {
    private readonly FirebaseService _firebaseService;
    private readonly string _collectionPath = "customers";

    public CustomerService(FirebaseService firebaseService)
    {
      _firebaseService = firebaseService;
    }

    // Get all customers
    public async Task<List<Customer>> GetAllCustomersAsync()
    {
      return await _firebaseService.GetCollectionAsync<Customer>(_collectionPath);
    }

    // Get customer by ID
    public async Task<Customer> GetCustomerByIdAsync(string id)
    {
      return await _firebaseService.GetDocumentAsync<Customer>(id, _collectionPath);
    }

    // Get customer by Email
    public async Task<Customer> GetCustomerByEmailAsync(string email)
    {
      var customers = await _firebaseService.QueryCollectionAsync<Customer>(_collectionPath, "Email", email);
      return customers.Count > 0 ? customers[0] : null;
    }

    // Create new customer
    public async Task<Customer> CreateCustomerAsync(Customer customer)
    {
      customer.CreatedAt = DateTime.Now;
      customer.UpdatedAt = DateTime.Now;

      return await _firebaseService.CreateDocumentAsync(customer, _collectionPath);
    }

    // Update existing customer
    public async Task<Customer> UpdateCustomerAsync(string id, Customer customer)
    {
      customer.UpdatedAt = DateTime.Now;

      return await _firebaseService.UpdateDocumentAsync(id, customer, _collectionPath);
    }

    // Delete customer
    public async Task DeleteCustomerAsync(string id)
    {
      await _firebaseService.DeleteDocumentAsync(id, _collectionPath);
    }

    // Get customers by User ID
    public async Task<List<Customer>> GetCustomersByUserIdAsync(string userId)
    {
      return await _firebaseService.QueryCollectionAsync<Customer>(_collectionPath, "UserId", userId);
    }
  }
}
*/
