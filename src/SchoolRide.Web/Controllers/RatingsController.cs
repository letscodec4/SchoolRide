using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;
using SchoolRide.Web.Services;
using SchoolRide.Web.ViewModels;

namespace SchoolRide.Web.Controllers;

[Authorize(Roles = "Passenger,Driver,Both")]
public class RatingsController(AppDbContext db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int bookingId)
    {
        var (booking, otherId, error) = await LoadAsync(bookingId);
        if (booking == null) return error!;
        var other = await db.Users.FirstAsync(u => u.Id == otherId);
        return View(new RatingViewModel
        {
            BookingId = bookingId,
            OtherName = other.FullName,
            RouteText = $"{booking.Ride.Origin.DisplayName()} to {booking.Ride.Destination.DisplayName()}"
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RatingViewModel vm)
    {
        var (booking, otherId, error) = await LoadAsync(vm.BookingId);
        if (booking == null) return error!;
        if (!ModelState.IsValid) return View(vm);

        db.Ratings.Add(new Rating
        {
            BookingId = vm.BookingId,
            RaterId = User.UserId(),
            RatedUserId = otherId,
            Score = vm.Score,
            Comment = vm.Comment?.Trim()
        });
        await db.SaveChangesAsync();

        TempData["Success"] = "Thanks. Your rating helps keep SchoolRide trustworthy.";
        return RedirectToAction(booking.Ride.DriverId == User.UserId() ? "Requests" : "Mine", "Bookings");
    }

    /// <summary>Loads the booking and works out who is being rated. Only the two people on a completed booking may rate, once each.</summary>
    private async Task<(Booking? booking, int otherId, IActionResult? error)> LoadAsync(int bookingId)
    {
        var me = User.UserId();
        var booking = await db.Bookings.Include(b => b.Ride).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return (null, 0, NotFound());

        var isPassenger = booking.PassengerId == me;
        var isDriver = booking.Ride.DriverId == me;
        if (!isPassenger && !isDriver) return (null, 0, Forbid());

        if (booking.Status != BookingStatus.Completed)
        {
            TempData["Warning"] = "You can rate once the trip is marked complete.";
            return (null, 0, RedirectToAction("Index", "Home"));
        }

        if (await db.Ratings.AnyAsync(r => r.BookingId == bookingId && r.RaterId == me))
        {
            TempData["Warning"] = "You have already rated this trip.";
            return (null, 0, RedirectToAction("Index", "Home"));
        }

        return (booking, isPassenger ? booking.Ride.DriverId : booking.PassengerId, null);
    }
}
