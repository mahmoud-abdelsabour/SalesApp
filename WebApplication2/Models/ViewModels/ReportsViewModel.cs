using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Web.Routing;

namespace WebApplication2.Models.ViewModels
{
    public class ChartPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    /// <summary>Reports overview: summary cards for the chosen period plus a headline for each report.</summary>
    public class ReportsViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Confirmed invoices only, whole period
        public decimal TotalRevenue { get; set; }
        public int InvoiceCount { get; set; }
        public decimal AverageInvoice { get; set; }
        public int ItemsSold { get; set; }

        // Headlines shown on the report cards
        public string BestDay { get; set; }
        public decimal BestDayTotal { get; set; }
        public string TopCustomer { get; set; }
        public decimal TopCustomerTotal { get; set; }
        public string TopProduct { get; set; }
        public decimal TopProductRevenue { get; set; }
        public string TopCategory { get; set; }
        public decimal TopCategoryRevenue { get; set; }

        // Daily sales for the trend chart (whole period)
        public List<ChartPoint> DateChart { get; set; }

        // Period headline: this calendar month against the previous one
        public decimal ThisMonthSales { get; set; }
        public decimal? MonthGrowthPercent { get; set; }

        // Profit headline (whole period)
        public decimal TotalProfit { get; set; }
        public decimal? MarginPercent { get; set; }

        // Inventory headline: active products at or below the default low-stock threshold
        public int LowStockCount { get; set; }
        public int LowStockThreshold { get; set; }
    }

    /// <summary>One report page (by date / customer / product) - a chart plus one page of the table.</summary>
    public class ReportPageViewModel
    {
        public string Action { get; set; }      // ByDate | ByCustomer | ByProduct | ByCategory
        public string Type { get; set; }        // date | customer | product | category (used by the exports)
        public string Icon { get; set; }        // Font Awesome icon name
        public string ChartKind { get; set; }   // line | bar
        public string Note { get; set; }

        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public TableReport Table { get; set; }
        public List<ChartPoint> Chart { get; set; }
    }

    /// <summary>Date-range filter used by the overview and every report page.</summary>
    public class ReportRangeModel
    {
        public string Action { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        /// <summary>Extra values the filter form keeps when it is submitted (e.g. granularity).</summary>
        public RouteValueDictionary Extra { get; set; }

        /// <summary>Quick buttons for month / year based ranges instead of day based ones.</summary>
        public bool PeriodChips { get; set; }
    }

    /// <summary>The sales-by-period page: sales grouped by week, month or year with growth.</summary>
    public class PeriodReportViewModel
    {
        public string Granularity { get; set; }   // week | month | year
        public DateTime From { get; set; }        // widened to whole periods
        public DateTime To { get; set; }

        // Summary cards (all periods shown)
        public decimal TotalSales { get; set; }
        public decimal AveragePerPeriod { get; set; }
        public string BestPeriod { get; set; }
        public decimal BestPeriodSales { get; set; }
        public decimal? LatestChangePercent { get; set; }
        public string LatestPeriod { get; set; }

        public TableReport Table { get; set; }
        public List<ChartPoint> Chart { get; set; }
    }

    /// <summary>The profit & margin page: revenue minus cost, grouped by product or category.</summary>
    public class ProfitReportViewModel
    {
        public string GroupBy { get; set; }       // product | category
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Summary cards (whole period)
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal? MarginPercent { get; set; }

        /// <summary>Sold lines whose cost was 0 (cost never set): their profit is overstated.</summary>
        public int UncostedLines { get; set; }

        public TableReport Table { get; set; }
        public List<ChartPoint> Chart { get; set; }
    }

    /// <summary>The inventory / low-stock report page (not date based).</summary>
    public class InventoryReportViewModel
    {
        public int Threshold { get; set; }
        public int? CategoryId { get; set; }
        public bool LowOnly { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }

        // Summary cards (all active products in the chosen category)
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public decimal StockValue { get; set; }

        public TableReport Table { get; set; }
        public List<ChartPoint> Chart { get; set; }
    }

    public class ReportPagerModel
    {
        public string Action { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public TableReport Table { get; set; }

        /// <summary>Route values for pages that aren't date based (From/To are then ignored).</summary>
        public RouteValueDictionary Route { get; set; }
    }
}
