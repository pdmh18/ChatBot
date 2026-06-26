using Application.Features.Ai;
using Application.Features.Auth;
using Application.Features.Dashboard;
using Application.Features.Lookups;
using Application.Features.Tasks;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Config;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing connection string 'Default'.");
        }

        services.AddDbContext<QuanLyDuAnAiContext>(options =>
            options.UseSqlServer(connectionString));

        // Auth
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();

        // Lookups
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<ILookupService, Application.Features.Lookups.LookupService>();

        // Tasks
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskService, Application.Features.Tasks.TaskService>();

        // AI
        services.AddScoped<IAiPredictionRepository, AiPredictionRepository>();
        services.AddScoped<IAiIntegrationService, AiIntegrationService>();

        // Dashboard
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDashboardService, DashboardService>();

        var aiBaseUrl = config["AiServer:BaseUrl"];

        if (string.IsNullOrWhiteSpace(aiBaseUrl))
        {
            throw new InvalidOperationException("Missing configuration: AiServer:BaseUrl");
        }

        services.AddHttpClient<IAiClient, PythonAiClient>(client =>
        {
            client.BaseAddress = new Uri(aiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}