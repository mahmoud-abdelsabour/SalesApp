using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using WebApplication2.Helpers;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;
using WebApplication2.Models.ViewModels;

namespace WebApplication2.Services
{
    /// <summary>
    /// Sales reports (by date, customer, product). Only confirmed invoices count,
    /// as defined in <see cref="SalesQueries"/>.
    /// </summary>
    public class ReportService
    {
        private const int PageSize = 15;

        /// <summary>Products with this much stock or less count as "low stock" unless the user picks another level.</summary>
        public const int DefaultLowStockThreshold = 10;

        // Row shapes for the grouped queries (so paging can run in the database)
        private class DateRow { public DateTime? Day { get; set; } public int Count { get; set; } public decimal Total { get; set; } }
        private class CustomerRow { public string Name { get; set; } public int Count { get; set; } public decimal Total { get; set; } }
        private class CategoryRow { public string Name { get; set; } public int Invoices { get; set; } public int Qty { get; set; } public decimal Revenue { get; set; } }
        private class ProductRow { public string Name { get; set; } public int Qty { get; set; } public decimal Revenue { get; set; } }
        private class InventoryRow { public string Name { get; set; } public string Category { get; set; } public int Stock { get; set; } public decimal Price { get; set; } public decimal Value { get; set; } }

        private readonly AppDbContext db;

        public ReportService(AppDbContext db)
        {
            this.db = db;
        }

        // ---------- pages ----------

        /// <summary>Summary cards, a headline per report and the daily trend for the period.</summary>
        public ReportsViewModel GetOverview(string from, string to)
        {
            DateTime f, t;
            ParseRange(from, to, out f, out t);

            var sales = db.ConfirmedInvoices(f, t);
            int invoiceCount = sales.Count();
            decimal revenue = sales.Sum(i => (decimal?)i.TotalAmount) ?? 0m;

            var days = DateGroups(f, t).ToList();
            var bestDay = days.OrderByDescending(r => r.Total).FirstOrDefault();
            var topCustomer = CustomerGroups(f, t).FirstOrDefault();
            var topProduct = ProductGroups(f, t).FirstOrDefault();
            var topCategory = CategoryGroups(f, t).FirstOrDefault();

            return new ReportsViewModel
            {
                From = f,
                To = t,
                InvoiceCount = invoiceCount,
                TotalRevenue = revenue,
                AverageInvoice = invoiceCount == 0 ? 0m : Math.Round(revenue / invoiceCount, 2),
                ItemsSold = db.ConfirmedItems(f, t).Sum(x => (int?)x.Quantity) ?? 0,
                BestDay = bestDay == null ? null : bestDay.Day.Value.ToString("yyyy-MM-dd"),
                BestDayTotal = bestDay == null ? 0m : bestDay.Total,
                TopCustomer = topCustomer == null ? null : topCustomer.Name,
                TopCustomerTotal = topCustomer == null ? 0m : topCustomer.Total,
                TopProduct = topProduct == null ? null : topProduct.Name,
                TopProductRevenue = topProduct == null ? 0m : topProduct.Revenue,
                TopCategory = topCategory == null ? null : topCategory.Name,
                TopCategoryRevenue = topCategory == null ? 0m : topCategory.Revenue,
                DateChart = DateChart(days),
                LowStockThreshold = DefaultLowStockThreshold,
                LowStockCount = db.Products.Count(p => p.IsActive && p.StockQuantity <= DefaultLowStockThreshold)
            };
        }

        public ReportPageViewModel GetByDate(string from, string to, int page)
        {
            DateTime f, t;
            ParseRange(from, to, out f, out t);

            return new ReportPageViewModel
            {
                Action = "ByDate", Type = "date", Icon = "calendar-day", ChartKind = "line",
                From = f, To = t,
                Table = BuildDateTable(f, t, page),
                // the chart covers every day in the period, not only the table page
                Chart = DateChart(DateGroups(f, t).ToList())
            };
        }

        public ReportPageViewModel GetByCustomer(string from, string to, int page)
        {
            DateTime f, t;
            ParseRange(from, to, out f, out t);

            return new ReportPageViewModel
            {
                Action = "ByCustomer", Type = "customer", Icon = "users", ChartKind = "bar",
                From = f, To = t,
                Table = BuildCustomerTable(f, t, page),
                Chart = CustomerGroups(f, t).Take(10).ToList()
                    .Select(r => new ChartPoint { Label = r.Name, Value = r.Total }).ToList()
            };
        }

