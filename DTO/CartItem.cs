namespace AspnetCoreMvcFull.DTO
{
  public class CartItem
  {
    public required string ProductId { get; set; }
    public required string Name { get; set; }
    public required string ImageUrl { get; set; }
    public int Price { get; set; }
    public int Quantity { get; set; }
    public int SubTotal => Price * Quantity;
  }
}
