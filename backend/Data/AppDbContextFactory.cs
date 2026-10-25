using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PollSystem.Data
{
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var backendDirectory = File.Exists(Path.Combine(currentDirectory, "swe-1.csproj"))
                ? currentDirectory
                : Path.Combine(currentDirectory, "backend");
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(backendDirectory, "swe-1.db")}");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
