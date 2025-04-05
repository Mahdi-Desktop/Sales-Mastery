using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using System.Threading.Tasks;
using System.IO;

namespace AspnetCoreMvcFull.Controllers
{
  public class UploadController : Controller
  {
    private readonly FirebaseStorageService _storageService;

    public UploadController(FirebaseStorageService storageService)
    {
      _storageService = storageService;
    }

    [HttpPost]
    public async Task<IActionResult> Index(IFormFile file)
    {
      if (file == null || file.Length == 0)
        return BadRequest(new { error = "No file uploaded" });

      try
      {
        using (var stream = file.OpenReadStream())
        {
          // Upload to Firebase Storage
          string fileUrl = await _storageService.UploadFileAsync(stream, file.FileName);

          // Return the URL for the client
          return Ok(new { path = fileUrl });
        }
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }

    [HttpDelete]
    public async Task<IActionResult> Index(string filename)
    {
      if (string.IsNullOrEmpty(filename))
        return BadRequest(new { error = "No filename provided" });

      try
      {
        // Delete from Firebase Storage
        await _storageService.DeleteFileAsync(filename);
        return Ok(new { success = true });
      }
      catch (Exception ex)
      {
        return StatusCode(500, new { error = ex.Message });
      }
    }
  }
}
