namespace CloudStorage.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsDisabled : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FtpUsers", "IsDisabled", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.FtpUsers", "IsDisabled");
        }
    }
}
