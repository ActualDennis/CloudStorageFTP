namespace CloudStorage.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class noName : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FtpUsers", "StorageBytesTotal", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.FtpUsers", "StorageBytesTotal");
        }
    }
}
