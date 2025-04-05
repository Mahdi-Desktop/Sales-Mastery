using AspnetCoreMvcFull.DTO;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class InvoiceController : Controller
  {
    private readonly InvoiceService _invoiceService;
    private readonly ProductService _productService;

    public InvoiceController(InvoiceService invoiceService, ProductService productService)
    {
      _invoiceService = invoiceService;
      _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoiceDetails(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      try
      {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
          return NotFound();
        }

        // Enrich invoice items with product details if needed
        if (invoice.Items != null)
        {
          foreach (var item in invoice.Items)
          {
            if (!string.IsNullOrEmpty(item.ProductId) && string.IsNullOrEmpty(item.ProductName))
            {
              var product = await _productService.GetProductById(item.ProductId);
              if (product != null)
              {
                item.ProductName = product.Name;
                item.ProductSKU = product.SKU;
                if (string.IsNullOrEmpty(item.Description))
                {
                  item.Description = product.Description;
                }
              }
            }
          }
        }

        return Json(invoice);
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    [HttpGet]
    public async Task<IActionResult> Download(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      try
      {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
          return NotFound();
        }

        // In a real application, you would generate a PDF here
        // For now, we'll just return a simple text file
        string content = $"Invoice #{invoice.InvoiceNumber}\n";
        content += $"Date: {invoice.InvoiceDate.ToDateTime():yyyy-MM-dd}\n";
        content += $"Status: {invoice.Status}\n";
        content += $"Total: ${invoice.TotalAmount}\n\n";

        content += "Items:\n";
        if (invoice.Items != null && invoice.Items.Count > 0)
        {
          foreach (var item in invoice.Items)
          {
            content += $"- {item.ProductName} (SKU: {item.ProductSKU})\n";
            content += $"  Description: {item.Description}\n";
            content += $"  Quantity: {item.Quantity}, Unit Price: ${item.UnitPrice}, Total: ${item.Total}\n\n";
          }
        }
        else
        {
          content += "No items found in this invoice.\n";
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return File(bytes, "text/plain", $"Invoice-{invoice.InvoiceNumber}.txt");
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    // GET: Invoices/Details/5
    public async Task<IActionResult> Details(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      try
      {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
          return NotFound();
        }

        return View(invoice);
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"Error retrieving invoice: {ex.Message}";
        return RedirectToAction("Index", "Home");
      }
    }

    // GET: Invoices/Edit/5
    public async Task<IActionResult> Edit(string id)
    {
      if (string.IsNullOrEmpty(id))
      {
        return NotFound();
      }

      try
      {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null)
        {
          return NotFound();
        }

        return View(invoice);
      }
      catch (Exception ex)
      {
        TempData["ErrorMessage"] = $"Error retrieving invoice: {ex.Message}";
        return RedirectToAction("Index", "Home");
      }
    }

    // POST: Invoices/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Invoice invoice)
    {
      if (id != invoice.InvoiceId)
      {
        return NotFound();
      }

      if (ModelState.IsValid)
      {
        try
        {
          // Update timestamp
          invoice.UpdatedAt = Google.Cloud.Firestore.Timestamp.FromDateTime(DateTime.UtcNow);

          await _invoiceService.UpdateAsync(id, invoice);
          TempData["SuccessMessage"] = "Invoice updated successfully";
          return RedirectToAction(nameof(Details), new { id = invoice.InvoiceId });
        }
        catch (Exception ex)
        {
          TempData["ErrorMessage"] = $"Error updating invoice: {ex.Message}";
        }
      }

      return View(invoice);
    }

    public IActionResult Add() => View();
    public IActionResult List() => View();
    public IActionResult Edit() => View();
    public IActionResult Preview() => View();
    public IActionResult Print() => View();
  }
}
