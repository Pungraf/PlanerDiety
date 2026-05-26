using DietPlanner.Api.Extensions;
using DietPlanner.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationServices(builder.Configuration);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
    await SqliteSchemaBootstrapper.InitializeAsync(dbContext);
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => "DietPlanner API");

app.Run();

public partial class Program
{
}
