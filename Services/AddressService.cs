using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class AddressService : FirestoreService<Address>
  {
    private readonly ILogger<AddressService> _logger;
    private const string CollectionName = "addresses";

    public AddressService(IConfiguration configuration, ILogger<AddressService> logger)
        : base(configuration, CollectionName)
    {
      _logger = logger;
    }

    public async Task<IEnumerable<Address>> GetAddressesByUserIdAsync(string userId)
    {
      try
      {
        var query = _firestoreDb.Collection(CollectionName)
            .WhereEqualTo(nameof(Address.UserId), userId);

        var snapshot = await query.GetSnapshotAsync();
        return snapshot.Documents.Select(doc =>
        {
          var address = doc.ConvertTo<Address>();
          address.FirestoreId = doc.Id;
          return address;
        }).ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting addresses for user with ID {userId}");
        throw;
      }
    }

    public async Task<Address> GetAddressByIdAsync(string addressId)
    {
      try
      {
        return await GetByIdAsync(addressId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting address with ID {addressId}");
        throw;
      }
    }

    public async Task<string> AddAddressAsync(Address address)
    {
      try
      {
        // Set creation timestamp
        address.CreatedAt = DateTime.UtcNow;

        return await AddAsync(address);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error adding address");
        throw;
      }
    }

    public async Task UpdateAddressAsync(Address address)
    {
      try
      {
        await UpdateAsync(address.FirestoreId, address);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating address with ID {address.FirestoreId}");
        throw;
      }
    }

    public async Task DeleteAddressAsync(string addressId)
    {
      try
      {
        await DeleteAsync(addressId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting address with ID {addressId}");
        throw;
      }
    }
  }
}
