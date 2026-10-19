using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;
using SchoolRide.Web.Services;
using SchoolRide.Web.ViewModels;

namespace SchoolRide.Web.Controllers;

[Authorize(Roles = "Passenger,Driver,Both")]
public class BookingsController(AppDbContext db, FareService fares) : Controller
{
    // ---------- Passenger ----------
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Passenger,Both")]
    [RequireVerified]
    public async Task<IActionResult> Create(int rideId, int seats)
    {
        var me = User.UserId();
        var ride = await db.Rides.FirstOrDefaultAsync(r => r.Id == rideId);
        if (ride == null) return NotFound();

        string? problem = null;
        if (ride.DriverId == me) problem = "You cannot book your own ride.";
        else if (ride.Status != RideStatus.Open || ride.DepartureTime <= AppClock.Now) problem = "This ride is no longer open.";
        else if (seats < 1 || seats > ride.SeatsAvailable) problem = "That many seats are not available.";
        else if (await db.Bookings.AnyAsync(b => b.RideId == rideId && b.PassengerId == me &&
                     (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted)))
            problem = "You already have a request on this ride.";

        if (problem != null)
        {
            TempData["Warning"] = problem;
            return RedirectToAction("Details", "Rides", new { id = rideId });
        }

        var quote = fares.Quote(ride.PricePerSeat, seats);
        db.Bookings.Add(new Booking
        {
            RideId = rideId,
            PassengerId = me,
            SeatsRequested = seats,
            FareAmount = quote.Total,
            PlatformFee = quote.PlatformFee,
            ExpiresAt = AppClock.Now.AddMinutes(fares.BookingTimeoutMinutes)
        });
        await db.SaveChangesAsync();

        TempData["Success"] = $"Request sent. If the driver does not answer within {fares.BookingTimeoutMinutes} minutes it lapses and you can try another ride.";
        return RedirectToAction(nameof(Mine));
    }

    [Authorize(Roles = "Passenger,Both")]
    public async Task<IActionResult> Mine()
    {
        var me = User.UserId();
        var list = await db.Bookings
            .Include(b => b.Ride).ThenInclude(r => r.Driver)
            .Where(b => b.PassengerId == me)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return View(new BookingListViewModel { Bookings = list, RatedByMe = await RatedByAsync(me) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Passenger,Both")]
    public async Task<IActionResult> Cancel(int id)
    {
        var me = User.UserId();
        var b = await db.Bookings.Include(x => x.Ride).FirstOrDefaultAsync(x => x.Id == id && x.PassengerId == me);
        if (b == null) return NotFound();

        if (b.Status is not (BookingStatus.Pending or BookingStatus.Accepted))
        {
            TempData["Warning"] = "This booking can no longer be cancelled.";
            return RedirectToAction(nameof(Mine));
        }

        if (b.Status == BookingStatus.Accepted && b.Ride.Status == RideStatus.Open)
            b.Ride.SeatsAvailable += b.SeatsRequested;
        b.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();

        TempData["Success"] = "Booking cancelled.";
        return RedirectToAction(nameof(Mine));
    }

    // ---------- Driver ----------
    [Authorize(Roles = "Driver,Both")]
    public async Task<IActionResult> Requests()
    {
        var me = User.UserId();
        var list = await db.Bookings
            .Include(b => b.Ride)
            .Include(b => b.Passenger)
            .Where(b => b.Ride.DriverId == me)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        // Pending requests first, then newest.
        list = list.OrderBy(b => b.Status == BookingStatus.Pending ? 0 : 1).ThenByDescending(b => b.CreatedAt).ToList();
        return View(new BookingListViewModel { Bookings = list, RatedByMe = await RatedByAsync(me) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Driver,Both")]
    public async Task<IActionResult> Respond(int id, bool accept)
    {
        var me = User.UserId();
        var b = await db.Bookings.Include(x => x.Ride).FirstOrDefaultAsync(x => x.Id == id && x.Ride.DriverId == me);
        if (b == null) return NotFound();

        if (b.Status != BookingStatus.Pending || b.ExpiresAt < AppClock.Now)
        {
            TempData["Warning"] = "This request is no longer pending.";
            return RedirectToAction(nameof(Requests));
        }

        if (accept)
        {
            if (b.Ride.Status != RideStatus.Open || b.Ride.SeatsAvailable < b.SeatsRequested)
            {
                TempData["Warning"] = "Not enough seats left on this ride.";
                return RedirectToAction(nameof(Requests));
            }
            b.Ride.SeatsAvailable -= b.SeatsRequested;
            b.Status = BookingStatus.Accepted;
        }
        else
        {
            b.Status = BookingStatus.Rejected;
        }

        b.RespondedAt = AppClock.Now;
        await db.SaveChangesAsync();

        TempData["Success"] = accept ? "Request accepted." : "Request declined.";
        return RedirectToAction(nameof(Requests));
    }

    private async Task<HashSet<int>> RatedByAsync(int userId) =>
        (await db.Ratings.Where(r => r.RaterId == userId).Select(r => r.BookingId).ToListAsync()).ToHashSet();
}
