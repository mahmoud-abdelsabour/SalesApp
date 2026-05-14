namespace WebApplication2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddConcurrentSessions : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ConcurrentSessions",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        SessionToken = c.String(nullable: false, maxLength: 500),
                        LoginTime = c.DateTime(nullable: false),
                        LastActivity = c.DateTime(nullable: false),
                        DeviceInfo = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ConcurrentSessions", "UserId", "dbo.Users");
            DropIndex("dbo.ConcurrentSessions", new[] { "UserId" });
            DropTable("dbo.ConcurrentSessions");
        }
    }
}
