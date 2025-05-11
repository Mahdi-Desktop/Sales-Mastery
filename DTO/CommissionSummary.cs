namespace AspnetCoreMvcFull.DTO
{
    public class CommissionSummary
    {
      public string AffiliateId { get; set; }
      public double TotalCommissions { get; set; }
      public double PaidCommissions { get; set; }
      public double UnpaidCommissions { get; set; }
      public int TotalOrders { get; set; }
    }

  }
