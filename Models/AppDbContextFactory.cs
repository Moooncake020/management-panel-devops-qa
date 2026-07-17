using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace YonetimPaneli.Models
{
    /// <summary>
    /// Entity Framework komutları (Update-Database, Add-Migration vb.) için
    /// tasarım zamanı DbContext oluşturur. Eski migration zincirindeki yalnızca
    /// metadata kaynaklı model uyarısı veritabanı kurulumunu engellemesin diye
    /// PendingModelChangesWarning burada bastırılır. Uygulamanın normal çalışma
    /// zamanı DbContext ayarları Program.cs içinde kalır.
    /// </summary>
    public sealed class AppDbContextFactory
        : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();

            // Komut solution kökünden çalıştırılırsa appsettings dosyası
            // yonetimpaneli alt klasöründedir.
            var projectPath = File.Exists(Path.Combine(basePath, "appsettings.json"))
                ? basePath
                : Path.Combine(basePath, "yonetimpaneli");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(projectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration
                .GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "DefaultConnection bağlantı cümlesi bulunamadı.");

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder
                .UseSqlServer(connectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(
                    RelationalEventId.PendingModelChangesWarning));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
