using System.Web.Mvc;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class HomeController : AppController
    {
        private readonly DashboardService dashboard;

        public HomeController()
        {
            dashboard = new DashboardService(Db);
        }

        public ActionResult Index()
        {
            var summary = dashboard.GetSummary();

            ViewBag.TotalCategories = summary.Categories;
            ViewBag.TotalProducts = summary.Products;
            ViewBag.TotalCustomers = summary.Customers;
            ViewBag.TotalInvoices = summary.Invoices;
            ViewBag.TotalRevenue = summary.Revenue;

            return View();
        }

        // AJAX: Sales over last 30 days
        public JsonResult GetSalesChart()
        {
            return ChartJson(dashboard.GetSalesChart());
        }

        // AJAX: Top 5 products by quantity sold
        public JsonResult GetTopProducts()
        {
            return ChartJson(dashboard.GetTopProducts());
        }

        // AJAX: Revenue by category
        public JsonResult GetRevenueByCategory()
        {
            return ChartJson(dashboard.GetRevenueByCategory());
        }

        private JsonResult ChartJson(ChartSeries series)
        {
            return Json(new { labels = series.Labels, values = series.Values }, JsonRequestBehavior.AllowGet);
        }
    }
}
