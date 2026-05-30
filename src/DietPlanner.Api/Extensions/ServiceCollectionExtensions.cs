using System.Text;
using DietPlanner.Application.Abstractions;
using DietPlanner.Application.Auth;
using DietPlanner.Application.Meals.Queries;
using DietPlanner.Application.Planning;
using DietPlanner.Application.Plans.Commands;
using DietPlanner.Application.Plans.Planning;
using DietPlanner.Application.Plans.Queries;
using DietPlanner.Application.Shopping;
using DietPlanner.Application.Shopping.Commands;
using DietPlanner.Application.Shopping.Queries;
using DietPlanner.Infrastructure.Auth;
using DietPlanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DietPlanner.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DietPlanner") ?? "Data Source=dietplanner.db";

        services.AddControllers();
        services.AddDbContext<DietPlannerDbContext>(options =>
        {
            if (LooksLikePostgresConnectionString(connectionString))
            {
                options.UseNpgsql(connectionString);
                return;
            }

            options.UseSqlite(connectionString);
        });

        services.AddScoped<IApplicationDbContext>(serviceProvider =>
            serviceProvider.GetRequiredService<DietPlannerDbContext>());

        services.AddScoped<GoogleLoginHandler>();
        services.AddScoped<GetCurrentPlanHandler>();
        services.AddScoped<GetPlanningStateHandler>();
        services.AddScoped<GenerateFutureWeekHandler>();
        services.AddScoped<PlanningStateService>();
        services.AddScoped<ActivateDraftHandler>();
        services.AddScoped<DeleteShoppingListsForPlanHandler>();
        services.AddScoped<ReplaceMealHandler>();
        services.AddScoped<CopyDayHandler>();
        services.AddScoped<SearchMealsHandler>();
        services.AddScoped<GetMealDetailsHandler>();
        services.AddScoped<ListShoppingListsHandler>();
        services.AddScoped<CreateShoppingListHandler>();
        services.AddScoped<GetShoppingListDetailsHandler>();
        services.AddScoped<GetShoppingListCreateOptionsHandler>();
        services.AddScoped<DeleteShoppingListHandler>();
        services.AddScoped<GetShoppingListHandler>();
        services.AddScoped<ToggleShoppingListItemHandler>();
        services.AddScoped<IWeeklyPlanGenerator, WeeklyPlanGenerator>();
        services.AddScoped<IShoppingListService, ShoppingListService>();
        services.AddScoped<IShoppingListSyncService, ShoppingListSyncService>();
        services.AddScoped<IGoogleTokenVerifier, GoogleTokenVerifier>();
        services.AddScoped<ISessionTokenService, JwtSessionTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var signingKey = configuration["Jwt:Key"]
                    ?? throw new InvalidOperationException("JWT signing key configuration is missing: Jwt:Key");
                var issuer = configuration["Jwt:Issuer"] ?? "DietPlanner";
                var audience = configuration["Jwt:Audience"] ?? "DietPlanner.Client";

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

        services.AddAuthorization();

        return services;
    }

    private static bool LooksLikePostgresConnectionString(string connectionString)
        => connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Username=", StringComparison.OrdinalIgnoreCase)
           || connectionString.Contains("Port=", StringComparison.OrdinalIgnoreCase);
}
