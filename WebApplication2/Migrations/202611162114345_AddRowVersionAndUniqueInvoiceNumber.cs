namespace WebApplication2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRowVersionAndUniqueInvoiceNumber : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
            CreateIndex("dbo.Invoices", "InvoiceNumber", unique: true, name: "IX_Invoices_InvoiceNumber");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Invoices", "IX_Invoices_InvoiceNumber");
            DropColumn("dbo.Products", "RowVersion");
        }
    }
}
