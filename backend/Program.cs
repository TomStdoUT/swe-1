using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PollSystem.Repositories;
using PollSystem.Repositories.Interfaces;
using PollSystem.Services;
using PollSystem.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();

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

builder.Services.AddSingleton<IPollRepository, PollRepository>();
builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();
builder.Services.AddSingleton<IUserRepository>(sp => new UserRepository(
    Path.Combine(builder.Environment.ContentRootPath, "users.json")));

builder.Services.AddTransient<IPollService, PollService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<INotificationService, NotificationService>();
builder.Services.AddHostedService<PollBackgroundWorker>();

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var pollRepo = app.Services.GetRequiredService<IPollRepository>();
await pollRepo.LoadStateAsync();

var userRepo = app.Services.GetRequiredService<IUserRepository>();
await userRepo.LoadStateAsync();

app.Run("http://127.0.0.1:5000");