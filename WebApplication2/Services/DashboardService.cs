using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using WebApplication2.Models.Data;

namespace WebApplication2.Services
{
    public class DashboardSummary
    {
        public int Categories { get; set; }
        public int Products { get; set; }
        public int Customers { get; set; }
        public int Invoices { get; set; }
        public decimal Revenue { get; set; }
    }

    public class LowStockItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Stock { get; set; }
    }

    /// <summary>The dashboard's low-stock alert: how many products, and the ones closest to running out.</summary>
    public class LowStockSummary
    {
        public int Threshold { get; set; }
        public int Count { get; set; }
        public int OutOfStock { get; set; }
        public List<LowStockItem> Items { get; set; }
    }

    /// <summary>Labels and values for one dashboard chart.</summary>
    public class ChartSeries
    {
        public List<string> Labels { get; set; }
        public List<decimal> Values { get; set; }
    }

    /// <summary>Numbers and charts for the home dashboard.</summary>
    public class DashboardService
    {
        private readonly AppDbContext db;

        public DashboardService(AppDbContext db)
        {
            this.db = db;
        }

        public DashboardSummary GetSummary()
        {
            return new DashboardSummary
            {
                Categories = db.Categories.Count(c => c.IsActive),
                Products = db.Products.Count(p => p.IsActive),
                Customers = db.Customers.Count(c => c.IsActive),
                Invoices = db.Invoices.Count(),
                Revenue = db.ConfirmedInvoices().Sum(i => (decimal?)i.TotalAmount) ?? 0
            };
        }

        /// <summary>Active products at or below the saved low-stock level, lowest stock first (the worst few are listed).</summary>
        public LowStockSummary GetLowStock(int listSize = 6)
        {
            int threshold = new SettingsService(db).GetLowStockThreshold();
            bool ar = WebApplication2.Helpers.LanguageHelper.IsArabic();

            var low = db.Products.Where(p => p.IsActive && p.StockQuantity <= threshold);

            var items = low
                .OrderBy(p => p.StockQuantity).ThenBy(p => p.NameEn)
                .Take(listSize)
                .Select(p => new
                {
                    p.Id,
                    p.NameEn,
                    p.NameAr,
                    CategoryEn = p.Category.NameEn,
                    CategoryAr = p.Category.NameAr,
                    p.StockQuantity
                })
                .ToList();

            return new LowStockSummary
            {
                Threshold = threshold,
                Count = low.Count(),
                OutOfStock = low.Count(p => p.StockQuantity == 0),
                Items = items.Select(p => new LowStockItem
                {
                    Id = p.Id,
                    Name = ar ? p.NameAr : p.NameEn,
                    Category = ar ? p.CategoryAr : p.CategoryEn,
                    Stock = p.StockQuantity
                }).ToList()
            };
        }

        /// <summary>Daily sales over the last 30 days.</summary>
        public ChartSeries GetSalesChart()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var data = db.ConfirmedInvoices()
                .Where(i => i.InvoiceDate >= thirtyDaysAgo)
                .GroupBy(i => DbFunctions.TruncateTime(i.InvoiceDate))
                .Select(g => new { date = g.Key, total = g.Sum(i => i.TotalAmount) })
                .OrderBy(g => g.date)
                .ToList();

            return new ChartSeries
            {
                Labels = data.Select(d => d.date.Value.ToString("MM/dd")).ToList(),
                Values = data.Select(d => d.total).ToList()
            };
        }

        /// <summary>Top 5 products by quantity sold.</summary>
        public ChartSeries GetTopProducts()
        {
            var data = db.ConfirmedItems()
                .GroupBy(i => i.ProductName)
                .Select(g => new { name = g.Key, qty = g.Sum(i => i.Quantity) })
                .OrderByDescending(g => g.qty)
                .Take(5)
                .ToList();

            return new ChartSeries
            {
                Labels = data.Select(d => d.name).ToList(),
                Values = data.Select(d => (decimal)d.qty).ToList()
            };
        }

        /// <summary>Revenue per product category.</summary>
        public ChartSeries GetRevenueByCategory()
        {
            var data = db.ConfirmedItems()
                .GroupBy(i => i.Product.Category.NameEn)
                .Select(g => new { category = g.Key, revenue = g.Sum(i => i.LineTotal) })
                .OrderByDescending(g => g.revenue)
                .ToList();

            return new ChartSeries
            {
                Labels = data.Select(d => d.category).ToList(),
                Values = data.Select(d => d.revenue).ToList()
            };
        }
    }
}
