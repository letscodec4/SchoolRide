namespace SchoolRide.Web.Models;

public class Booking
{
    public int Id { get; set; }
    public int RideId { get; set; }
    public Ride Ride { get; set; } = null!;
    public int PassengerId { get; set; }
    public User Passenger { get; set; } = null!;

    public int SeatsRequested { get; set; } = 1;

    /// <summary>Total the passenger pays for all seats.</summary>
    public decimal FareAmount { get; set; }
    public decimal PlatformFee { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = AppClock.Now;

    /// <summary>A pending request lapses at this time (design review: PENDING needs a timeout).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Set when the driver accepts or rejects. Used to report driver response time.</summary>
    public DateTime? RespondedAt { get; set; }

    public List<Rating> Ratings { get; set; } = new();
}
