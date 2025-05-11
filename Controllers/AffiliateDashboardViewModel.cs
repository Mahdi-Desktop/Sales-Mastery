using AspnetCoreMvcFull.DTO;

// View models for Affiliate pages
public class AffiliateDashboardViewModel
{
  public bool IsAdmin { get; set; }
  public Affiliate CurrentAffiliate { get; set; }
  public User CurrentUser { get; set; }
  public int TotalEarnings { get; set; }
  public int PendingEarnings { get; set; }
  public List<Commission> RecentCommissions { get; set; }
  public int TotalAffiliates { get; set; }
  public List<AffiliateWithUserDetails> AffiliateDetails { get; set; }
}

public class CreateAffiliateViewModel
{
  public string FirstName { get; set; }
  public string LastName { get; set; }
  public string Email { get; set; }
  public string Password { get; set; }
  public string PhoneNumber { get; set; }
  public int CommissionRate { get; set; }
}