        public ReportPageViewModel GetByProduct(string from, string to, int page)
        {
            DateTime f, t;
            ParseRange(from, to, out f, out t);

            return new ReportPageViewModel
            {
                Action = "ByProduct", Type = "product", Icon = "box", ChartKind = "bar",
                Note = LanguageHelper.T("Revenue is before invoice-level discount.", "الإيراد قبل خصم الفاتورة."),
                From = f, To = t,
                Table = BuildProductTable(f, t, page),
                Chart = ProductGroups(f, t).Take(10).ToList()
                    .Select(r => new ChartPoint { Label = r.Name, Value = r.Revenue }).ToList()
            };
        }

        public ReportPageViewModel GetByCategory(string from, string to, int page)
        {
            DateTime f, t;
            ParseRange(from, to, out f, out t);

            return new ReportPageViewModel
            {
                Action = "ByCategory", Type = "category", Icon = "tags", ChartKind = "bar",
                Note = LanguageHelper.T("Revenue is before invoice-level discount.", "الإيراد قبل خصم الفاتورة."),
                From = f, To = t,
                Table = BuildCategoryTable(f, t, page),
                Chart = CategoryGroups(f, t).Take(10).ToList()
                    .Select(r => new ChartPoint { Label = r.Name, Value = r.Revenue }).ToList()
            };
        }

        /// <summary>A whole report (never paged) for the Excel / PDF exports. type: date | customer | product | category.</summary>
        public TableReport BuildTable(string type, string from, string to)
        {
            DateTime f, t;
            ParseRange(from, to, out f, out t);

            switch ((type ?? "").ToLowerInvariant())
            {
                case "customer": return BuildCustomerTable(f, t, null);
                case "product": return BuildProductTable(f, t, null);
                case "category": return BuildCategoryTable(f, t, null);
                default: return BuildDateTable(f, t, null);
            }
        }

        // ---------- inventory / low stock ----------

        /// <summary>
        /// Stock levels of active products: summary cards for the chosen category, the lowest-stock chart,
        /// and one page of the table (optionally only products at or below the threshold).
        /// </summary>
        public InventoryReportViewModel GetInventory(int threshold, int? categoryId, bool lowOnly, int page)
        {
            threshold = Math.Max(0, threshold);
            var scope = InventoryScope(categoryId);

            return new InventoryReportViewModel
            {
                Threshold = threshold,
                CategoryId = categoryId,
                LowOnly = lowOnly,
                Categories = new CategoryService(db).GetSelectList(categoryId),
                TotalProducts = scope.Count(),
                LowStockCount = scope.Count(p => p.StockQuantity <= threshold),
                OutOfStockCount = scope.Count(p => p.StockQuantity == 0),
                StockValue = scope.Sum(p => (decimal?)(p.Price * p.StockQuantity)) ?? 0m,
                Table = BuildInventoryTable(threshold, categoryId, lowOnly, page),
                // the products closest to running out (within the current filter)
                Chart = InventoryRows(threshold, categoryId, lowOnly).Take(10).ToList()
                    .Select(r => new ChartPoint { Label = r.Name, Value = r.Stock }).ToList()
            };
        }

        /// <summary>The whole inventory report (never paged) for the Excel / PDF exports.</summary>
        public TableReport BuildInventoryTable(int threshold, int? categoryId, bool lowOnly)
        {
            return BuildInventoryTable(Math.Max(0, threshold), categoryId, lowOnly, null);
        }

