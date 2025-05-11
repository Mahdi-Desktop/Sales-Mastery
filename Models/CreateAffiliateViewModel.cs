using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.Models
{
  public class CreateAffiliateViewModel
  {
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "Email Address")]
    public string Email { get; set; }

    [Required]
    [Phone]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }

    [Required]
    [Range(0, 100, ErrorMessage = "Commission rate must be between 0 and 100")]
    [Display(Name = "Commission Rate (%)")]
    public decimal CommissionRate { get; set; } = 15;
  }
}
