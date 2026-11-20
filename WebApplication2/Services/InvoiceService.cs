using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;
using WebApplication2.Services.Common;

namespace WebApplication2.Services
{
    public class InvoiceService
    {
        private const int PageSize = 20;

        private readonly AppDbContext db;

        public InvoiceService(AppDbContext db)
        {
            this.db = db;
        }

        // ---------- listing ----------

        public PagedResult<InvoiceListViewModel> GetPage(int page, string dateFrom, string dateTo, int? customerId, string status)
        {
            var query = Filter(dateFrom, dateTo, customerId, status)
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

            return PagedResult<InvoiceListViewModel>.Create(query, page, PageSize);
        }

        /// <summary>Every matching invoice, for the Excel / PDF exports (no paging).</summary>
        public TableReport BuildTable(string dateFrom, string dateTo, int? customerId, string status)
        {
            var rows = Filter(dateFrom, dateTo, customerId, status)
                .OrderByDescending(i => i.InvoiceDate)
                .Select(i => new
                {
                    i.InvoiceNumber,
                    Customer = i.Customer.FullName,
                    i.InvoiceDate,
                    i.SubTotal,
                    i.InvoiceDiscountPct,
                    i.InvoiceDiscountAmt,
                    i.TotalAmount,
                    i.Status,
                    CreatedBy = i.CreatedByUser.FullName
                })
                .ToList();

            var parts = new List<string>();
            if (!string.IsNullOrEmpty(dateFrom)) parts.Add(Resources.Labels.FilterDateFrom + ": " + dateFrom);
            if (!string.IsNullOrEmpty(dateTo)) parts.Add(Resources.Labels.FilterDateTo + ": " + dateTo);
            if (!string.IsNullOrEmpty(status)) parts.Add(Resources.Labels.InvoiceStatus + ": " + status);

            return new TableReport
            {
                Title = Resources.Labels.Invoices,
                Subtitle = string.Join(" | ", parts),
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(Resources.Labels.InvoiceNumber),
                    new ExcelColumn(Resources.Labels.InvoiceCustomer),
                    new ExcelColumn(Resources.Labels.InvoiceDate, ExcelExportHelper.DateTimeFormat),
                    new ExcelColumn(Resources.Labels.InvoiceSubTotal, ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(Resources.Labels.InvoiceDiscount + " %", ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(Resources.Labels.InvoiceDiscountAmt, ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(Resources.Labels.InvoiceTotal, ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(Resources.Labels.InvoiceStatus),
                    new ExcelColumn("Created By")
                },
                Rows = rows.Select(r => new object[]
                {
                    r.InvoiceNumber, r.Customer, r.InvoiceDate, r.SubTotal, r.InvoiceDiscountPct,
                    r.InvoiceDiscountAmt, r.TotalAmount, r.Status, r.CreatedBy
                }).ToList()
            };
        }

        /// <summary>One invoice with its customer and items (details, print and Excel), or null.</summary>
        public InvoiceDetailsViewModel GetDetails(int id)
        {
            return db.Invoices
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
        }

        // ---------- creating ----------

        /// <summary>Checks the submitted form (customer, items, quantities, discounts).</summary>
        public ServiceResult Validate(InvoiceCreateViewModel model)
        {
            if (model.CustomerId == 0)
                return ServiceResult.Fail("Please select a customer.");

            if (model.Items.Count == 0)
                return ServiceResult.Fail(Resources.Labels.InvoiceNoItems);

            foreach (var item in model.Items)
            {
                if (item.Quantity <= 0)
                    return ServiceResult.Fail(Resources.Labels.InvoiceInvalidQty);

                if (item.DiscountPct < 0 || item.DiscountPct > 100)
                    return ServiceResult.Fail(Resources.Labels.InvoiceInvalidDiscount);
            }

            if (model.InvoiceDiscountPct < 0 || model.InvoiceDiscountPct > 100)
                return ServiceResult.Fail(Resources.Labels.InvoiceInvalidDiscount);

            return ServiceResult.Ok();
        }

        /// <summary>
        /// Saves a confirmed invoice and deducts stock, all in one transaction.
        /// Returns the new invoice id.
        /// </summary>
        public ServiceResult<int> Create(InvoiceCreateViewModel model, string username)
        {
            var user = db.Users.FirstOrDefault(u => u.Username == username);

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var invoice = new Invoice
                    {
                        InvoiceNumber = GenerateNumber(),
                        CustomerId = model.CustomerId,
                        InvoiceDate = DateTime.Now,
                        InvoiceDiscountPct = model.InvoiceDiscountPct,
                        Notes = model.Notes,
                        CreatedBy = user.Id,
                        CreatedAt = DateTime.Now,
                        Status = InvoiceStatus.Confirmed
                    };

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
                            return ServiceResult<int>.Fail("Product not found: " + item.ProductId);

                        if (item.Quantity <= 0)
                            return ServiceResult<int>.Fail(Resources.Labels.InvoiceInvalidQty);

                        if (requestedByProduct[item.ProductId] > product.StockQuantity)
                            return ServiceResult<int>.Fail(product.NameEn + ": " + Resources.Labels.InvoiceExceedsStock +
                                " (Available: " + product.StockQuantity + ")");

                        decimal discountAmt = product.Price * item.Quantity * item.DiscountPct / 100;
                        decimal lineTotal = (product.Price * item.Quantity) - discountAmt;

                        invoiceItems.Add(new InvoiceItem
                        {
                            ProductId = product.Id,
                            ProductName = product.NameEn,
                            UnitPrice = product.Price,
                            UnitCost = product.CostPrice,
                            Quantity = item.Quantity,
                            DiscountPct = item.DiscountPct,
                            DiscountAmt = discountAmt,
                            LineTotal = lineTotal
                        });

                        subTotal += lineTotal;
                    }

                    // Invoice level discount
                    decimal invoiceDiscountAmt = subTotal * model.InvoiceDiscountPct / 100;

                    invoice.SubTotal = subTotal;
                    invoice.InvoiceDiscountAmt = invoiceDiscountAmt;
                    invoice.TotalAmount = subTotal - invoiceDiscountAmt;

                    db.Invoices.Add(invoice);
                    SaveWithNumberRetry(invoice);

                    foreach (var item in invoiceItems)
                        item.InvoiceId = invoice.Id;

                    db.InvoiceItems.AddRange(invoiceItems);
                    db.SaveChanges();

                    // Deduct stock (the Product row version rejects two invoices selling the same stock at once)
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
                    return ServiceResult<int>.Ok(invoice.Id);
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Another invoice changed the same product's stock at the same time
                    transaction.Rollback();
                    return ServiceResult<int>.Fail("Stock was changed by another transaction. Please review quantities and try again.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ServiceResult<int>.Fail("Error saving invoice: " + ex.Message);
                }
            }
        }

        // ---------- cancelling ----------

        /// <summary>Marks the invoice cancelled and returns each item's quantity to stock, in one transaction.</summary>
        public ServiceResult Cancel(int id)
        {
            var invoice = db.Invoices.Find(id);
            if (invoice == null)
                return ServiceResult.Fail("Invoice not found.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                return ServiceResult.Fail("Invoice is already cancelled.");

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var items = db.InvoiceItems.Where(i => i.InvoiceId == id).ToList();
                    foreach (var item in items)
                    {
                        var product = db.Products.Find(item.ProductId);
                        if (product != null)
                            product.StockQuantity += item.Quantity;
                    }

                    invoice.Status = InvoiceStatus.Cancelled;
                    db.SaveChanges();
                    transaction.Commit();

                    return ServiceResult.Ok("Invoice cancelled successfully.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ServiceResult.Fail("Stock was changed by another transaction. Please try again.");
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    return ServiceResult.Fail("Error cancelling invoice.");
                }
            }
        }

        // ---------- helpers ----------

        // Date / customer / status filters, shared by the list and the exports
        private IQueryable<Invoice> Filter(string dateFrom, string dateTo, int? customerId, string status)
        {
            var query = db.Invoices.AsQueryable();

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
                    DateTime toEndOfDay = to.Date.AddDays(1);
                    query = query.Where(i => i.InvoiceDate < toEndOfDay);
                }
            }

            if (customerId.HasValue)
                query = query.Where(i => i.CustomerId == customerId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);

            return query;
        }

        // Next number for today: INV-yyyyMMdd-0001, 0002, ...
        private string GenerateNumber()
        {
            string prefix = "INV-" + DateTime.Now.ToString("yyyyMMdd") + "-";

            var lastInvoice = db.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                string lastPart = lastInvoice.InvoiceNumber.Replace(prefix, "");
                int lastNumber;
                if (int.TryParse(lastPart, out lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return prefix + nextNumber.ToString("D4");
        }

        // The unique index on InvoiceNumber guards against simultaneous saves;
        // on a collision, generate a fresh number and retry.
        private void SaveWithNumberRetry(Invoice invoice)
        {
            const int maxAttempts = 5;
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    db.SaveChanges();
                    return;
                }
                catch (DbUpdateException ex) when (attempt < maxAttempts && IsDuplicateKey(ex))
                {
                    invoice.InvoiceNumber = GenerateNumber();
                }
            }
        }

        private static bool IsDuplicateKey(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                var sql = e as SqlException;
                if (sql != null && (sql.Number == 2601 || sql.Number == 2627))
                    return true;
            }
            return false;
        }
    }
}
