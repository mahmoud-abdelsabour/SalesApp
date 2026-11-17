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
    public class InvoicesController : Controller
    {
        private AppDbContext db = new AppDbContext();

        // GET: Invoices
        public ActionResult Index(int page = 1, string dateFrom = null, string dateTo = null, int? customerId = null, string status = null)
        {
            int pageSize = 20;

            var query = db.Invoices.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(dateFrom))
            {
                DateTime from;
                if (DateTime.TryParse(dateFrom, out from))
                    query = query.Where(i => i.InvoiceDate >= from);
            }

            if (!string.IsNullOrEmpty(dateTo))
            {
                DateTime to;
                if (DateTime.TryParse(dateTo, out to))
                {
                    DateTime toEndOfDay = to.AddDays(1);
                    query = query.Where(i => i.InvoiceDate <= toEndOfDay);
                }
            }

            if (customerId.HasValue)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);

            var projected = query
                .Select(i => new InvoiceListViewModel
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerName = i.Customer.FullName,
                    InvoiceDate = i.InvoiceDate,
                    TotalAmount = i.TotalAmount,
                    Status = i.Status
                })
                .OrderByDescending(i => i.InvoiceDate);

            int totalRecords = projected.Count();
            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var invoices = projected
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.RouteValues = new { dateFrom, dateTo, customerId, status };

            // Keep filter values for form
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.CustomerId = customerId;
            ViewBag.Status = status;

            return View(invoices);
        }

        // GET: Invoices/Create
        public ActionResult Create()
        {
            ViewBag.Categories = db.Categories.Where(c => c.IsActive).OrderBy(c => c.NameEn).ToList();
            return View(new InvoiceCreateViewModel());
        }

        // POST: Invoices/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(InvoiceCreateViewModel model)
        {
            // Null safety
            if (model == null)
                model = new InvoiceCreateViewModel();

            if (model.Items == null)
                model.Items = new List<InvoiceItemViewModel>();

            // Re-populate categories for view in case of error
            ViewBag.Categories = db.Categories.Where(c => c.IsActive).OrderBy(c => c.NameEn).ToList();

            // Validate customer
            if (model.CustomerId == 0)
            {
                ModelState.AddModelError("", "Please select a customer.");
                return View(model);
            }

            // Server-side validation
            if (model.Items.Count == 0)
            {
                ModelState.AddModelError("", Resources.Labels.InvoiceNoItems);
                return View(model);
            }

            foreach (var item in model.Items)
            {
                if (item.Quantity <= 0)
                {
                    ModelState.AddModelError("", Resources.Labels.InvoiceInvalidQty);
                    return View(model);
                }
                if (item.DiscountPct < 0 || item.DiscountPct > 100)
                {
                    ModelState.AddModelError("", Resources.Labels.InvoiceInvalidDiscount);
                    return View(model);
                }
            }

            if (!ModelState.IsValid)
                return View(model);

            // Get logged in user id
            var username = User.Identity.Name;
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Build invoice header
                    var invoice = new Invoice
                    {
                        InvoiceNumber = InvoiceNumberHelper.Generate(db),
                        CustomerId = model.CustomerId,
                        InvoiceDate = DateTime.Now,
                        InvoiceDiscountPct = model.InvoiceDiscountPct,
                        Notes = model.Notes,
                        CreatedBy = user.Id,
                        CreatedAt = DateTime.Now,
                        Status = "Confirmed"
                    };

                    // Calculate line items
                    decimal subTotal = 0;
                    var invoiceItems = new List<InvoiceItem>();

                    // Total requested per product (the same product may appear on several lines)
                    var requestedByProduct = model.Items
                        .GroupBy(i => i.ProductId)
                        .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

                    foreach (var item in model.Items)
                    {
                        var product = db.Products.Find(item.ProductId);
                        if (product == null || !product.IsActive)
                        {
                            ModelState.AddModelError("", "Product not found: " + item.ProductId);
                            ViewBag.Categories = db.Categories.Where(c => c.IsActive).OrderBy(c => c.NameEn).ToList();
                            return View(model);
                        }

                        // Stock validation
                        if (item.Quantity <= 0)
                        {
                            ModelState.AddModelError("", Resources.Labels.InvoiceInvalidQty);
                            ViewBag.Categories = db.Categories.Where(c => c.IsActive).OrderBy(c => c.NameEn).ToList();
                            return View(model);
                        }

                        if (requestedByProduct[item.ProductId] > product.StockQuantity)
                        {
                            ModelState.AddModelError("", product.NameEn + ": " + Resources.Labels.InvoiceExceedsStock +
                                " (Available: " + product.StockQuantity + ")");
                            ViewBag.Categories = db.Categories.Where(c => c.IsActive).OrderBy(c => c.NameEn).ToList();
                            return View(model);
                        }

                        decimal discountAmt = product.Price * item.Quantity * item.DiscountPct / 100;
                        decimal lineTotal = (product.Price * item.Quantity) - discountAmt;

                        invoiceItems.Add(new InvoiceItem
                        {
                            ProductId = product.Id,
                            ProductName = product.NameEn,
                            UnitPrice = product.Price,
                            Quantity = item.Quantity,
                            DiscountPct = item.DiscountPct,
                            DiscountAmt = discountAmt,
                            LineTotal = lineTotal
                        });

                        subTotal += lineTotal;
                    }

                    // Invoice level discount
                    decimal invoiceDiscountAmt = subTotal * model.InvoiceDiscountPct / 100;
                    decimal totalAmount = subTotal - invoiceDiscountAmt;

                    invoice.SubTotal = subTotal;
                    invoice.InvoiceDiscountAmt = invoiceDiscountAmt;
                    invoice.TotalAmount = totalAmount;

                    db.Invoices.Add(invoice);

                    // Unique index on InvoiceNumber guards against simultaneous saves;
                    // on a collision, generate a fresh number and retry.
                    const int maxAttempts = 5;
                    for (int attempt = 1; ; attempt++)
                    {
                        try
                        {
                            db.SaveChanges();
                            break;
                        }
                        catch (System.Data.Entity.Infrastructure.DbUpdateException ex)
                            when (attempt < maxAttempts && IsDuplicateKey(ex))
                        {
                            invoice.InvoiceNumber = InvoiceNumberHelper.Generate(db);
                        }
                    }

                    // Attach invoice id to items
                    foreach (var item in invoiceItems)
                        item.InvoiceId = invoice.Id;

                    db.InvoiceItems.AddRange(invoiceItems);
                    db.SaveChanges();

                    // Deduct stock for each product
                    foreach (var item in invoiceItems)
                    {
                        var product = db.Products.Find(item.ProductId);
                        if (product != null)
                        {
                            product.StockQuantity -= item.Quantity;
                            if (product.StockQuantity < 0) product.StockQuantity = 0;
                        }
                    }
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["Success"] = Resources.Labels.InvoiceSaved;
                    return RedirectToAction("Details", new { id = invoice.Id });
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    // Another invoice changed the same product's stock at the same time
                    transaction.Rollback();
                    ModelState.AddModelError("", "Stock was changed by another transaction. Please review quantities and try again.");
                    return View(model);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    ModelState.AddModelError("", "Error saving invoice: " + ex.Message);
                    return View(model);
                }
            }
        }

        // GET: Invoices/Details/5
        public ActionResult Details(int id)
        {
            var invoice = db.Invoices
                .Where(i => i.Id == id)
                .Select(i => new InvoiceDetailsViewModel
                {
                    Id = i.Id,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerName = i.Customer.FullName,
                    CustomerPhone = i.Customer.Phone,
                    CustomerEmail = i.Customer.Email,
                    InvoiceDate = i.InvoiceDate,
                    SubTotal = i.SubTotal,
                    InvoiceDiscountPct = i.InvoiceDiscountPct,
                    InvoiceDiscountAmt = i.InvoiceDiscountAmt,
                    TotalAmount = i.TotalAmount,
                    Notes = i.Notes,
                    Status = i.Status,
                    CreatedBy = i.CreatedByUser.FullName,
                    Items = i.Items.Select(item => new InvoiceItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        Quantity = item.Quantity,
                        DiscountPct = item.DiscountPct,
                        DiscountAmt = item.DiscountAmt,
                        LineTotal = item.LineTotal
                    }).ToList()
                })
                .FirstOrDefault();

            if (invoice == null)
                return HttpNotFound();

            return View(invoice);
        }

        // GET: Invoices/GetProducts?categoryId=1
        public JsonResult GetProducts(int? categoryId)
        {
            var query = db.Products.Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            var products = query
                .Select(p => new
                {
                    id = p.Id,
                    text = p.NameEn,
                    price = p.Price,
                    stock = p.StockQuantity
                })
                .OrderBy(p => p.text)
                .ToList();

            return Json(products, JsonRequestBehavior.AllowGet);
        }

        // POST: Invoices/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Cancel(int id)
        {
            var invoice = db.Invoices.Find(id);
            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found." });

            if (invoice.Status == "Cancelled")
                return Json(new { success = false, message = "Invoice is already cancelled." });

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Return each item's quantity to stock
                    var items = db.InvoiceItems.Where(i => i.InvoiceId == id).ToList();
                    foreach (var item in items)
                    {
                        var product = db.Products.Find(item.ProductId);
                        if (product != null)
                            product.StockQuantity += item.Quantity;
                    }

                    invoice.Status = "Cancelled";
                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true, message = "Invoice cancelled successfully." });
                }
                catch (System.Data.Entity.Infrastructure.DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Stock was changed by another transaction. Please try again." });
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Error cancelling invoice." });
                }
            }
        }

        private static bool IsDuplicateKey(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                var sql = e as System.Data.SqlClient.SqlException;
                if (sql != null && (sql.Number == 2601 || sql.Number == 2627))
                    return true;
            }
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}