using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using PollSystem.Data;
using PollSystem.Repositories;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services;
using PollSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();

var databasePath = Path.Combine(builder.Environment.ContentRootPath, "swe-1.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddScoped<LegacyJsonImporter>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "a_very_long_and_secure_secret_key_for_swe_1_project_12345!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPollService, PollService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<PollBackgroundWorker>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var legacyImporter = scope.ServiceProvider.GetRequiredService<LegacyJsonImporter>();
    await legacyImporter.ImportIfDatabaseIsEmptyAsync();
}

app.Run("http://127.0.0.1:5000");
