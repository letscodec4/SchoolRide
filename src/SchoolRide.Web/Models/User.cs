using System.ComponentModel.DataAnnotations;

namespace SchoolRide.Web.Models;

public class User
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string FullName { get; set; } = "";

    [Required, StringLength(150)]
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    [Required, StringLength(120)]
    public string Institution { get; set; } = "";

    [Required, StringLength(30)]
    public string StudentId { get; set; } = "";

    [StringLength(20)]
    public string? Phone { get; set; }

    public UserRole Role { get; set; } = UserRole.Passenger;

    /// <summary>Shortcut copy of the latest Verification result so ride queries stay simple.
    /// The Verification table remains the source of truth.</summary>
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.NotSubmitted;

    public DateTime CreatedAt { get; set; } = AppClock.Now;

    public List<Verification> Verifications { get; set; } = new();
    public List<Ride> RidesPosted { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();

    public bool CanDrive => Role is UserRole.Driver or UserRole.Both;
    public bool CanRide => Role is UserRole.Passenger or UserRole.Both;
}
