namespace WebApiEE.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddCountryAndCity : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Cities",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CountryId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 100),
                        Population = c.Int(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Countries", t => t.CountryId, cascadeDelete: true)
                .Index(t => new { t.CountryId, t.Name }, unique: true, name: "IX_City_CountryIdName");
            
            CreateTable(
                "dbo.Countries",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Code = c.String(nullable: false, maxLength: 8),
                        Capital = c.String(maxLength: 100),
                        Province = c.String(maxLength: 100),
                        Area = c.Int(nullable: false),
                        Population = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Name, unique: true)
                .Index(t => t.Code, unique: true);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Cities", "CountryId", "dbo.Countries");
            DropIndex("dbo.Countries", new[] { "Code" });
            DropIndex("dbo.Countries", new[] { "Name" });
            DropIndex("dbo.Cities", "IX_City_CountryIdName");
            DropTable("dbo.Countries");
            DropTable("dbo.Cities");
        }
    }
}
