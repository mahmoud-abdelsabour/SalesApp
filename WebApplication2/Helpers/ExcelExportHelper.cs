using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace WebApplication2.Helpers
{
    public class ExcelColumn
    {
        public string Header { get; set; }
        public string Format { get; set; }   // optional Excel number/date format

        public ExcelColumn(string header, string format = null)
        {
            Header = header;
            Format = format;
        }
    }

    /// <summary>Builds simple, styled .xlsx files (RTL-aware) from tabular data.</summary>
    public static class ExcelExportHelper
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public const string MoneyFormat = "#,##0.00";
        public const string DateTimeFormat = "yyyy-mm-dd hh:mm";
        public const string DateFormat = "yyyy-mm-dd";

        public static byte[] Build(string sheetName, IList<ExcelColumn> columns, IEnumerable<object[]> rows)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(sheetName);
                ws.View.RightToLeft = LanguageHelper.IsArabic();

                // Header row
                for (int c = 0; c < columns.Count; c++)
                    ws.Cells[1, c + 1].Value = columns[c].Header;

                using (var header = ws.Cells[1, 1, 1, columns.Count])
                {
                    header.Style.Font.Bold = true;
                    header.Style.Font.Color.SetColor(Color.White);
                    header.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    header.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml("#0D1B2A"));
                    header.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Data rows
                var data = rows.ToList();
                if (data.Count > 0)
                    ws.Cells[2, 1].LoadFromArrays(data);

                // Column formats
                for (int c = 0; c < columns.Count; c++)
                {
                    if (data.Count > 0 && !string.IsNullOrEmpty(columns[c].Format))
                        ws.Cells[2, c + 1, data.Count + 1, c + 1].Style.Numberformat.Format = columns[c].Format;
                }

                ws.View.FreezePanes(2, 1);
                if (data.Count > 0)
                    ws.Cells[1, 1, data.Count + 1, columns.Count].AutoFilter = true;

                ws.Cells[ws.Dimension.Address].AutoFitColumns(10, 60);

                return package.GetAsByteArray();
            }
        }

        public static string FileName(string baseName)
        {
            return baseName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
        }
    }
}
