using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public sealed class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.ToTable("Meals");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Type).HasConversion<string>().IsRequired();
        builder.Property(x => x.IsDessert).IsRequired();
        builder.Property(x => x.Kcal).IsRequired();
        builder.Property(x => x.Protein).IsRequired();
        builder.Property(x => x.Description).IsRequired().HasDefaultValue(string.Empty);

        builder.HasMany(x => x.Ingredients)
            .WithOne()
            .HasForeignKey(x => x.MealId);

        builder.Navigation(x => x.Ingredients).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
