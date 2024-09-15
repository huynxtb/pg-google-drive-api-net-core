using Microsoft.AspNetCore.Mvc;

namespace GoogleDriveAPI.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}