using System.Web.Mvc;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class ReportsController : AppController
    {
        private readonly ReportService reports;

        public ReportsController()
        {
            reports = new ReportService(Db);
        }

        // GET: Reports  (overview: summary cards + a card per report)
        public ActionResult Index(string from = null, string to = null)
        {
            return View(reports.GetOverview(from, to));
        }

        // GET: Reports/ByDate?from=..&to=..&page=1
        public ActionResult ByDate(string from = null, string to = null, int page = 1)
        {
            return View("ReportPage", reports.GetByDate(from, to, page));
        }

        // GET: Reports/ByPeriod?granularity=week|month|year&from=..&to=..&page=1
        public ActionResult ByPeriod(string granularity = "month", string from = null, string to = null, int page = 1)
        {
            return View("PeriodReport", reports.GetByPeriod(granularity, from, to, page));
        }

        // GET: Reports/ExportPeriod  (Excel, all periods)
        public ActionResult ExportPeriod(string granularity = "month", string from = null, string to = null)
        {
            return ExcelFile(reports.BuildPeriodTable(granularity, from, to), "Report_period");
        }

        // GET: Reports/ExportPeriodPdf  (printable, all periods; use the browser's "Save as PDF")
        public ActionResult ExportPeriodPdf(string granularity = "month", string from = null, string to = null)
        {
            return PrintableReport(reports.BuildPeriodTable(granularity, from, to));
        }

        // GET: Reports/ByCustomer?from=..&to=..&page=1
        public ActionResult ByCustomer(string from = null, string to = null, int page = 1)
        {
            return View("ReportPage", reports.GetByCustomer(from, to, page));
        }

        // GET: Reports/ByProduct?from=..&to=..&page=1
        public ActionResult ByProduct(string from = null, string to = null, int page = 1)
        {
            return View("ReportPage", reports.GetByProduct(from, to, page));
        }

        // GET: Reports/ByCategory?from=..&to=..&page=1
        public ActionResult ByCategory(string from = null, string to = null, int page = 1)
        {
            return View("ReportPage", reports.GetByCategory(from, to, page));
        }

        // GET: Reports/Inventory?threshold=10&categoryId=..&lowOnly=true&page=1
        public ActionResult Inventory(int threshold = ReportService.DefaultLowStockThreshold, int? categoryId = null, bool lowOnly = false, int page = 1)
        {
            return View("InventoryReport", reports.GetInventory(threshold, categoryId, lowOnly, page));
        }

        // GET: Reports/ExportInventory  (Excel, all rows)
        public ActionResult ExportInventory(int threshold = ReportService.DefaultLowStockThreshold, int? categoryId = null, bool lowOnly = false)
        {
            return ExcelFile(reports.BuildInventoryTable(threshold, categoryId, lowOnly), "Report_inventory");
        }

        // GET: Reports/ExportInventoryPdf  (printable, all rows; use the browser's "Save as PDF")
        public ActionResult ExportInventoryPdf(int threshold = ReportService.DefaultLowStockThreshold, int? categoryId = null, bool lowOnly = false)
        {
            return PrintableReport(reports.BuildInventoryTable(threshold, categoryId, lowOnly));
        }

        // GET: Reports/Export?type=date|customer|product|category&from=..&to=..   (Excel, all rows)
        public ActionResult Export(string type = "date", string from = null, string to = null)
        {
            return ExcelFile(reports.BuildTable(type, from, to), "Report_" + (type ?? "date"));
        }

        // GET: Reports/ExportPdf?type=..  (printable, all rows; use the browser's "Save as PDF")
        public ActionResult ExportPdf(string type = "date", string from = null, string to = null)
        {
            return PrintableReport(reports.BuildTable(type, from, to));
        }
    }
}
