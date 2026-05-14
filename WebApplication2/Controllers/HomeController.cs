using System;
using System.Collections.Generic;
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

        // AJAX: Sales over last 30 days
        public JsonResult GetSalesChart()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var data = db.Invoices
                .Where(i => i.Status == "Confirmed" && i.InvoiceDate >= thirtyDaysAgo)
                .GroupBy(i => System.Data.Entity.DbFunctions.TruncateTime(i.InvoiceDate))
                .Select(g => new
                {
                    date = g.Key,
                    total = g.Sum(i => i.TotalAmount)
                })
                .OrderBy(g => g.date)
                .ToList();

            var labels = data.Select(d => d.date.Value.ToString("MM/dd")).ToList();
            var values = data.Select(d => d.total).ToList();

            return Json(new { labels, values }, JsonRequestBehavior.AllowGet);
        }

        // AJAX: Top 5 products by quantity sold
        public JsonResult GetTopProducts()
        {
            var data = db.InvoiceItems
                .GroupBy(i => i.ProductName)
                .Select(g => new
                {
                    name = g.Key,
                    qty = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(g => g.qty)
                .Take(5)
                .ToList();

            var labels = data.Select(d => d.name).ToList();
            var values = data.Select(d => d.qty).ToList();

            return Json(new { labels, values }, JsonRequestBehavior.AllowGet);
        }

        // AJAX: Revenue by category
        public JsonResult GetRevenueByCategory()
        {
            var data = db.InvoiceItems
                .GroupBy(i => i.Product.Category.NameEn)
                .Select(g => new
                {
                    category = g.Key,
                    revenue = g.Sum(i => i.LineTotal)
                })
                .OrderByDescending(g => g.revenue)
                .ToList();

            var labels = data.Select(d => d.category).ToList();
            var values = data.Select(d => d.revenue).ToList();

            return Json(new { labels, values }, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}