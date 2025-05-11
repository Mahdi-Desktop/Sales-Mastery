namespace AspnetCoreMvcFull.DTO
{
    public class CommissionSummary
    {
      public string AffiliateId { get; set; }
      public decimal TotalCommissions { get; set; }
      public decimal PaidCommissions { get; set; }
      public decimal UnpaidCommissions { get; set; }
      public int TotalOrders { get; set; }
    }

  }
