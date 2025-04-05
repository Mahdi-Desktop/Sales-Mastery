using AspnetCoreMvcFull.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services.Interface
{
  public interface IUserService
  {
    Task<User> GetUserByIdAsync(string userId);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<string> AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeleteUserAsync(string userId);
    Task<User> GetUserByEmailAsync(string email);
  }
}
