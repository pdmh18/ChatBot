using System;
using Application.Features.Auth;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Application.Features.Lookups;
namespace Infrastructure.Config;
using Application.Features.Lookups;
using Infrastructure.Persistence.Repositories;
using Application.Features.Tasks;
public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing connection string 'Default'.");

        services.AddDbContext<QuanLyDuAnAiContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<ITaskService, TaskService>();
        return services;
    }
}