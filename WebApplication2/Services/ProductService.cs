using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services.Common;

namespace WebApplication2.Services
{
    public class ProductService
    {
        private const int PageSize = 15;
        private const string DuplicateNameError = "A product with this name already exists in this category.";

        private readonly AppDbContext db;
        private readonly CategoryService categories;

        public ProductService(AppDbContext db, CategoryService categories)
        {
            this.db = db;
            this.categories = categories;
        }

        public PagedResult<ProductViewModel> GetPage(int? categoryId, int page)
        {
            var query = Active(categoryId)
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

            return PagedResult<ProductViewModel>.Create(query, page, PageSize);
        }

        /// <summary>Category drop-down for the list filter.</summary>
        public IEnumerable<SelectListItem> GetCategoryList(int? selectedId)
        {
            return categories.GetSelectList(selectedId);
        }

        /// <summary>Empty create form with its category drop-down.</summary>
        public ProductViewModel NewForm()
        {
            var model = new ProductViewModel();
            FillCategories(model);
            return model;
        }

        /// <summary>The product for editing (with categories), or null when it doesn't exist / was deleted.</summary>
        public ProductViewModel GetForEdit(int id)
        {
            var product = db.Products.Find(id);
            if (product == null || !product.IsActive)
                return null;

            var model = new ProductViewModel
            {
                Id = product.Id,
                NameEn = product.NameEn,
                NameAr = product.NameAr,
                CategoryId = product.CategoryId,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                Description = product.Description,
                IsActive = product.IsActive
            };
            FillCategories(model);
            return model;
        }

        /// <summary>Re-fills the category drop-down when a form is shown again after an error.</summary>
        public void FillCategories(ProductViewModel model)
        {
            model.Categories = categories.GetSelectList(model.CategoryId);
        }

        public ServiceResult Create(ProductViewModel model)
        {
            if (NameExistsInCategory(model, null))
                return ServiceResult.Fail(DuplicateNameError, "NameEn");

            db.Products.Add(new Product
            {
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                CategoryId = model.CategoryId,
                Price = model.Price,
                StockQuantity = model.StockQuantity,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        public ServiceResult Update(int id, ProductViewModel model)
        {
            if (NameExistsInCategory(model, id))
                return ServiceResult.Fail(DuplicateNameError, "NameEn");

            var product = db.Products.Find(id);
            if (product == null || !product.IsActive)
                return ServiceResult.NotFound();

            product.NameEn = model.NameEn;
            product.NameAr = model.NameAr;
            product.CategoryId = model.CategoryId;
            product.Price = model.Price;
            product.StockQuantity = model.StockQuantity;
            product.Description = model.Description;
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        /// <summary>Products on invoices are only deactivated (history is kept); others are removed.</summary>
        public ServiceResult Delete(int id)
        {
            var product = db.Products.Find(id);
            if (product == null || !product.IsActive)
                return ServiceResult.Fail("Product not found.");

            if (db.InvoiceItems.Any(i => i.ProductId == id))
            {
                product.IsActive = false;
                db.SaveChanges();
                return ServiceResult.Ok("Product deactivated (it has invoice history).");
            }

            db.Products.Remove(product);
            db.SaveChanges();
            return ServiceResult.Ok(Resources.Labels.DeleteSuccess);
        }

        /// <summary>Every matching product, for the Excel / PDF exports (no paging).</summary>
        public TableReport BuildTable(int? categoryId)
        {
            var rows = Active(categoryId)
                .OrderBy(p => p.NameEn)
                .Select(p => new
                {
                    p.NameEn,
                    p.NameAr,
                    Category = p.Category.NameEn,
                    p.Price,
                    p.StockQuantity,
                    p.Description,
                    p.CreatedAt
                })
                .ToList();

            return new TableReport
            {
                Title = Resources.Labels.Products,
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(Resources.Labels.ProductNameEn),
                    new ExcelColumn(Resources.Labels.ProductNameAr),
                    new ExcelColumn(Resources.Labels.ProductCategory),
                    new ExcelColumn(Resources.Labels.ProductPrice, ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(Resources.Labels.ProductStock),
                    new ExcelColumn(Resources.Labels.ProductDescription),
                    new ExcelColumn("Created", ExcelExportHelper.DateFormat)
                },
                Rows = rows.Select(r => new object[]
                {
                    r.NameEn, r.NameAr, r.Category, r.Price, r.StockQuantity, r.Description, r.CreatedAt
                }).ToList()
            };
        }

        // Active products, optionally in one category (shared by the list and the exports)
        private IQueryable<Product> Active(int? categoryId)
        {
            var query = db.Products.Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            return query;
        }

        private bool NameExistsInCategory(ProductViewModel model, int? excludeId)
        {
            return db.Products.Any(p =>
                p.NameEn.ToLower() == model.NameEn.ToLower() &&
                p.CategoryId == model.CategoryId &&
                p.IsActive &&
                (!excludeId.HasValue || p.Id != excludeId.Value));
        }
    }
}
