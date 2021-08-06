using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace ntbs_service.Migrations
{
    public partial class InitialMigrationsSquashed_prep : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20210629101707_ExpandMBovisExposuresOtherDetails')
                    BEGIN   
                    WITH Migrations AS  
                    (  
                        SELECT 
	                    ROW_NUMBER() OVER(ORDER BY MigrationId ASC) AS Row, MigrationId
	                    FROM [__EFMigrationsHistory]
                        WHERE MigrationId <> '20200326094027_AddDataProtectionKeys'
                    )   
                    DELETE
                    FROM Migrations   
                    WHERE Row <= (SELECT Row FROM Migrations WHERE [MigrationId] = N'20210629101707_ExpandMBovisExposuresOtherDetails');
                    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20210629101707_InitialMigrationsSquashed', N'5.0.5');
                    END");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
