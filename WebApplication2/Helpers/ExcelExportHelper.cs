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

        /// <summary>True for change/growth columns: the report page shows + / - and colors them.</summary>
        public bool Signed { get; set; }

        public ExcelColumn(string header, string format = null, bool signed = false)
        {
            Header = header;
            Format = format;
            Signed = signed;
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

        /// <summary>Single invoice as a worksheet: header block, items table and totals.</summary>
        public static byte[] BuildInvoice(WebApplication2.Models.ViewModels.InvoiceDetailsViewModel inv)
        {
            using (var package = new ExcelPackage())
            {
                var ws = package.Workbook.Worksheets.Add(Resources.Labels.InvoiceNumber);
                ws.View.RightToLeft = LanguageHelper.IsArabic();
                var dark = ColorTranslator.FromHtml("#0D1B2A");

                ws.Cells["A1"].Value = Resources.Labels.AppName;
                ws.Cells["A1"].Style.Font.Size = 16;
                ws.Cells["A1"].Style.Font.Bold = true;

                int r = 3;
                Action<string, object, string> info = (label, value, format) =>
                {
                    ws.Cells[r, 1].Value = label;
                    ws.Cells[r, 1].Style.Font.Bold = true;
                    ws.Cells[r, 2].Value = value;
                    if (format != null) ws.Cells[r, 2].Style.Numberformat.Format = format;
                    ws.Cells[r, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    r++;
                };

                info(Resources.Labels.InvoiceNumber, inv.InvoiceNumber, null);
                info(Resources.Labels.InvoiceDate, inv.InvoiceDate, DateTimeFormat);
                info(Resources.Labels.InvoiceStatus, inv.Status, null);
                info(Resources.Labels.InvoiceCustomer, inv.CustomerName, null);
                info(Resources.Labels.CustomerPhone, inv.CustomerPhone, null);
                info(Resources.Labels.CustomerEmail, inv.CustomerEmail, null);
                info("Created By", inv.CreatedBy, null);

                // Items table
                r += 1;
                int headerRow = r;
                string[] heads =
                {
                    "#", Resources.Labels.InvoiceProducts, Resources.Labels.InvoiceUnitPrice, Resources.Labels.InvoiceQty,
                    Resources.Labels.InvoiceDiscount + " %", Resources.Labels.InvoiceLineTotal
                };
                for (int c = 0; c < heads.Length; c++)
                    ws.Cells[headerRow, c + 1].Value = heads[c];
                using (var h = ws.Cells[headerRow, 1, headerRow, heads.Length])
                {
                    h.Style.Font.Bold = true;
                    h.Style.Font.Color.SetColor(Color.White);
                    h.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    h.Style.Fill.BackgroundColor.SetColor(dark);
                    h.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                r = headerRow + 1;
                int n = 1;
                foreach (var item in inv.Items)
                {
                    ws.Cells[r, 1].Value = n++;
                    ws.Cells[r, 2].Value = item.ProductName;
                    ws.Cells[r, 3].Value = item.UnitPrice;
                    ws.Cells[r, 4].Value = item.Quantity;
                    ws.Cells[r, 5].Value = item.DiscountPct;
                    ws.Cells[r, 6].Value = item.LineTotal;
                    r++;
                }
                if (inv.Items.Count > 0)
                {
                    int first = headerRow + 1, last = r - 1;
                    ws.Cells[first, 3, last, 3].Style.Numberformat.Format = MoneyFormat;
                    ws.Cells[first, 5, last, 6].Style.Numberformat.Format = MoneyFormat;
                }

                // Totals
                r += 1;
                Action<string, decimal, bool> total = (label, value, bold) =>
                {
                    ws.Cells[r, 5].Value = label;
                    ws.Cells[r, 6].Value = value;
                    ws.Cells[r, 6].Style.Numberformat.Format = MoneyFormat;
                    ws.Cells[r, 5, r, 6].Style.Font.Bold = bold;
                    r++;
                };
                total(Resources.Labels.InvoiceSubTotal, inv.SubTotal, false);
                total(Resources.Labels.InvoiceDiscountAmt + " (" + inv.InvoiceDiscountPct.ToString("N2") + "%)", inv.InvoiceDiscountAmt, false);
                total(Resources.Labels.InvoiceTotal, inv.TotalAmount, true);

                if (!string.IsNullOrEmpty(inv.Notes))
                {
                    r += 1;
                    ws.Cells[r, 1].Value = Resources.Labels.InvoiceNotes;
                    ws.Cells[r, 1].Style.Font.Bold = true;
                    ws.Cells[r, 2].Value = inv.Notes;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns(10, 50);
                return package.GetAsByteArray();
            }
        }

        public static string FileName(string baseName)
        {
            return baseName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".xlsx";
        }
    }
}
