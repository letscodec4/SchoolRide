using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.Services;

/// <summary>Marks PENDING bookings as EXPIRED once the driver has not answered in time,
/// so passengers are never left waiting indefinitely.</summary>
public class BookingExpiryService(IServiceScopeFactory scopes, ILogger<BookingExpiryService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = AppClock.Now;
                var lapsed = await db.Bookings
                    .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt < now)
                    .ToListAsync(stoppingToken);
                if (lapsed.Count > 0)
                {
                    foreach (var b in lapsed) b.Status = BookingStatus.Expired;
                    await db.SaveChangesAsync(stoppingToken);
                    log.LogInformation("Expired {Count} unanswered booking request(s).", lapsed.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Booking expiry sweep failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
