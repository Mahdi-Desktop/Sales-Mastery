using AspnetCoreMvcFull.DTO;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IUserService
  {
    Task<User> GetUserByIdAsync(string id);
    Task<User> GetUserByEmailAsync(string email);
    // Other methods...
  }

}
