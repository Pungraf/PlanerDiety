using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public class ShoppingListConfiguration : IEntityTypeConfiguration<ShoppingList>
{
    public void Configure(EntityTypeBuilder<ShoppingList> builder)
    {
        builder.ToTable("ShoppingLists");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.WeeklyPlanId).IsRequired();

        builder.HasIndex(x => x.WeeklyPlanId).IsUnique();

        builder.HasOne<WeeklyPlan>()
            .WithOne()
            .HasForeignKey<ShoppingList>(x => x.WeeklyPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Items)
            .WithOne()
            .HasForeignKey("ShoppingListId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
