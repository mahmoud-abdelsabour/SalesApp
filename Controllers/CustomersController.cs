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
    public class CustomersController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Customers
        public ActionResult Index(string search)
        {
            var query = db.Customers.Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c =>
                    c.FullName.Contains(search) ||
                    c.Phone.Contains(search));

            var customers = query
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
                .OrderBy(c => c.FullName)
                .ToList();

            ViewBag.Search = search;
            return View(customers);
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