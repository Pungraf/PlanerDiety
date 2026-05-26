using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DietPlanner.Infrastructure.Persistence.Migrations;

[DbContext(typeof(DietPlannerDbContext))]
[Migration(SqliteSchemaBootstrapper.BaselineMigrationId)]
public partial class BaselineCurrentSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE "Users" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Email" TEXT NULL,
                "GoogleSubject" TEXT NULL
            );

            CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email") WHERE "Email" IS NOT NULL;
            CREATE UNIQUE INDEX "IX_Users_GoogleSubject" ON "Users" ("GoogleSubject") WHERE "GoogleSubject" IS NOT NULL;

            CREATE TABLE "Ingredients" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Ingredients" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Unit" TEXT NOT NULL
            );

            CREATE TABLE "Meals" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_Meals" PRIMARY KEY,
                "Name" TEXT NOT NULL,
                "Type" TEXT NOT NULL,
                "IsDessert" INTEGER NOT NULL,
                "Kcal" INTEGER NOT NULL,
                "Protein" INTEGER NOT NULL
            );

            CREATE TABLE "WeeklyPlans" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_WeeklyPlans" PRIMARY KEY,
                "UserId" TEXT NOT NULL,
                "StartDate" TEXT NOT NULL,
                "DinnerMode" TEXT NOT NULL,
                "Status" TEXT NOT NULL
            );

            CREATE TABLE "DailyPlans" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_DailyPlans" PRIMARY KEY,
                "Date" TEXT NOT NULL,
                "WeeklyPlanId" TEXT NOT NULL,
                CONSTRAINT "FK_DailyPlans_WeeklyPlans_WeeklyPlanId" FOREIGN KEY ("WeeklyPlanId") REFERENCES "WeeklyPlans" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "MealIngredients" (
                "MealId" TEXT NOT NULL,
                "IngredientId" TEXT NOT NULL,
                "Quantity" TEXT NOT NULL,
                "Unit" TEXT NOT NULL,
                "ShoppingCategory" TEXT NOT NULL,
                CONSTRAINT "PK_MealIngredients" PRIMARY KEY ("MealId", "IngredientId"),
                CONSTRAINT "FK_MealIngredients_Ingredients_IngredientId" FOREIGN KEY ("IngredientId") REFERENCES "Ingredients" ("Id") ON DELETE RESTRICT,
                CONSTRAINT "FK_MealIngredients_Meals_MealId" FOREIGN KEY ("MealId") REFERENCES "Meals" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "ShoppingLists" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ShoppingLists" PRIMARY KEY,
                "WeeklyPlanId" TEXT NOT NULL,
                CONSTRAINT "FK_ShoppingLists_WeeklyPlans_WeeklyPlanId" FOREIGN KEY ("WeeklyPlanId") REFERENCES "WeeklyPlans" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "DailyMealSlots" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_DailyMealSlots" PRIMARY KEY,
                "SlotType" TEXT NOT NULL,
                "MealId" TEXT NULL,
                "DailyPlanId" TEXT NOT NULL,
                CONSTRAINT "FK_DailyMealSlots_DailyPlans_DailyPlanId" FOREIGN KEY ("DailyPlanId") REFERENCES "DailyPlans" ("Id") ON DELETE CASCADE
            );

            CREATE TABLE "ShoppingListItems" (
                "Id" TEXT NOT NULL CONSTRAINT "PK_ShoppingListItems" PRIMARY KEY,
                "IngredientId" TEXT NOT NULL,
                "Quantity" TEXT NOT NULL,
                "Unit" TEXT NOT NULL,
                "ShoppingListId" TEXT NOT NULL,
                CONSTRAINT "FK_ShoppingListItems_ShoppingLists_ShoppingListId" FOREIGN KEY ("ShoppingListId") REFERENCES "ShoppingLists" ("Id") ON DELETE CASCADE
            );

            CREATE INDEX "IX_DailyMealSlots_DailyPlanId" ON "DailyMealSlots" ("DailyPlanId");
            CREATE INDEX "IX_DailyPlans_WeeklyPlanId" ON "DailyPlans" ("WeeklyPlanId");
            CREATE UNIQUE INDEX "IX_ShoppingLists_WeeklyPlanId" ON "ShoppingLists" ("WeeklyPlanId");
            CREATE INDEX "IX_ShoppingListItems_ShoppingListId" ON "ShoppingListItems" ("ShoppingListId");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "ShoppingListItems";
            DROP TABLE IF EXISTS "DailyMealSlots";
            DROP TABLE IF EXISTS "MealIngredients";
            DROP TABLE IF EXISTS "ShoppingLists";
            DROP TABLE IF EXISTS "DailyPlans";
            DROP TABLE IF EXISTS "Ingredients";
            DROP TABLE IF EXISTS "Meals";
            DROP TABLE IF EXISTS "WeeklyPlans";
            DROP TABLE IF EXISTS "Users";
            """);
    }
}
