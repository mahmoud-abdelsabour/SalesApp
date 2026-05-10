using System;
using System.Linq;
using WebApplication2.Models.Data;

namespace WebApplication2.Helpers
{
    public static class InvoiceNumberHelper
    {
        public static string Generate(AppDbContext db)
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            string prefix = "INV-" + datePart + "-";

            // Find the last invoice number for today
            var lastInvoice = db.Invoices
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefault();

            int nextNumber = 1;
            if (lastInvoice != null)
            {
                string lastPart = lastInvoice.InvoiceNumber.Replace(prefix, "");
                if (int.TryParse(lastPart, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return prefix + nextNumber.ToString("D4");
        }
    }
}