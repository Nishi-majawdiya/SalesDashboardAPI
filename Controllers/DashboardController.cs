using Microsoft.AspNetCore.Mvc;

namespace SalesDashboardAPI.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}