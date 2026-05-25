using DietPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DietPlanner.Infrastructure.Persistence.Configurations;

public class WeeklyPlanConfiguration : IEntityTypeConfiguration<WeeklyPlan>
{
    public void Configure(EntityTypeBuilder<WeeklyPlan> builder)
    {
        builder.ToTable("WeeklyPlans");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.DinnerMode).HasConversion<string>().IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().IsRequired();

        builder.HasMany(x => x.Days)
            .WithOne()
            .HasForeignKey("WeeklyPlanId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Days).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
