using System;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Filters;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;

namespace WebApplication2.Controllers
{
    [CustomAuthorize]
    public class CategoriesController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Categories
        public ActionResult Index(int page = 1)
        {
            int pageSize = 10;

            var query = db.Categories
                .Where(c => c.IsActive)
                .Select(c => new CategoryViewModel
                {
                    Id = c.Id,
                    NameEn = c.NameEn,
                    NameAr = c.NameAr,
                    Description = c.Description,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    ProductCount = c.Products.Count(p => p.IsActive)
                })
                .OrderBy(c => c.NameEn);

            int totalRecords = query.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var categories = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.RouteValues = new { };

            return View(categories);
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

            // Check name uniqueness
            bool exists = db.Categories.Any(c =>
                c.NameEn.ToLower() == model.NameEn.ToLower() && c.IsActive);
            if (exists)
            {
                ModelState.AddModelError("NameEn", "A category with this English name already exists.");
                return View(model);
            }

            var category = new Category
            {
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Categories.Add(category);
            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // GET: Categories/Edit/5
        public ActionResult Edit(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null || !category.IsActive)
                return HttpNotFound();

            var model = new CategoryViewModel
            {
                Id = category.Id,
                NameEn = category.NameEn,
                NameAr = category.NameAr,
                Description = category.Description,
                IsActive = category.IsActive
            };

            return View(model);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CategoryViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check name uniqueness excluding self
            bool exists = db.Categories.Any(c =>
                c.NameEn.ToLower() == model.NameEn.ToLower() &&
                c.IsActive && c.Id != id);
            if (exists)
            {
                ModelState.AddModelError("NameEn", "A category with this English name already exists.");
                return View(model);
            }

            var category = db.Categories.Find(id);
            if (category == null || !category.IsActive)
                return HttpNotFound();

            category.NameEn = model.NameEn;
            category.NameAr = model.NameAr;
            category.Description = model.Description;

            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // POST: Categories/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null || !category.IsActive)
                return Json(new { success = false, message = "Category not found." });

            bool hasProducts = db.Products.Any(p => p.CategoryId == id && p.IsActive);
            if (hasProducts)
                return Json(new { success = false, message = "Cannot delete a category that has active products." });

            category.IsActive = false;
            db.SaveChanges();

            return Json(new { success = true, message = Resources.Labels.DeleteSuccess });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}