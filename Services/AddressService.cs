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

    /*public async Task<IEnumerable<Address>> GetAddressesByUserIdAsync(string userId)
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
    }*/

    public async Task<List<Address>> GetAddressesByUserIdAsync(string userId)
    {
      try
      {
        var query = _collection.WhereEqualTo("UserId", userId);
        var snapshot = await query.GetSnapshotAsync();

        return snapshot.Documents
            .Select(doc => {
              var address = doc.ConvertTo<Address>();
              address.AddressId = doc.Id;
              return address;
            })
            .ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting addresses for user {userId}");
        return new List<Address>();
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


    /*    public async Task<string> AddAddressAsync(Address address)
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
    */

    public async Task<string> AddAddressAsync(Address address)
    {
      try
      {
        // Set timestamps
        address.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        address.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        string addressId = await AddAsync(address);

        // If this is marked as primary, update other addresses to not be primary
        if (address.IsPrimary)
        {
          await UpdateOtherAddressesToNonPrimaryAsync(address.UserId, addressId);
        }

        return addressId;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error adding address");
        throw;
      }
    }
    /*public async Task UpdateAddressAsync(Address address)
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
    }*/

    public async Task UpdateAddressAsync(Address address)
        {
            try
            {
                // Update timestamp
                address.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

                await UpdateAsync(address.AddressId, address);

                // If this is marked as primary, update other addresses to not be primary
                if (address.IsPrimary)
                {
                    await UpdateOtherAddressesToNonPrimaryAsync(address.UserId, address.AddressId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating address with ID {address.AddressId}");
                throw;
            }
        }

    private async Task UpdateOtherAddressesToNonPrimaryAsync(string userId, string currentAddressId)
    {
      try
      {
        var query = _collection
            .WhereEqualTo("UserId", userId)
            .WhereEqualTo("IsPrimary", true)
            .WhereNotEqualTo(FieldPath.DocumentId, currentAddressId);

        var snapshot = await query.GetSnapshotAsync();

        var batch = _firestoreDb.StartBatch();
        foreach (var doc in snapshot.Documents)
        {
          var addressRef = _collection.Document(doc.Id);
          batch.Update(addressRef, "IsPrimary", false);
          batch.Update(addressRef, "UpdatedAt", Timestamp.FromDateTime(DateTime.UtcNow));
        }

        await batch.CommitAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating other addresses to non-primary for user {userId}");
        // Don't throw here, just log the error
      }
    }

  /*  public async Task DeleteAddressAsync(string addressId)
    {
      try
      {
        // Get the address first to check if it's primary
        var address = await GetByIdAsync(addressId);

        // Delete the address
        await DeleteAsync(addressId);

        // If this was a primary address, set another address as primary if available
        if (address != null && address.IsPrimary)
        {
          await SetNewPrimaryAddressAsync(address.UserId);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting address with ID {addressId}");
        throw;
      }
    }*/

    private async Task SetNewPrimaryAddressAsync(string userId)
    {
      try
      {
        // Find any remaining address for this user
        var query = _collection.WhereEqualTo("UserId", userId).Limit(1);
        var snapshot = await query.GetSnapshotAsync();

        if (snapshot.Count > 0)
        {
          var doc = snapshot.Documents[0];
          var addressRef = _collection.Document(doc.Id);
          await addressRef.UpdateAsync("IsPrimary", true);
          await addressRef.UpdateAsync("UpdatedAt", Timestamp.FromDateTime(DateTime.UtcNow));
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error setting new primary address for user {userId}");
        // Don't throw here, just log the error
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
