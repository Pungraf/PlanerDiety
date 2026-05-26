using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.ToTable("ShoppingListItems");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.IngredientId).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(64).IsRequired();
        builder.Property(x => x.IsChecked).IsRequired();
        builder.Property<Guid>("ShoppingListId").IsRequired();
    }
}
