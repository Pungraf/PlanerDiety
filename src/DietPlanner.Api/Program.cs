using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Auth;
using DietPlanner.Infrastructure.Auth;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<DietPlannerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DietPlanner") ?? "Data Source=dietplanner.db"));
builder.Services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<DietPlannerDbContext>());
builder.Services.AddScoped<GoogleLoginHandler>();
builder.Services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
builder.Services.AddScoped<ISessionTokenService, JwtSessionTokenService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DietPlannerDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.MapControllers();
app.MapGet("/", () => "DietPlanner API");

app.Run();

public partial class Program
{
}
