using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;
using SchoolRide.Web.Services;
using SchoolRide.Web.ViewModels;

namespace SchoolRide.Web.Controllers;

[Authorize(Roles = "Passenger,Driver,Both")]
public class RidesController(AppDbContext db, FareService fares) : Controller
{
    // ---------- Search (passenger) ----------
    [RequireVerified]
    public async Task<IActionResult> Index([FromQuery] RideSearchViewModel vm)
    {
        var now = AppClock.Now;
        var me = User.UserId();
        var q = db.Rides.Include(r => r.Driver)
            .Where(r => r.Status == RideStatus.Open && r.SeatsAvailable > 0 && r.DepartureTime > now && r.DriverId != me
                        && r.Driver.VerificationStatus == VerificationStatus.Approved);

        if (vm.Origin.HasValue) { var o = vm.Origin.Value; q = q.Where(r => r.Origin == o); }
        if (vm.Destination.HasValue) { var d = vm.Destination.Value; q = q.Where(r => r.Destination == d); }
        if (vm.Date.HasValue)
        {
            var start = vm.Date.Value.Date;
            var end = start.AddDays(1);
            q = q.Where(r => r.DepartureTime >= start && r.DepartureTime < end);
        }

        vm.Results = await q.OrderBy(r => r.DepartureTime).Take(100).ToListAsync();
        vm.DriverRatings = await RatingSummariesAsync(vm.Results.Select(r => r.DriverId).Distinct().ToList());
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ride = await db.Rides.Include(r => r.Driver).FirstOrDefaultAsync(r => r.Id == id);
        if (ride == null) return NotFound();

        var me = User.UserId();
        var meUser = await db.Users.FirstAsync(u => u.Id == me);
        var vm = new RideDetailsViewModel
        {
            Ride = ride,
            DriverRating = (await RatingSummariesAsync(new List<int> { ride.DriverId })).GetValueOrDefault(ride.DriverId),
            PlatformFeeRate = 0.10m
        };

        if (!meUser.CanRide) vm.CannotBookReason = "Your account is set up as a driver only.";
        else if (meUser.VerificationStatus != VerificationStatus.Approved) vm.CannotBookReason = "Verify your student status to book.";
        else if (ride.DriverId == me) vm.CannotBookReason = "This is your own ride.";
        else if (ride.Status != RideStatus.Open || ride.DepartureTime <= AppClock.Now) vm.CannotBookReason = "This ride is no longer open.";
        else if (ride.SeatsAvailable < 1) vm.CannotBookReason = "No seats left.";
        else if (await db.Bookings.AnyAsync(b => b.RideId == id && b.PassengerId == me &&
                     (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted)))
            vm.CannotBookReason = "You already have a request on this ride.";
        else vm.CanBook = true;

        return View(vm);
    }

