using AspnetCoreMvcFull.DTO;
using Google.Cloud.Firestore;
using AspnetCoreMvcFull.Services.Interface;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Services
{
  public class InvoiceService : IInvoiceService
  {
    private readonly FirestoreDb _firestoreDb;
    private readonly ILogger<InvoiceService> _logger;
    private const string CollectionName = "Invoices";

    public InvoiceService(FirestoreDb firestoreDb, ILogger<InvoiceService> logger)
    {
      _firestoreDb = firestoreDb;
      _logger = logger;
    }

    public async Task<Invoice> GetInvoiceByIdAsync(string invoiceId)
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(CollectionName).Document(invoiceId).GetSnapshotAsync();
        if (!snapshot.Exists)
        {
          _logger.LogWarning("Invoice with ID {InvoiceId} not found", invoiceId);
          return null;
        }

        var invoice = snapshot.ConvertTo<Invoice>();
        invoice.InvoiceId = snapshot.Id;
        return invoice;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting invoice by ID {InvoiceId}", invoiceId);
        throw;
      }
    }

    public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
    {
      try
      {
        var snapshot = await _firestoreDb.Collection(CollectionName).GetSnapshotAsync();
        return snapshot.Documents.Select(doc =>
        {
          var invoice = doc.ConvertTo<Invoice>();
          invoice.InvoiceId = doc.Id;
          return invoice;
        }).ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all invoices");
        throw;
      }
    }

    public async Task<IEnumerable<Invoice>> GetInvoicesByUserIdAsync(string userId)
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
        _logger.LogError(ex, "Error getting invoices for user {UserId}", userId);
        throw;
      }
    }

    public async Task<string> CreateInvoiceAsync(Invoice invoice)
    {
      try
      {
        invoice.CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        invoice.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);

        var docRef = await _firestoreDb.Collection(CollectionName).AddAsync(invoice);
        return docRef.Id;
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
        invoice.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        await _firestoreDb.Collection(CollectionName)
            .Document(invoice.InvoiceId)
            .SetAsync(invoice, SetOptions.MergeAll);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating invoice {InvoiceId}", invoice.InvoiceId);
        throw;
      }
    }

    public async Task DeleteInvoiceAsync(string invoiceId)
    {
      try
      {
        await _firestoreDb.Collection(CollectionName).Document(invoiceId).DeleteAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting invoice {InvoiceId}", invoiceId);
        throw;
      }
    }
  }
}
