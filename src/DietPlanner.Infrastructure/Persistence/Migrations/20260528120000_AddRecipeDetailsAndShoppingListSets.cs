using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DietPlannerDbContext))]
[Migration("20260528120000_AddRecipeDetailsAndShoppingListSets")]
public partial class AddRecipeDetailsAndShoppingListSets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "ShoppingLists"
            ADD COLUMN "Name" TEXT NOT NULL DEFAULT 'Shopping list';

            ALTER TABLE "ShoppingLists"
            ADD COLUMN "CreatedAt" TEXT NOT NULL DEFAULT '2026-05-28T00:00:00+00:00';

            DROP INDEX IF EXISTS "IX_ShoppingLists_WeeklyPlanId";

            CREATE INDEX "IX_ShoppingLists_WeeklyPlanId" ON "ShoppingLists" ("WeeklyPlanId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
