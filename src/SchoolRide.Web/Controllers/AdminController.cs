using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;
using SchoolRide.Web.ViewModels;

namespace SchoolRide.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        // SQLite cannot sum or order decimals in SQL, so money and time maths is done in memory.
        // Fine for a pilot of a few hundred rows.
        var bookings = await db.Bookings.ToListAsync();

        var answered = bookings.Where(b => b.RespondedAt.HasValue).ToList();
        var decided = bookings.Count(b => b.Status != BookingStatus.Pending);
        var lapsed = bookings.Count(b => b.Status == BookingStatus.Expired);
        var completed = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();

        var vm = new AdminDashboardViewModel
        {
            Users = await db.Users.CountAsync(u => u.Role != UserRole.Admin),
            VerifiedUsers = await db.Users.CountAsync(u => u.Role != UserRole.Admin && u.VerificationStatus == VerificationStatus.Approved),
            PendingVerifications = await db.Verifications.CountAsync(v => v.Status == VerificationStatus.Pending),
            OpenRides = await db.Rides.CountAsync(r => r.Status == RideStatus.Open),
            TotalRides = await db.Rides.CountAsync(),
            BookingsByStatus = bookings.GroupBy(b => b.Status).ToDictionary(g => g.Key, g => g.Count()),
            AvgDriverResponseMinutes = answered.Count == 0 ? null
                : Math.Round(answered.Average(b => (b.RespondedAt!.Value - b.CreatedAt).TotalMinutes), 1),
            LapseRatePercent = decided == 0 ? null : Math.Round(100.0 * lapsed / decided, 1),
            CompletedFares = completed.Sum(b => b.FareAmount),
            PlatformRevenue = completed.Sum(b => b.PlatformFee)
        };
        return View(vm);
    }
}
