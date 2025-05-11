using AspnetCoreMvcFull.DTO;

namespace AspnetCoreMvcFull.Services.Interfaces
{
  public interface IAffiliateService
  {
    Task<Affiliate> GetAffiliateByUserIdAsync(string userId);
    // Other methods...
  }


}
