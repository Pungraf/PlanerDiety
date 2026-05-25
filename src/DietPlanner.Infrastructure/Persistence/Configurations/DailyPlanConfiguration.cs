using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public class DailyPlanConfiguration : IEntityTypeConfiguration<DailyPlan>
{
    public void Configure(EntityTypeBuilder<DailyPlan> builder)
    {
        builder.ToTable("DailyPlans");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Date).IsRequired();
        builder.Property<Guid>("WeeklyPlanId").IsRequired();

        builder.HasMany(x => x.MealSlots)
            .WithOne()
            .HasForeignKey("DailyPlanId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.MealSlots).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