    // ---------- Post a ride (driver) ----------
    [Authorize(Roles = "Driver,Both")]
    [RequireVerified]
    public IActionResult Create()
    {
        ViewBag.Min = fares.MinPrice; ViewBag.Max = fares.MaxPrice;
        var vm = new RideCreateViewModel();
        vm.PricePerSeat = fares.Suggest(vm.Origin, vm.Destination);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Driver,Both")]
    [RequireVerified]
    public async Task<IActionResult> Create(RideCreateViewModel vm)
    {
        if (vm.Origin == vm.Destination)
            ModelState.AddModelError(nameof(vm.Destination), "Origin and destination must be different.");
        if (!vm.Origin.IsCampus() && !vm.Destination.IsCampus())
            ModelState.AddModelError(nameof(vm.Destination), "SchoolRide is for trips to or from a campus. Choose a campus as one end.");
        if (vm.DepartureTime <= AppClock.Now)
            ModelState.AddModelError(nameof(vm.DepartureTime), "Departure must be in the future.");
        if (!fares.IsPriceAllowed(vm.PricePerSeat))
            ModelState.AddModelError(nameof(vm.PricePerSeat), $"Price per seat must be between P{fares.MinPrice:0.00} and P{fares.MaxPrice:0.00}.");

        if (!ModelState.IsValid)
        {
            ViewBag.Min = fares.MinPrice; ViewBag.Max = fares.MaxPrice;
            return View(vm);
        }

        var ride = new Ride
        {
            DriverId = User.UserId(),
            Origin = vm.Origin,
            Destination = vm.Destination,
            PickupNote = vm.PickupNote?.Trim(),
            DepartureTime = vm.DepartureTime,
            SeatsTotal = vm.Seats,
            SeatsAvailable = vm.Seats,
            PricePerSeat = Math.Round(vm.PricePerSeat, 2),
            MaxDetourMinutes = vm.MaxDetourMinutes
        };
        db.Rides.Add(ride);
        await db.SaveChangesAsync();

        TempData["Success"] = "Your ride is posted.";
        return RedirectToAction(nameof(Mine));
    }

    /// <summary>Fare suggestion used by the Post a ride form.</summary>
    [HttpGet]
    public IActionResult Suggest(Place origin, Place destination) => Json(new { price = fares.Suggest(origin, destination) });

    // ---------- Driver: my rides ----------
    [Authorize(Roles = "Driver,Both")]
    public async Task<IActionResult> Mine()
    {
        var me = User.UserId();
        var rides = await db.Rides
            .Include(r => r.Bookings).ThenInclude(b => b.Passenger)
            .Where(r => r.DriverId == me)
            .OrderByDescending(r => r.DepartureTime)
            .ToListAsync();
        return View(rides);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Driver,Both")]
    public async Task<IActionResult> Cancel(int id)
    {
        var me = User.UserId();
        var ride = await db.Rides.Include(r => r.Bookings).FirstOrDefaultAsync(r => r.Id == id && r.DriverId == me);
        if (ride == null) return NotFound();
        if (ride.Status != RideStatus.Open)
        {
            TempData["Warning"] = "Only open rides can be cancelled.";
            return RedirectToAction(nameof(Mine));
        }

        ride.Status = RideStatus.Cancelled;
        foreach (var b in ride.Bookings.Where(b => b.Status is BookingStatus.Pending or BookingStatus.Accepted))
            b.Status = BookingStatus.Cancelled;
        await db.SaveChangesAsync();

        TempData["Success"] = "Ride cancelled. Passengers with requests on it have been marked as cancelled.";
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Driver,Both")]
    public async Task<IActionResult> Complete(int id)
    {
        var me = User.UserId();
        var ride = await db.Rides.Include(r => r.Bookings).FirstOrDefaultAsync(r => r.Id == id && r.DriverId == me);
        if (ride == null) return NotFound();
        if (ride.Status != RideStatus.Open)
        {
            TempData["Warning"] = "This ride is already closed.";
            return RedirectToAction(nameof(Mine));
        }

        ride.Status = RideStatus.Completed;
        foreach (var b in ride.Bookings)
        {
            if (b.Status == BookingStatus.Accepted) b.Status = BookingStatus.Completed;
            else if (b.Status == BookingStatus.Pending) b.Status = BookingStatus.Expired;
        }
        await db.SaveChangesAsync();

        TempData["Success"] = "Trip marked complete. Remember to rate your passengers.";
        return RedirectToAction(nameof(Mine));
    }

    private async Task<Dictionary<int, RatingSummary>> RatingSummariesAsync(List<int> userIds)
    {
        var rows = await db.Ratings
            .Where(r => userIds.Contains(r.RatedUserId))
            .GroupBy(r => r.RatedUserId)
            .Select(g => new { UserId = g.Key, Avg = g.Average(r => r.Score), Count = g.Count() })
            .ToListAsync();
        return rows.ToDictionary(x => x.UserId, x => new RatingSummary(Math.Round(x.Avg, 1), x.Count));
    }
}
