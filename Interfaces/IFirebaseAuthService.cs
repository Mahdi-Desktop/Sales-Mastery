namespace AspnetCoreMvcFull.Interfaces;

public interface IFirebaseAuthService
{
    public Task<string?> SignUp(string email, string password);

    public Task<string?> Login(string email, string password);
  public async Task<string?> SendPhoneVerificationCode(string phoneNumber)
  {
    // This is a placeholder - not actually sending SMS
    return await Task.FromResult("verification-id-placeholder");
  }
  public async Task<bool> VerifyPhoneNumber(string verificationId, string code)
  {
    // This is a placeholder - always returns true
    return await Task.FromResult(true);
  }
  public Task<bool> ResetPasswordByPhone(string uid, string newPassword);
    public void SignOut();
}
