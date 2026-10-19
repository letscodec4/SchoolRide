using System.ComponentModel.DataAnnotations;

namespace SchoolRide.Web.Models;

/// <summary>A completed booking collects at most two ratings: passenger to driver and driver to passenger.</summary>
public class Rating
{
    public int Id { get; set; }
    public int BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public int RaterId { get; set; }
    public User Rater { get; set; } = null!;
    public int RatedUserId { get; set; }
    public User RatedUser { get; set; } = null!;

    [Range(1, 5)]
    public int Score { get; set; }

    [StringLength(300)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = AppClock.Now;
}
