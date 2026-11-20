using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class CustomersController : AppController
    {
        private readonly CustomerService customers;

        public CustomersController()
        {
            customers = new CustomerService(Db);
        }

        // GET: Customers
        public ActionResult Index(string search, int page = 1)
        {
            var paged = customers.GetPage(search, page);
            SetPaging(paged, new { search });
            ViewBag.Search = search;
            return View(paged.Items);
        }

        // GET: Customers/Export  (Excel, all rows)
        public ActionResult Export(string search)
        {
            return ExcelFile(customers.BuildTable(search), "Customers");
        }

        // GET: Customers/ExportPdf  (printable list; use the browser's "Save as PDF")
        public ActionResult ExportPdf(string search)
        {
            return PrintableReport(customers.BuildTable(search));
        }

        // GET: Customers/Create
        public ActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = customers.Create(model);
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // GET: Customers/Edit/5
        public ActionResult Edit(int id)
        {
            var model = customers.Get(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = customers.Update(id, model);
            if (result.IsNotFound)
                return HttpNotFound();
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            return JsonOutcome(customers.Delete(id));
        }

        // GET: Customers/Search?term=xxx (AJAX for the invoice customer picker)
        public JsonResult Search(string term)
        {
            var items = customers.Search(term).Select(c => new { id = c.Id, text = c.Text });
            return Json(items, JsonRequestBehavior.AllowGet);
        }
    }
}
