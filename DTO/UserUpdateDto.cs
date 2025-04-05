using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.DTO
{
  public class UserUpdateDto
  {
    [Required]
    public string UserId { get; set; }

    [Required]
    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    [Required]
    public string Role { get; set; }

    [Required(ErrorMessage = "Please enter your password to confirm changes")]
    public string VerificationPassword { get; set; }
  }
}
