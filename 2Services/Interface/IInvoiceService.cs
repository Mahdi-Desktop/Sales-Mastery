using AspnetCoreMvcFull.DTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services.Interface
{
  public interface IInvoiceService
  {
    Task<Invoice> GetInvoiceByIdAsync(string invoiceId);
    Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
    Task<IEnumerable<Invoice>> GetInvoicesByUserIdAsync(string userId);
    Task<string> CreateInvoiceAsync(Invoice invoice);
    Task UpdateInvoiceAsync(Invoice invoice);
    Task DeleteInvoiceAsync(string invoiceId);
  }
}
