using System;
using System.Linq;
using WebApplication2.Models.Data;
using WebApplication2.Models.Entities;

namespace WebApplication2.Services
{
    /// <summary>
    /// The one definition of "a sale": a Confirmed invoice (cancelled and draft invoices never count).
    /// The dashboard and the reports both build on these queries.
    /// </summary>
    public static class SalesQueries
    {
        public static IQueryable<Invoice> ConfirmedInvoices(this AppDbContext db)
        {
            return db.Invoices.Where(i => i.Status == InvoiceStatus.Confirmed);
        }

        /// <summary>Confirmed invoices dated from <paramref name="from"/> to the end of <paramref name="toInclusive"/>'s day.</summary>
        public static IQueryable<Invoice> ConfirmedInvoices(this AppDbContext db, DateTime from, DateTime toInclusive)
        {
            var end = toInclusive.AddDays(1);
            return db.ConfirmedInvoices().Where(i => i.InvoiceDate >= from && i.InvoiceDate < end);
        }

        public static IQueryable<InvoiceItem> ConfirmedItems(this AppDbContext db)
        {
            return db.InvoiceItems.Where(x => x.Invoice.Status == InvoiceStatus.Confirmed);
        }

        /// <summary>Items of confirmed invoices dated from <paramref name="from"/> to the end of <paramref name="toInclusive"/>'s day.</summary>
        public static IQueryable<InvoiceItem> ConfirmedItems(this AppDbContext db, DateTime from, DateTime toInclusive)
        {
            var end = toInclusive.AddDays(1);
            return db.ConfirmedItems().Where(x => x.Invoice.InvoiceDate >= from && x.Invoice.InvoiceDate < end);
        }
    }
}
