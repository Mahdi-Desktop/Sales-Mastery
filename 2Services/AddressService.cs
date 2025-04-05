/*using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Services
{
  public class AddressService : FirestoreService<Address>
  {
    public AddressService(FirestoreDb db) : base(db) { }

    public async Task<List<Address>> GetAllAddressesAsync()
    {
      try
      {
        QuerySnapshot snapshot = await _db.Collection("Addresses").GetSnapshotAsync();
        List<Address> addresses = new List<Address>();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
          Address address = document.ConvertTo<Address>();
          address.AddressId = document.Id; // Using AddressId instead of Id
          addresses.Add(address);
        }
        return addresses;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error in GetAllAddressesAsync: {ex.Message}");
        throw;
      }
    }

    public async Task<List<Address>> GetAddressesByUserIdAsync(string userId)
    {
      try
      {
        Query query = _db.Collection("Addresses").WhereEqualTo("UserId", userId);
        QuerySnapshot snapshot = await query.GetSnapshotAsync();

        List<Address> addresses = new List<Address>();
        foreach (DocumentSnapshot document in snapshot.Documents)
        {
          Address address = document.ConvertTo<Address>();
          address.AddressId = document.Id; // Using AddressId instead of Id
          addresses.Add(address);
        }
        return addresses;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error getting addresses by user ID: {ex.Message}");
        throw;
      }
    }
  }
}
*/
