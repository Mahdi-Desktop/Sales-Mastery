using System.Threading.Tasks;
using AspnetCoreMvcFull.DTO;

namespace AspnetCoreMvcFull.Services.Interface
{
  public interface IFirebaseAuthService
  {
    Task<User> LoginAsync(string email, string password);
    Task<string> SignUpAsync(string email, string password, string role, string createdBy);
    Task SendPasswordResetEmailAsync(string email);
    Task<User> GetUserAsync(string userId);
    Task LogoutAsync();
  }
}
