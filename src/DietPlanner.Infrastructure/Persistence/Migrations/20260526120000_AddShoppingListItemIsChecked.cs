using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DietPlannerDbContext))]
[Migration("20260526120000_AddShoppingListItemIsChecked")]
public partial class AddShoppingListItemIsChecked : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "ShoppingListItems"
            ADD COLUMN "IsChecked" INTEGER NOT NULL DEFAULT 0;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