        private TableReport BuildInventoryTable(int threshold, int? categoryId, bool lowOnly, int? page)
        {
            var subtitle = LanguageHelper.T("Low stock level", "حد انخفاض المخزون") + ": " + threshold;
            if (lowOnly)
                subtitle += "  |  " + LanguageHelper.T("Low stock only", "المخزون المنخفض فقط");

            var table = new TableReport
            {
                Title = LanguageHelper.T("Inventory & Low Stock", "المخزون والمخزون المنخفض"),
                Subtitle = subtitle,
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(LanguageHelper.T("Product", "المنتج")),
                    new ExcelColumn(LanguageHelper.T("Category", "الفئة")),
                    new ExcelColumn(LanguageHelper.T("In Stock", "المخزون")),
                    new ExcelColumn(LanguageHelper.T("Unit Price", "سعر الوحدة"), ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(LanguageHelper.T("Stock Value", "قيمة المخزون"), ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(LanguageHelper.T("Status", "الحالة"))
                }
            };

            var rows = Fetch(InventoryRows(threshold, categoryId, lowOnly), page, table);
            table.Rows = rows.Select(r => new object[]
            {
                r.Name, r.Category, r.Stock, r.Price, r.Value, StockStatus(r.Stock, threshold)
            }).ToList();
            return table;
        }

        // Active products, optionally in one category
        private IQueryable<Product> InventoryScope(int? categoryId)
        {
            var query = db.Products.Where(p => p.IsActive);

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            return query;
        }

        // Lowest stock first, then by name
        private IQueryable<InventoryRow> InventoryRows(int threshold, int? categoryId, bool lowOnly)
        {
            var query = InventoryScope(categoryId);

            if (lowOnly)
                query = query.Where(p => p.StockQuantity <= threshold);

            return query
                .Select(p => new InventoryRow
                {
                    Name = p.NameEn,
                    Category = p.Category.NameEn,
                    Stock = p.StockQuantity,
                    Price = p.Price,
                    Value = p.Price * p.StockQuantity
                })
                .OrderBy(r => r.Stock).ThenBy(r => r.Name);
        }

        private static string StockStatus(int stock, int threshold)
        {
            if (stock == 0) return LanguageHelper.T("Out of stock", "نفد المخزون");
            if (stock <= threshold) return LanguageHelper.T("Low", "منخفض");
            return LanguageHelper.T("OK", "جيد");
        }

        // ---------- tables ----------

        private TableReport BuildDateTable(DateTime f, DateTime t, int? page)
        {
            var table = new TableReport
            {
                Title = LanguageHelper.T("Sales by Date", "المبيعات حسب التاريخ"),
                Subtitle = RangeText(f, t),
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(LanguageHelper.T("Date", "التاريخ"), ExcelExportHelper.DateFormat),
                    new ExcelColumn(LanguageHelper.T("Invoices", "الفواتير")),
                    new ExcelColumn(LanguageHelper.T("Total Sales", "إجمالي المبيعات"), ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(LanguageHelper.T("Average Invoice", "متوسط الفاتورة"), ExcelExportHelper.MoneyFormat)
                }
            };

            var rows = Fetch(DateGroups(f, t), page, table);
            table.Rows = rows.Select(r => new object[]
            {
                r.Day.Value, r.Count, r.Total, Math.Round(r.Total / r.Count, 2)
            }).ToList();
            return table;
        }

        private TableReport BuildCustomerTable(DateTime f, DateTime t, int? page)
        {
            var table = new TableReport
            {
                Title = LanguageHelper.T("Sales by Customer", "المبيعات حسب العميل"),
                Subtitle = RangeText(f, t),
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(LanguageHelper.T("Customer", "العميل")),
                    new ExcelColumn(LanguageHelper.T("Invoices", "الفواتير")),
                    new ExcelColumn(LanguageHelper.T("Total Sales", "إجمالي المبيعات"), ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(LanguageHelper.T("Share %", "النسبة %"), ExcelExportHelper.MoneyFormat)
                }
            };

            // Share % is relative to the whole period, not just the visible page
            decimal grand = db.ConfirmedInvoices(f, t).Sum(i => (decimal?)i.TotalAmount) ?? 0m;

            var rows = Fetch(CustomerGroups(f, t), page, table);
            table.Rows = rows.Select(r => new object[]
            {
                r.Name, r.Count, r.Total, grand == 0 ? 0m : Math.Round(r.Total * 100 / grand, 2)
            }).ToList();
            return table;
        }

        private TableReport BuildProductTable(DateTime f, DateTime t, int? page)
        {
            var table = new TableReport
            {
                Title = LanguageHelper.T("Sales by Product", "المبيعات حسب المنتج"),
                Subtitle = RangeText(f, t) + "  |  " + LanguageHelper.T("Revenue is before invoice-level discount", "الإيراد قبل خصم الفاتورة"),
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(LanguageHelper.T("Product", "المنتج")),
                    new ExcelColumn(LanguageHelper.T("Quantity Sold", "الكمية المباعة")),
                    new ExcelColumn(LanguageHelper.T("Revenue", "الإيراد"), ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(LanguageHelper.T("Share %", "النسبة %"), ExcelExportHelper.MoneyFormat)
                }
            };

            decimal grand = db.ConfirmedItems(f, t).Sum(x => (decimal?)x.LineTotal) ?? 0m;

            var rows = Fetch(ProductGroups(f, t), page, table);
            table.Rows = rows.Select(r => new object[]
            {
                r.Name, r.Qty, r.Revenue, grand == 0 ? 0m : Math.Round(r.Revenue * 100 / grand, 2)
            }).ToList();
            return table;
        }

        private TableReport BuildCategoryTable(DateTime f, DateTime t, int? page)
        {
            var table = new TableReport
            {
                Title = LanguageHelper.T("Sales by Category", "المبيعات حسب الفئة"),
                Subtitle = RangeText(f, t) + "  |  " + LanguageHelper.T("Revenue is before invoice-level discount", "الإيراد قبل خصم الفاتورة"),
                Columns = new List<ExcelColumn>
                {
                    new ExcelColumn(LanguageHelper.T("Category", "الفئة")),
                    new ExcelColumn(LanguageHelper.T("Invoices", "الفواتير")),
                    new ExcelColumn(LanguageHelper.T("Quantity Sold", "الكمية المباعة")),
                    new ExcelColumn(LanguageHelper.T("Revenue", "الإيراد"), ExcelExportHelper.MoneyFormat),
                    new ExcelColumn(LanguageHelper.T("Share %", "النسبة %"), ExcelExportHelper.MoneyFormat)
                }
            };

            // Share % is relative to the whole period, not just the visible page
            decimal grand = db.ConfirmedItems(f, t).Sum(x => (decimal?)x.LineTotal) ?? 0m;

            var rows = Fetch(CategoryGroups(f, t), page, table);
            table.Rows = rows.Select(r => new object[]
            {
                r.Name, r.Invoices, r.Qty, r.Revenue, grand == 0 ? 0m : Math.Round(r.Revenue * 100 / grand, 2)
            }).ToList();
            return table;
        }

        // ---------- grouped queries ----------

        private IQueryable<DateRow> DateGroups(DateTime f, DateTime t)
        {
            return db.ConfirmedInvoices(f, t)
                .GroupBy(i => DbFunctions.TruncateTime(i.InvoiceDate))
                .Select(g => new DateRow { Day = g.Key, Count = g.Count(), Total = g.Sum(i => i.TotalAmount) })
                .OrderBy(r => r.Day);
        }

        private IQueryable<CustomerRow> CustomerGroups(DateTime f, DateTime t)
        {
            return db.ConfirmedInvoices(f, t)
                .GroupBy(i => new { i.CustomerId, i.Customer.FullName })
                .Select(g => new CustomerRow { Name = g.Key.FullName, Count = g.Count(), Total = g.Sum(i => i.TotalAmount) })
                .OrderByDescending(r => r.Total).ThenBy(r => r.Name);
        }

        private IQueryable<CategoryRow> CategoryGroups(DateTime f, DateTime t)
        {
            return db.ConfirmedItems(f, t)
                .GroupBy(x => new { x.Product.CategoryId, x.Product.Category.NameEn })
                .Select(g => new CategoryRow
                {
                    Name = g.Key.NameEn,
                    Invoices = g.Select(x => x.InvoiceId).Distinct().Count(),
                    Qty = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(r => r.Revenue).ThenBy(r => r.Name);
        }

        private IQueryable<ProductRow> ProductGroups(DateTime f, DateTime t)
        {
            return db.ConfirmedItems(f, t)
                .GroupBy(x => new { x.ProductId, x.ProductName })
                .Select(g => new ProductRow { Name = g.Key.ProductName, Qty = g.Sum(x => x.Quantity), Revenue = g.Sum(x => x.LineTotal) })
                .OrderByDescending(r => r.Revenue).ThenBy(r => r.Name);
        }

        private static List<ChartPoint> DateChart(List<DateRow> days)
        {
            return days.Select(r => new ChartPoint { Label = r.Day.Value.ToString("MM/dd"), Value = r.Total }).ToList();
        }

        // ---------- helpers ----------

        // Applies Skip/Take in the database when a page is requested; page == null returns everything.
        private static List<T> Fetch<T>(IQueryable<T> ordered, int? page, TableReport table)
        {
            int total = ordered.Count();
            table.TotalRecords = total;

            if (!page.HasValue)
                return ordered.ToList();

            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            int current = Math.Min(Math.Max(1, page.Value), totalPages);

            table.PageSize = PageSize;
            table.TotalPages = totalPages;
            table.Page = current;

            return ordered.Skip((current - 1) * PageSize).Take(PageSize).ToList();
        }

        // Dates arrive as yyyy-MM-dd (<input type="date">); default is the last 30 days.
        private static void ParseRange(string from, string to, out DateTime f, out DateTime t)
        {
            DateTime tmp;
            const string fmt = "yyyy-MM-dd";

            t = DateTime.TryParseExact(to, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmp)
                ? tmp.Date : DateTime.Today;
            f = DateTime.TryParseExact(from, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out tmp)
                ? tmp.Date : t.AddDays(-29);

            if (f > t)
            {
                var swap = f; f = t; t = swap;
            }
        }

        private static string RangeText(DateTime f, DateTime t)
        {
            // LRM marks keep each date in left-to-right order inside RTL text
            const string lrm = "‎";
            return LanguageHelper.T("From", "من") + " " + lrm + f.ToString("yyyy-MM-dd") + lrm + " " +
                   LanguageHelper.T("to", "إلى") + " " + lrm + t.ToString("yyyy-MM-dd") + lrm;
        }
    }
}
