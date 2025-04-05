using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class CustomerService : FirestoreService<Customer>
  {
    private readonly ILogger<CustomerService> _logger;
    private const string CollectionName = "customers";

    public CustomerService(IConfiguration configuration, ILogger<CustomerService> logger)
        : base(configuration, CollectionName)
    {
      _logger = logger;
    }

    public async Task<Customer> GetCustomerByUserIdAsync(string userId)
    {
      try
      {
        var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("UserId", userId);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count > 0)
        {
          var document = snapshot.Documents[0];
          var customer = document.ConvertTo<Customer>();
          customer.CustomerId = document.Id;
          return customer;
        }

        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting customer with userId {userId}");
        return null;
      }
    }

    public async Task<Customer> GetCustomerByEmailAsync(string email)
    {
      try
      {
        var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("Email", email);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count > 0)
        {
          var document = snapshot.Documents[0];
          var customer = document.ConvertTo<Customer>();
          customer.CustomerId = document.Id;
          return customer;
        }

        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting customer with email {email}");
        return null;
      }
    }

    public async Task<List<Customer>> GetCustomersByReferrerAsync(string referrerId)
    {
      try
      {
        var customers = new List<Customer>();
        var query = _firestoreDb.Collection(CollectionName).WhereEqualTo("ReferenceUserId", referrerId);
        var snapshot = await query.GetSnapshotAsync();

        foreach (var document in snapshot.Documents)
        {
          var customer = document.ConvertTo<Customer>();
          customer.CustomerId = document.Id;
          customers.Add(customer);
        }

        return customers;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting customers for referrer {referrerId}");
        return new List<Customer>();
      }
    }

    internal async Task AddCustomerAsync(string userId, string currentUserId)
    {
      throw new NotImplementedException();
    }
  }
}
