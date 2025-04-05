using AspnetCoreMvcFull.Controllers;

namespace AspnetCoreMvcFull.DTO
{
  public class OrderCreateViewModel
  {
    public string CustomerId { get; set; }
    public List<OrderItemViewModel> OrderItems { get; set; } = new List<OrderItemViewModel>();
  }
}
