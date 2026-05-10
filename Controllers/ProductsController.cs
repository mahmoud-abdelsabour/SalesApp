using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Filters;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;

namespace WebApplication2.Controllers
{
    [CustomAuthorize]
    public class ProductsController : Controller
    {
        private AppDbContext db = new AppDbContext();

        private IEnumerable<System.Web.Mvc.SelectListItem> GetCategoryList(int? selectedId = null)
        {
            return db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.NameEn)
                .ToList()
                .Select(c => new System.Web.Mvc.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.NameEn,
                    Selected = selectedId.HasValue && c.Id == selectedId.Value
                });
        }

        // GET: Products
        public ActionResult Index(int? categoryId, int page = 1)
        {
            int pageSize = 15;

            var query = db.Products.Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var projected = query
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    NameEn = p.NameEn,
                    NameAr = p.NameAr,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category.NameEn,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                    Description = p.Description,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt
                })
                .OrderBy(p => p.NameEn);

            int totalRecords = projected.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var products = projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.CategoryId = categoryId;
            ViewBag.Categories = GetCategoryList(categoryId);
            ViewBag.RouteValues = new { categoryId };

            return View(products);
        }

        // GET: Products/Create
        public ActionResult Create()
        {
            var model = new ProductViewModel
            {
                Categories = GetCategoryList()
            };
            return View(model);
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = GetCategoryList(model.CategoryId);
                return View(model);
            }

            // Check name uniqueness within category
            bool exists = db.Products.Any(p =>
                p.NameEn.ToLower() == model.NameEn.ToLower() &&
                p.CategoryId == model.CategoryId &&
                p.IsActive);
            if (exists)
            {
                ModelState.AddModelError("NameEn", "A product with this name already exists in this category.");
                model.Categories = GetCategoryList(model.CategoryId);
                return View(model);
            }

            var product = new Product
            {
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                CategoryId = model.CategoryId,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Products.Add(product);
            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // GET: Products/Edit/5
        public ActionResult Edit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null || !product.IsActive)
                return HttpNotFound();

            var model = new ProductViewModel
            {
                Id = product.Id,
                NameEn = product.NameEn,
                NameAr = product.NameAr,
                CategoryId = product.CategoryId,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                IsActive = product.IsActive,
                Categories = GetCategoryList(product.CategoryId)
            };

            return View(model);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, ProductViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Categories = GetCategoryList(model.CategoryId);
                return View(model);
            }

            // Check name uniqueness excluding self
            bool exists = db.Products.Any(p =>
                p.NameEn.ToLower() == model.NameEn.ToLower() &&
                p.CategoryId == model.CategoryId &&
                p.IsActive && p.Id != id);
            if (exists)
            {
                ModelState.AddModelError("NameEn", "A product with this name already exists in this category.");
                model.Categories = GetCategoryList(model.CategoryId);
                return View(model);
            }

            var product = db.Products.Find(id);
            if (product == null || !product.IsActive)
                return HttpNotFound();

            product.NameEn = model.NameEn;
            product.NameAr = model.NameAr;
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.Description = model.Description;

            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // POST: Products/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            var product = db.Products.Find(id);
            if (product == null || !product.IsActive)
                return Json(new { success = false, message = "Product not found." });

            bool usedInInvoice = db.InvoiceItems.Any(i => i.ProductId == id);
            if (usedInInvoice)
            {
                // Soft delete only
                product.IsActive = false;
                db.SaveChanges();
                return Json(new { success = true, message = "Product deactivated (it has invoice history)." });
            }

            // Hard delete
            db.Products.Remove(product);
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