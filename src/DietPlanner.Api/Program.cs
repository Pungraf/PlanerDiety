using System.Text;
using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Auth;
using DietPlanner.Application.Meals.Queries;
using DietPlanner.Application.Plans.Commands;
using DietPlanner.Application.Plans.Queries;
using DietPlanner.Application.Shopping;
using DietPlanner.Infrastructure.Auth;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<DietPlannerDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DietPlanner") ?? "Data Source=dietplanner.db"));
builder.Services.AddScoped<IApplicationDbContext>(serviceProvider => serviceProvider.GetRequiredService<DietPlannerDbContext>());
builder.Services.AddScoped<GoogleLoginHandler>();
builder.Services.AddScoped<GetCurrentPlanHandler>();
builder.Services.AddScoped<ActivateDraftHandler>();
builder.Services.AddScoped<ReplaceMealHandler>();
builder.Services.AddScoped<CopyDayHandler>();
builder.Services.AddScoped<SearchMealsHandler>();
builder.Services.AddScoped<IShoppingListService, ShoppingListService>();
builder.Services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
builder.Services.AddScoped<ISessionTokenService, JwtSessionTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var signingKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key configuration is missing: Jwt:Key");
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "DietPlanner";
        var audience = builder.Configuration["Jwt:Audience"] ?? "DietPlanner.Client";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

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
