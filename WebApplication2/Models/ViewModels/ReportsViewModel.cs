using System;
using System.Collections.Generic;

namespace WebApplication2.Models.ViewModels
{
    public class ChartPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class ReportsViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Summary cards (confirmed invoices only, whole period)
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageInvoice { get; set; }
        public int ItemsSold { get; set; }

        // One page of each table (server-side paging)
        public TableReport ByDate { get; set; }
        public TableReport ByCustomer { get; set; }
        public TableReport ByProduct { get; set; }

        // Charts use the whole period, independent of the table page
        public List<ChartPoint> DateChart { get; set; }
        public List<ChartPoint> CustomerChart { get; set; }
        public List<ChartPoint> ProductChart { get; set; }

        // Current page of each tab, so pager links keep the other tabs' pages
        public int DatePage { get; set; }
        public int CustomerPage { get; set; }
        public int ProductPage { get; set; }
    }

    public class ReportPagerModel
    {
        public string Param { get; set; }      // dp | cp | pp
        public string TabHash { get; set; }    // #tab-date ...
        public TableReport Table { get; set; }
        public ReportsViewModel Report { get; set; }
    }
}
