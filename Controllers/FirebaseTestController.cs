/*using Microsoft.AspNetCore.Mvc;
using AspnetCoreMvcFull.Services;
using System.Threading.Tasks;

namespace AspnetCoreMvcFull.Controllers
{
  public class FirebaseTestController : Controller
  {
    private readonly FirestoreService _firestoreService;

    public FirebaseTestController(FirestoreService firestoreService)
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
      ViewBag.DocumentId = docId;
      return View("Success");
    }

    public async Task<IActionResult> Details(string id)
    {
      var data = await _firestoreService.GetTestDocument(id);
      return View(data);
    }
  }
}
*/
