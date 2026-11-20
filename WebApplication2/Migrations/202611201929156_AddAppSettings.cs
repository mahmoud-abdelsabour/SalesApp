namespace WebApplication2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddAppSettings : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AppSettings",
                c => new
                    {
                        Key = c.String(nullable: false, maxLength: 100),
                        Value = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Key);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.AppSettings");
        }
    }
}
