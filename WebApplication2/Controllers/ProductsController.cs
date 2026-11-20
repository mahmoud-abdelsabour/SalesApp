using System.Web.Mvc;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class ProductsController : AppController
    {
        private readonly ProductService products;

        public ProductsController()
        {
            products = new ProductService(Db, new CategoryService(Db));
        }

        // GET: Products
        public ActionResult Index(int? categoryId, int page = 1)
        {
            var paged = products.GetPage(categoryId, page);
            SetPaging(paged, new { categoryId });
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = products.GetCategoryList(categoryId);
            return View(paged.Items);
        }

        // GET: Products/Export  (Excel, all rows)
        public ActionResult Export(int? categoryId)
        {
            return ExcelFile(products.BuildTable(categoryId), "Products");
        }

        // GET: Products/ExportPdf  (printable list; use the browser's "Save as PDF")
        public ActionResult ExportPdf(int? categoryId)
        {
            return PrintableReport(products.BuildTable(categoryId));
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            return View(products.NewForm());
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                products.FillCategories(model);
                return View(model);
            }

            var result = products.Create(model);
            if (!result.Success)
            {
                AddModelError(result);
                products.FillCategories(model);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int id)
        {
            var model = products.GetForEdit(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                products.FillCategories(model);
                return View(model);
            }

            var result = products.Update(id, model);
            if (result.IsNotFound)
                return HttpNotFound();
            if (!result.Success)
            {
                AddModelError(result);
                products.FillCategories(model);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // POST: Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            return JsonOutcome(products.Delete(id));
        }
    }
}
