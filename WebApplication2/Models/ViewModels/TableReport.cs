using System.Collections.Generic;
using WebApplication2.Helpers;

namespace WebApplication2.Models.ViewModels
{
    /// <summary>
    /// A generic table of data, shared by the Excel export and the printable PDF report.
    /// Page/PageSize/TotalRecords are only set when the table is paged (the Reports page);
    /// exports always carry every row.
    /// </summary>
    public class TableReport
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public List<ExcelColumn> Columns { get; set; } = new List<ExcelColumn>();
        public List<object[]> Rows { get; set; } = new List<object[]>();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; } = 1;
    }
}
