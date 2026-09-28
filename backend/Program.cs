using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PollSystem.Domain; // Connects to your Models

var builder = WebApplication.CreateBuilder(args);

// Enable CORS so React can connect
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

// Enable Controller Architecture
builder.Services.AddControllers();

// Register the memory state as Singletons
// This ensures every controller shares the exact same lists and mock user
builder.Services.AddSingleton<PollManager>();
builder.Services.AddSingleton<PostManager>();
builder.Services.AddSingleton<User>(new User("Povilas", 100)); // Your mock user

var app = builder.Build();

app.UseCors();

// Map the endpoints from your Controller files
app.MapControllers();

app.Run("http://127.0.0.1:5000");