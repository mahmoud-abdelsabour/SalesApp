using System;
using System.Collections.Generic;

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

        // Daily sales for the trend chart (whole period)
        public List<ChartPoint> DateChart { get; set; }
    }

    /// <summary>One report page (by date / customer / product) - a chart plus one page of the table.</summary>
    public class ReportPageViewModel
    {
        public string Action { get; set; }      // ByDate | ByCustomer | ByProduct
        public string Type { get; set; }        // date | customer | product (used by the exports)
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
    }

    public class ReportPagerModel
    {
        public string Action { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public TableReport Table { get; set; }
    }
}
