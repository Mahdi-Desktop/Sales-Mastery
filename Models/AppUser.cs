using Microsoft.AspNetCore.Identity;

namespace AspnetCoreMvcFull.Models
{
  public class AppUser : IdentityUser
  {
    public string FirebaseUserId { get; set; }
    public string Name { get; set; }
  }
}
