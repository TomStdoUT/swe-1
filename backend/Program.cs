using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PollSystem.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
        options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddControllers();

builder.Services.AddSingleton<PollManager>();
builder.Services.AddSingleton<PostManager>();
builder.Services.AddSingleton(_ => new UserManager(
    Path.Combine(builder.Environment.ContentRootPath, "users.json")
));

var app = builder.Build();

app.UseCors();

app.MapControllers();

var pollManager = app.Services.GetRequiredService<PollManager>();
await pollManager.LoadStateAsync();
var userManager = app.Services.GetRequiredService<UserManager>();
await userManager.LoadStateAsync();

app.Run("http://127.0.0.1:5000");
