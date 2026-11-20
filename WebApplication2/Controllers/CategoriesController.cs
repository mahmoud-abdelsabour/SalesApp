using System.Web.Mvc;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services;

namespace WebApplication2.Controllers
{
    public class CategoriesController : AppController
    {
        private readonly CategoryService categories;

        public CategoriesController()
        {
            categories = new CategoryService(Db);
        }

        // GET: Categories
        public ActionResult Index(int page = 1)
        {
            var paged = categories.GetPage(page);
            SetPaging(paged, new { });
            return View(paged.Items);
        }

        // GET: Categories/Create
        public ActionResult Create()
        {
            return View(new CategoryViewModel());
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = categories.Create(model);
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // GET: Categories/Edit/5
        public ActionResult Edit(int id)
        {
            var model = categories.Get(id);
            if (model == null)
                return HttpNotFound();

            return View(model);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = categories.Update(id, model);
            if (result.IsNotFound)
                return HttpNotFound();
            if (!result.Success)
            {
                AddModelError(result);
                return View(model);
            }

            return RedirectWithSuccess();
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            return JsonOutcome(categories.Delete(id));
        }
    }
}
