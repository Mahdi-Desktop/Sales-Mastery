using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class InvoiceService : FirestoreService<Invoice>
  {
    private readonly ILogger<InvoiceService> _logger;
    private const string CollectionName = "invoices";

    public InvoiceService(IConfiguration configuration, ILogger<InvoiceService> logger)
        : base(configuration, CollectionName)
    {
      _logger = logger;
    }

    public async Task<Invoice> GetInvoiceByIdAsync(string invoiceId)
    {
      try
      {
        return await GetByIdAsync(invoiceId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting invoice with ID {invoiceId}");
        throw;
      }
    }

    public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
    {
      try
      {
        return await GetAllAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all invoices");
        throw;
      }
    }

    /*    public async Task<IEnumerable<Invoice>> GetInvoicesByUserIdAsync(string userId)
        {
          try
          {
            var query = _firestoreDb.Collection(CollectionName)
                .WhereEqualTo(nameof(Invoice.UserId), userId);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Documents.Select(doc =>
            {
              var invoice = doc.ConvertTo<Invoice>();
              invoice.InvoiceId = doc.Id;
              return invoice;
            }).ToList();
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, $"Error getting invoices for user with ID {userId}");
            throw;
          }
        }
    */
    public async Task<IEnumerable<Invoice>> GetInvoicesByUserIdAsync(string userId)
    {
      try
      {
        // Check if the collection exists first
        var collectionRef = _firestoreDb.Collection(CollectionName);
        var emptyQuery = await collectionRef.Limit(1).GetSnapshotAsync();

        // If the collection doesn't exist or is empty, return an empty list
        if (!emptyQuery.Any())
        {
          return new List<Invoice>();
        }

        // Otherwise, proceed with the original query
        var query = collectionRef.WhereEqualTo(nameof(Invoice.UserId), userId);
        var snapshot = await query.GetSnapshotAsync();
        return snapshot.Documents.Select(doc =>
        {
          var invoice = doc.ConvertTo<Invoice>();
          invoice.InvoiceId = doc.Id;
          return invoice;
        }).ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting invoices for user with ID {userId}");
        // Return empty list instead of throwing
        return new List<Invoice>();
      }
    }

    public async Task<string> CreateInvoiceAsync(Invoice invoice)
    {
      try
      {
        // Set timestamps
        invoice.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        invoice.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        // Generate invoice number if not provided
        if (string.IsNullOrEmpty(invoice.InvoiceNumber))
        {
          invoice.InvoiceNumber = GenerateInvoiceNumber();
        }

        // Calculate total if not set
        if (invoice.TotalAmount == 0 && invoice.Items != null && invoice.Items.Any())
        {
          invoice.TotalAmount = invoice.Items.Sum(i => i.Total);
        }

        return await AddAsync(invoice);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating invoice");
        throw;
      }
    }

    public async Task UpdateInvoiceAsync(Invoice invoice)
    {
      try
      {
        // Update timestamp
        invoice.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        // Recalculate total if items are provided
        if (invoice.Items != null && invoice.Items.Any())
        {
          invoice.TotalAmount = invoice.Items.Sum(i => i.Total);
        }

        await UpdateAsync(invoice.InvoiceId, invoice);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating invoice with ID {invoice.InvoiceId}");
        throw;
      }
    }

    public async Task DeleteInvoiceAsync(string invoiceId)
    {
      try
      {
        await DeleteAsync(invoiceId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error deleting invoice with ID {invoiceId}");
        throw;
      }
    }

    private string GenerateInvoiceNumber()
    {
      // Generate a unique invoice number
      // Format: INV-YYYYMMDD-XXXX where XXXX is a random number
      string dateComponent = DateTime.UtcNow.ToString("yyyyMMdd");
      string randomComponent = new Random().Next(1000, 9999).ToString();
      return $"INV-{dateComponent}-{randomComponent}";
    }

    public async Task<Invoice> GetInvoiceDetailsAsync(string invoiceId)
    {
      try
      {
        var invoice = await GetByIdAsync(invoiceId);
        if (invoice == null)
        {
          _logger.LogWarning($"Invoice with ID {invoiceId} not found");
          return null;
        }

        // Ensure all related data is loaded
        if (invoice.Items == null)
        {
          invoice.Items = new List<InvoiceItem>();
        }

        return invoice;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error getting invoice details for ID {invoiceId}");
        throw;
      }
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(string invoiceId)
    {
      try
      {
        var invoice = await GetInvoiceDetailsAsync(invoiceId);
        if (invoice == null)
        {
          return null;
        }

        // Here you would implement PDF generation logic
        // This is a placeholder - you would need to use a PDF library like iTextSharp or DinkToPdf

        // For now, we'll just return a dummy byte array
        return new byte[0];
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error generating PDF for invoice with ID {invoiceId}");
        throw;
      }
    }

    public async Task<bool> UpdateInvoiceStatusAsync(string invoiceId, string newStatus)
    {
      try
      {
        var invoice = await GetByIdAsync(invoiceId);
        if (invoice == null)
        {
          return false;
        }

        invoice.Status = newStatus;
        invoice.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        await UpdateAsync(invoiceId, invoice);
        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"Error updating status for invoice with ID {invoiceId}");
        return false;
      }
    }
  }
}
