using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication2.Filters;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;

namespace WebApplication2.Controllers
{
    [CustomAuthorize]
    public class CustomersController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Customers
        public ActionResult Index(string search, int page = 1)
        {
            int pageSize = 15;

            var query = db.Customers.Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c =>
                    c.FullName.Contains(search) ||
                    c.Phone.Contains(search));

            var projected = query
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

            int totalRecords = projected.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var customers = projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;
            ViewBag.RouteValues = new { search };

            return View(customers);
        }

        // Table of customers (same search as Index) - shared by Excel and PDF
        private TableReport BuildCustomerTable(string search)
        {
            var query = db.Customers.Where(c => c.IsActive);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FullName.Contains(search) || c.Phone.Contains(search));

            var rows = query
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

        // GET: Customers/Export  (Excel)
        public ActionResult Export(string search)
        {
            var t = BuildCustomerTable(search);
            var bytes = ExcelExportHelper.Build(t.Title, t.Columns, t.Rows);
            return File(bytes, ExcelExportHelper.ContentType, ExcelExportHelper.FileName("Customers"));
        }

        // GET: Customers/ExportPdf  (printable list; use the browser's "Save as PDF")
        public ActionResult ExportPdf(string search)
        {
            return View("~/Views/Shared/ListReport.cshtml", BuildCustomerTable(search));
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

            // Check phone uniqueness
            bool phoneExists = db.Customers.Any(c =>
                c.Phone == model.Phone && c.IsActive);
            if (phoneExists)
            {
                ModelState.AddModelError("Phone", Resources.Labels.PhoneDuplicate);
                return View(model);
            }

            var customer = new Customer
            {
                FullName = model.FullName,
                Phone = model.Phone,
                Email = model.Email,
                Address = model.Address,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            db.Customers.Add(customer);
            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // GET: Customers/Edit/5
        public ActionResult Edit(int id)
        {
            var customer = db.Customers.Find(id);
            if (customer == null || !customer.IsActive)
                return HttpNotFound();

            var model = new CustomerViewModel
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Phone = customer.Phone,
                Email = customer.Email,
                Address = customer.Address,
                IsActive = customer.IsActive
            };

            return View(model);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CustomerViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check phone uniqueness excluding self
            bool phoneExists = db.Customers.Any(c =>
                c.Phone == model.Phone &&
                c.IsActive && c.Id != id);
            if (phoneExists)
            {
                ModelState.AddModelError("Phone", Resources.Labels.PhoneDuplicate);
                return View(model);
            }

            var customer = db.Customers.Find(id);
            if (customer == null || !customer.IsActive)
                return HttpNotFound();

            customer.FullName = model.FullName;
            customer.Phone = model.Phone;
            customer.Email = model.Email;
            customer.Address = model.Address;

            db.SaveChanges();

            TempData["Success"] = Resources.Labels.SaveSuccess;
            return RedirectToAction("Index");
        }

        // POST: Customers/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(int id)
        {
            var customer = db.Customers.Find(id);
            if (customer == null || !customer.IsActive)
                return Json(new { success = false, message = "Customer not found." });

            bool hasInvoices = db.Invoices.Any(i => i.CustomerId == id);
            if (hasInvoices)
            {
                customer.IsActive = false;
                db.SaveChanges();
                return Json(new { success = true, message = "Customer deactivated (has invoice history)." });
            }

            db.Customers.Remove(customer);
            db.SaveChanges();

            return Json(new { success = true, message = Resources.Labels.DeleteSuccess });
        }

        // GET: Customers/Search?term=xxx (AJAX for invoice autocomplete)
        public JsonResult Search(string term)
        {
            var customers = db.Customers
                .Where(c => c.IsActive &&
                    (c.FullName.Contains(term) || c.Phone.Contains(term)))
                .Select(c => new
                {
                    id = c.Id,
                    text = c.FullName + " - " + c.Phone
                })
                .Take(10)
                .ToList();

            return Json(customers, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}