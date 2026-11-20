using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services.Common;

namespace WebApplication2.Services
{
    /// <summary>One entry of an autocomplete / drop-down lookup.</summary>
    public class LookupItem
    {
        public int Id { get; set; }
        public string Text { get; set; }
    }

    public class CustomerService
    {
        private const int PageSize = 15;

        private readonly AppDbContext db;

        public CustomerService(AppDbContext db)
        {
            this.db = db;
        }

        public PagedResult<CustomerViewModel> GetPage(string search, int page)
        {
            var query = Active(search)
                .Select(c => new CustomerViewModel
                {
                    Id = c.Id,
                    FullName = c.FullName,
                    Phone = c.Phone,
                    Email = c.Email,
                    Address = c.Address,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    InvoiceCount = c.Invoices.Count()
                })
                .OrderBy(c => c.FullName);

            return PagedResult<CustomerViewModel>.Create(query, page, PageSize);
        }

        /// <summary>The customer for editing, or null when it doesn't exist / was deleted.</summary>
        public CustomerViewModel Get(int id)
        {
            var customer = db.Customers.Find(id);
            if (customer == null || !customer.IsActive)
                return null;

            return new CustomerViewModel
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                IsActive = customer.IsActive
            };
        }

        public ServiceResult Create(CustomerViewModel model)
        {
            if (PhoneExists(model.Phone, null))
                return ServiceResult.Fail(Resources.Labels.PhoneDuplicate, "Phone");

            db.Customers.Add(new Customer
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                IsActive = true,
                CreatedAt = DateTime.Now
            });
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        public ServiceResult Update(int id, CustomerViewModel model)
        {
            if (PhoneExists(model.Phone, id))
                return ServiceResult.Fail(Resources.Labels.PhoneDuplicate, "Phone");

            var customer = db.Customers.Find(id);
            if (customer == null || !customer.IsActive)
                return ServiceResult.NotFound();

            customer.FullName = model.FullName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;
            db.SaveChanges();

            return ServiceResult.Ok();
        }

        /// <summary>Customers with invoices are only deactivated (history is kept); others are removed.</summary>
        public ServiceResult Delete(int id)
        {
            var customer = db.Customers.Find(id);
            if (customer == null || !customer.IsActive)
                return ServiceResult.Fail("Customer not found.");

            if (db.Invoices.Any(i => i.CustomerId == id))
            {
                customer.IsActive = false;
                db.SaveChanges();
                return ServiceResult.Ok("Customer deactivated (has invoice history).");
            }

            db.Customers.Remove(customer);
            db.SaveChanges();
            return ServiceResult.Ok(Resources.Labels.DeleteSuccess);
        }

        /// <summary>Top 10 matches by name or phone (invoice customer picker).</summary>
        public List<LookupItem> Search(string term)
        {
            return db.Customers
                .Where(c => c.IsActive &&
                    (c.FullName.Contains(term) || c.Phone.Contains(term)))
                .Select(c => new LookupItem
                {
                    Id = c.Id,
                    Text = c.FullName + " - " + c.Phone
                })
                .Take(10)
                .ToList();
        }

        /// <summary>Every matching customer, for the Excel / PDF exports (no paging).</summary>
        public TableReport BuildTable(string search)
        {
            var rows = Active(search)
                .OrderBy(c => c.FullName)
                .Select(c => new
                {
                    c.FullName,
                    c.Phone,
                    c.Email,
                    c.Address,
                    InvoiceCount = c.Invoices.Count(),
                    c.CreatedAt
                })
                .ToList();

            return new TableReport
            {
                Title = Resources.Labels.Customers,
                Subtitle = string.IsNullOrEmpty(search) ? null : Resources.Labels.Search + ": " + search,
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(Resources.Labels.CustomerFullName),
                    new ExcelColumn(Resources.Labels.CustomerPhone),
                    new ExcelColumn(Resources.Labels.CustomerEmail),
                    new ExcelColumn(Resources.Labels.CustomerAddress),
                    new ExcelColumn(Resources.Labels.Invoices),
                    new ExcelColumn("Created", ExcelExportHelper.DateFormat)
                },
                Rows = rows.Select(r => new object[]
                {
                    r.FullName, r.Phone, r.Email, r.Address, r.InvoiceCount, r.CreatedAt
                }).ToList()
            };
        }

        // Active customers, optionally filtered by name or phone (shared by the list and the exports)
        private IQueryable<Customer> Active(string search)
        {
            var query = db.Customers.Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FullName.Contains(search) || c.Phone.Contains(search));

            return query;
        }

        private bool PhoneExists(string phone, int? excludeId)
        {
            return db.Customers.Any(c =>
                c.Phone == phone &&
                c.IsActive &&
                (!excludeId.HasValue || c.Id != excludeId.Value));
        }
    }
}
