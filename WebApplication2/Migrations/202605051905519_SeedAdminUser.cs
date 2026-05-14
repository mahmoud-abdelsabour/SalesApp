namespace WebApplication2.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class SeedAdminUser : DbMigration
    {
        public override void Up()
        {
            string encrypted = WebApplication2.Helpers.EncryptionHelper.Encrypt("Admin@123");
            Sql($"INSERT INTO Users (Username, PasswordEncrypted, FullName, Email, IsActive, CreatedAt) VALUES ('admin', '{encrypted}', 'System Administrator', 'admin@vtssales.com', 1, GETDATE())");
        }

        public override void Down()
        {
            Sql("DELETE FROM Users WHERE Username = 'admin'");
        }
    }
}
