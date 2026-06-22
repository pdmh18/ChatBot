using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Infrastructure.Config;
using WebApi.Extensions;
using WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);
// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);
// JWT Configuration
var jwtKey = GetRequiredConfig(builder.Configuration, "Jwt:SecretKey");
var jwtIssuer = GetRequiredConfig(builder.Configuration, "Jwt:Issuer");
var jwtAudience = GetRequiredConfig(builder.Configuration, "Jwt:Audience");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("JWT secret key must be at least 32 bytes.");
}
// Authentication - JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddSwaggerWithJwt();
// CORS
// Không dùng AllowAnyOrigin cho kiểu doanh nghiệp.
// Chỉ cho phép origin được cấu hình trong appsettings/user-secrets/env.
// Hiện tại Angular chạy ở http://localhost:4200.
var allowedOrigins = builder.Configuration
    .GetSection("AllowedOrigins")
    .GetChildren()
    .Select(x => x.Value)
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .ToArray();

if (allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("Missing AllowedOrigins configuration.");
}

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("ClientApp", p =>
    {
        p.WithOrigins(allowedOrigins!)
         .AllowAnyMethod()
         .AllowAnyHeader();
    });
});

var app = builder.Build();


// Middleware pipeline
// ExceptionMiddleware đặt sớm để bắt lỗi toàn cục.

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
static string GetRequiredConfig(IConfiguration configuration, string key)
{
    var value = configuration[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"Missing required configuration: {key}");
    }

    return value;
}
