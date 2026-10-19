using System.ComponentModel.DataAnnotations;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.ViewModels;

public record RatingSummary(double Average, int Count);

public class RideSearchViewModel
{
    public Place? Origin { get; set; }
    public Place? Destination { get; set; }
    [DataType(DataType.Date)]
    public DateTime? Date { get; set; }

    public List<Ride> Results { get; set; } = new();
    public Dictionary<int, RatingSummary> DriverRatings { get; set; } = new();
}

public class RideCreateViewModel
{
    [Display(Name = "From")]
    public Place Origin { get; set; } = Place.Mogoditshane;

    [Display(Name = "To")]
    public Place Destination { get; set; } = Place.UniversityOfBotswana;

    [StringLength(120), Display(Name = "Pick-up point (optional)")]
    public string? PickupNote { get; set; }

    [Display(Name = "Departure time")]
    public DateTime DepartureTime { get; set; } = AppClock.Now.Date.AddDays(1).AddHours(7);

    [Range(1, 6), Display(Name = "Seats offered")]
    public int Seats { get; set; } = 3;

    [Display(Name = "Price per seat (P)")]
    public decimal PricePerSeat { get; set; } = 10m;

    [Range(0, 30), Display(Name = "Maximum detour (minutes)")]
    public int MaxDetourMinutes { get; set; } = 5;
}

public class RideDetailsViewModel
{
    public Ride Ride { get; set; } = null!;
    public RatingSummary? DriverRating { get; set; }
    public bool CanBook { get; set; }
    public string? CannotBookReason { get; set; }
    public decimal PlatformFeeRate { get; set; }
}

public class BookingListViewModel
{
    public List<Booking> Bookings { get; set; } = new();
    public HashSet<int> RatedByMe { get; set; } = new();
}

public class RatingViewModel
{
    public int BookingId { get; set; }
    public string OtherName { get; set; } = "";
    public string RouteText { get; set; } = "";

    [Range(1, 5, ErrorMessage = "Choose a score from 1 to 5.")]
    public int Score { get; set; } = 5;

    [StringLength(300)]
    public string? Comment { get; set; }
}

public class AdminDashboardViewModel
{
    public int Users { get; set; }
    public int VerifiedUsers { get; set; }
    public int PendingVerifications { get; set; }
    public int OpenRides { get; set; }
    public int TotalRides { get; set; }
    public Dictionary<BookingStatus, int> BookingsByStatus { get; set; } = new();
    public double? AvgDriverResponseMinutes { get; set; }
    public double? LapseRatePercent { get; set; }
    public decimal CompletedFares { get; set; }
    public decimal PlatformRevenue { get; set; }
}
