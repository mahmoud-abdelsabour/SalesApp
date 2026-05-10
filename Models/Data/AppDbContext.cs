using System.Data.Entity;
using WebApplication2.Models.Entities;

namespace WebApplication2.Models.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=DefaultConnection")
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ConcurrentSession> ConcurrentSessions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            // Category -> Products: cascade delete
            modelBuilder.Entity<Product>()
                .HasRequired(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .WillCascadeOnDelete(true);

            // Invoice -> InvoiceItems: cascade delete
            modelBuilder.Entity<InvoiceItem>()
                .HasRequired(i => i.Invoice)
                .WithMany(inv => inv.Items)
                .HasForeignKey(i => i.InvoiceId)
                .WillCascadeOnDelete(true);

            // InvoiceItem -> Product: NO cascade (preserve history)
            modelBuilder.Entity<InvoiceItem>()
                .HasRequired(i => i.Product)
                .WithMany(p => p.InvoiceItems)
                .HasForeignKey(i => i.ProductId)
                .WillCascadeOnDelete(false);

            // Invoice -> Customer: no cascade (preserve history)
            modelBuilder.Entity<Invoice>()
                .HasRequired(i => i.Customer)
                .WithMany(c => c.Invoices)
                .HasForeignKey(i => i.CustomerId)
                .WillCascadeOnDelete(false);

            // Invoice -> User (CreatedBy): no cascade
            modelBuilder.Entity<Invoice>()
                .HasRequired(i => i.CreatedByUser)
                .WithMany()
                .HasForeignKey(i => i.CreatedBy)
                .WillCascadeOnDelete(false);

            // Decimal precision for all decimal columns
            modelBuilder.Entity<Product>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.SubTotal)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceDiscountPct)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.InvoiceDiscountAmt)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Invoice>()
                .Property(i => i.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(i => i.DiscountPct)
                .HasPrecision(5, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(i => i.DiscountAmt)
                .HasPrecision(18, 2);

            modelBuilder.Entity<InvoiceItem>()
                .Property(i => i.LineTotal)
                .HasPrecision(18, 2);
        }
    }
}