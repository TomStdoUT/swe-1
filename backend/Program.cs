using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
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

builder.Services.AddSingleton<IPollRepository, PollRepository>();
builder.Services.AddSingleton<INotificationRepository, NotificationRepository>();
builder.Services.AddSingleton<IUserRepository>(sp => new UserRepository(
    Path.Combine(builder.Environment.ContentRootPath, "users.json")));

builder.Services.AddTransient<IPollService, PollService>();
builder.Services.AddTransient<IUserService, UserService>();
builder.Services.AddTransient<INotificationService, NotificationService>();

var app = builder.Build();

app.UseCors();
app.MapControllers();

var pollRepo = app.Services.GetRequiredService<IPollRepository>();
await pollRepo.LoadStateAsync();

var userRepo = app.Services.GetRequiredService<IUserRepository>();
await userRepo.LoadStateAsync();

app.Run("http://127.0.0.1:5000");