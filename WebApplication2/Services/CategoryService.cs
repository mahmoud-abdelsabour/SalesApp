using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services.Common;

namespace WebApplication2.Services
{
    public class CategoryService
    {
        private const int PageSize = 10;

        private readonly AppDbContext db;

        public CategoryService(AppDbContext db)
        {
            this.db = db;
        }

        public PagedResult<CategoryViewModel> GetPage(int page)
        {
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

            return PagedResult<CategoryViewModel>.Create(query, page, PageSize);
        }

        /// <summary>The category for editing, or null when it doesn't exist / was deleted.</summary>
        public CategoryViewModel Get(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null || !category.IsActive)
                return null;

            return new CategoryViewModel
            {
                Id = category.Id,
                NameEn = category.NameEn,
                NameAr = category.NameAr,
                Description = category.Description,
                IsActive = category.IsActive
            };
        }

        public ServiceResult Create(CategoryViewModel model)
        {
            if (NameExists(model.NameEn, null))
                return ServiceResult.Fail("A category with this English name already exists.", "NameEn");

            db.Categories.Add(new Category
            {
                NameEn = model.NameEn,
                NameAr = model.NameAr,
                Description = model.Description,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        public ServiceResult Update(int id, CategoryViewModel model)
        {
            if (NameExists(model.NameEn, id))
                return ServiceResult.Fail("A category with this English name already exists.", "NameEn");

            var category = db.Categories.Find(id);
            if (category == null || !category.IsActive)
                return ServiceResult.NotFound();

            category.NameEn = model.NameEn;
            category.NameAr = model.NameAr;
            category.Description = model.Description;
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        /// <summary>Soft delete; refused while the category still has active products.</summary>
        public ServiceResult Delete(int id)
        {
            var category = db.Categories.Find(id);
            if (category == null || !category.IsActive)
                return ServiceResult.Fail("Category not found.");

            bool hasProducts = db.Products.Any(p => p.CategoryId == id && p.IsActive);
            if (hasProducts)
                return ServiceResult.Fail("Cannot delete a category that has active products.");

            category.IsActive = false;
            db.SaveChanges();

            return ServiceResult.Ok(Resources.Labels.DeleteSuccess);
        }

        /// <summary>Active categories for drop-downs, with one optionally pre-selected.</summary>
        public IEnumerable<SelectListItem> GetSelectList(int? selectedId = null)
        {
            return db.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.NameEn)
                .ToList()
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.NameEn,
                    Selected = selectedId.HasValue && c.Id == selectedId.Value
                });
        }

        private bool NameExists(string nameEn, int? excludeId)
        {
            return db.Categories.Any(c =>
                c.NameEn.ToLower() == nameEn.ToLower() &&
                c.IsActive &&
                (!excludeId.HasValue || c.Id != excludeId.Value));
        }
    }
}
