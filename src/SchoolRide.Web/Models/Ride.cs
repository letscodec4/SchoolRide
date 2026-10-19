namespace SchoolRide.Web.Models;

public class Ride
{
    public int Id { get; set; }
    public int DriverId { get; set; }
    public User Driver { get; set; } = null!;

    public Place Origin { get; set; }
    public Place Destination { get; set; }
    public string? PickupNote { get; set; }

    public DateTime DepartureTime { get; set; }
    public int SeatsTotal { get; set; }
    public int SeatsAvailable { get; set; }

    /// <summary>Price a passenger pays per seat (includes the platform fee).</summary>
    public decimal PricePerSeat { get; set; }

    /// <summary>Longest extra drive, in minutes, the driver will accept to pick someone up.
    /// Yao and Bekhor (2021) found detour tolerance drives match success.</summary>
    public int MaxDetourMinutes { get; set; }

    public RideStatus Status { get; set; } = RideStatus.Open;
    public DateTime CreatedAt { get; set; } = AppClock.Now;

    public List<Booking> Bookings { get; set; } = new();
}
