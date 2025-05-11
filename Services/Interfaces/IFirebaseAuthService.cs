using System.Threading.Tasks;
using AspnetCoreMvcFull.Services;

namespace AspnetCoreMvcFull.Interfaces;

public interface IFirebaseAuthService
{
  Task<string?> SignUp(string email, string password);
  Task<string?> Login(string email, string password);
  Task<string?> SendPhoneVerificationCode(string phoneNumber);
  Task<bool> VerifyPhoneNumber(string verificationId, string code);
  Task<bool> ResetPasswordByPhone(string uid, string newPassword);
  void SignOut();
  Task<string> GetTokenAsync(string email, string password);
}
