using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public sealed class MealIngredientConfiguration : IEntityTypeConfiguration<MealIngredient>
{
    public void Configure(EntityTypeBuilder<MealIngredient> builder)
    {
        builder.ToTable("MealIngredients");

        builder.HasKey(x => new { x.MealId, x.IngredientId });
        builder.HasOne<Ingredient>()
            .WithMany()
            .HasForeignKey(x => x.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.Unit).IsRequired();
        builder.Property(x => x.ShoppingCategory).IsRequired();
    }
}
