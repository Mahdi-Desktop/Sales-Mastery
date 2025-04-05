/*using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class FirebaseAuthController : Controller
  {
    private readonly FirestoreService _firestoreService;

    public FirebaseAuthController(FirestoreService firestoreService)
    {
      _firestoreService = firestoreService;
    }

    public IActionResult Index()
    {
      return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create()
    {
      var docId = await _firestoreService.AddTestDocument();
      return RedirectToAction("Details", new { id = docId });
    }

    public async Task<IActionResult> Details(string id)
    {
      var data = await _firestoreService.GetTestDocument(id);
      return View(data);
    }
  }
}
*/
