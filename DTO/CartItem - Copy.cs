namespace AspnetCoreMvcFull.DTO
{
    public class CartItem
    {
      public string ProductId { get; set; }
      public string Name { get; set; }
      public string ImageUrl { get; set; }
      public decimal Price { get; set; }
      public int Quantity { get; set; }
      public decimal SubTotal => Price * Quantity;
    }
  }

