using Microsoft.Extensions.Diagnostics.HealthChecks;
using YonetimPaneli.Models;

namespace YonetimPaneli.HealthChecks
{
    /// <summary>
    /// Uygulamanın SQL Server bağlantısını gerçek bir bağlantı denemesiyle
    /// kontrol eder. /saglik endpointi bu sonucu kullanır.
    /// </summary>
    public class VeritabaniSaglikKontrolu : IHealthCheck
    {
        private readonly AppDbContext _context;

        public VeritabaniSaglikKontrolu(AppDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var baglanabiliyorMu = await _context.Database
                    .CanConnectAsync(cancellationToken);

                return baglanabiliyorMu
                    ? HealthCheckResult.Healthy("Veritabanı bağlantısı kullanılabilir.")
                    : HealthCheckResult.Unhealthy("Veritabanına bağlanılamadı.");
            }
            catch (Exception exception)
            {
                return HealthCheckResult.Unhealthy(
                    "Veritabanı sağlık kontrolü başarısız oldu.",
                    exception);
            }
        }
    }
}
