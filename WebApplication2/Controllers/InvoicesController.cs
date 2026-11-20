using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Helpers;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class InvoicesController : AppController
    {
        private readonly InvoiceService invoices;
        private readonly CategoryService categories;
        private readonly ProductService products;

        public InvoicesController()
        {
            invoices = new InvoiceService(Db);
            categories = new CategoryService(Db);
            products = new ProductService(Db, categories);
        }

        // GET: Invoices
        public ActionResult Index(int page = 1, string dateFrom = null, string dateTo = null, int? customerId = null, string status = null)
        {
            var paged = invoices.GetPage(page, dateFrom, dateTo, customerId, status);
            SetPaging(paged, new { dateFrom, dateTo, customerId, status });

            // Keep filter values for the form
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.CustomerId = customerId;
            ViewBag.Status = status;

            return View(paged.Items);
        }

        // GET: Invoices/Export  (Excel list, all rows)
        public ActionResult Export(string dateFrom = null, string dateTo = null, int? customerId = null, string status = null)
        {
            return ExcelFile(invoices.BuildTable(dateFrom, dateTo, customerId, status), "Invoices");
        }

        // GET: Invoices/ExportPdf  (printable list; use the browser's "Save as PDF")
        public ActionResult ExportPdf(string dateFrom = null, string dateTo = null, int? customerId = null, string status = null)
        {
            return PrintableReport(invoices.BuildTable(dateFrom, dateTo, customerId, status));
        }

        // GET: Invoices/ExportDetails/5  (Excel for a single invoice)
        public ActionResult ExportDetails(int id)
        {
            var invoice = invoices.GetDetails(id);
            if (invoice == null)
                return HttpNotFound();

            var bytes = ExcelExportHelper.BuildInvoice(invoice);
            return File(bytes, ExcelExportHelper.ContentType, ExcelExportHelper.FileName("Invoice_" + invoice.InvoiceNumber));
        }

        // GET: Invoices/Create
        public ActionResult Create()
        {
            ViewBag.Categories = categories.GetActive();
            return View(new InvoiceCreateViewModel());
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InvoiceCreateViewModel model)
        {
            if (model == null)
                model = new InvoiceCreateViewModel();

            if (model.Items == null)
                model.Items = new List<InvoiceItemViewModel>();

            // Needed whenever the form is shown again
            ViewBag.Categories = categories.GetActive();

            var validation = invoices.Validate(model);
            if (!validation.Success)
            {
                AddModelError(validation);
                return View(model);
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = invoices.Create(model, User.Identity.Name);
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            TempData["Success"] = Resources.Labels.InvoiceSaved;
            // Details page prints the invoice automatically (print=true)
            return RedirectToAction("Details", new { id = result.Value, print = true });
        }

        // GET: Invoices/Details/5
        public ActionResult Details(int id)
        {
            var invoice = invoices.GetDetails(id);
            if (invoice == null)
                return HttpNotFound();

            return View(invoice);
        }

        // GET: Invoices/Print/5  (standalone printable page; use the browser's "Save as PDF")
        public ActionResult Print(int id)
        {
            var invoice = invoices.GetDetails(id);
            if (invoice == null)
                return HttpNotFound();

            return View(invoice);
        }

        // GET: Invoices/GetProducts?categoryId=1
        public JsonResult GetProducts(int? categoryId)
        {
            var items = products.GetActiveOptions(categoryId)
                .Select(p => new { id = p.Id, text = p.Text, price = p.Price, stock = p.Stock });

            return Json(items, JsonRequestBehavior.AllowGet);
        }

        // POST: Invoices/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Cancel(int id)
        {
            return JsonOutcome(invoices.Cancel(id));
        }
    }
}
