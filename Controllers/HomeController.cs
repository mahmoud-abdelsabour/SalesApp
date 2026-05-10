using System.Linq;
using System.Web.Mvc;
using WebApplication2.Filters;
using WebApplication2.Models.Data;

namespace WebApplication2.Controllers
{
    [CustomAuthorize]
    public class HomeController : Controller
    {
        private AppDbContext db = new AppDbContext();

        public ActionResult Index()
        {
            ViewBag.TotalCategories = db.Categories.Count(c => c.IsActive);
            ViewBag.TotalProducts = db.Products.Count(p => p.IsActive);
            ViewBag.TotalCustomers = db.Customers.Count(c => c.IsActive);
            ViewBag.TotalInvoices = db.Invoices.Count();
            ViewBag.TotalRevenue = db.Invoices
                .Where(i => i.Status == "Confirmed")
                .Sum(i => (decimal?)i.TotalAmount) ?? 0;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}