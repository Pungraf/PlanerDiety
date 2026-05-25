using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public class DailyMealSlotConfiguration : IEntityTypeConfiguration<DailyMealSlot>
{
    public void Configure(EntityTypeBuilder<DailyMealSlot> builder)
    {
        builder.ToTable("DailyMealSlots");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.SlotType).HasConversion<string>().IsRequired();
        builder.Property(x => x.MealId);
        builder.Property<Guid>("DailyPlanId").IsRequired();
    }
}
