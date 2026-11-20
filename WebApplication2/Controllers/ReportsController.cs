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

        // GET: Reports/Export?type=date|customer|product&from=..&to=..   (Excel, all rows)
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
