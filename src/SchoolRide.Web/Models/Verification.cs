namespace SchoolRide.Web.Models;

public class Verification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public VerificationMethod Method { get; set; }

    /// <summary>Student ID number, portal username, phone number or institutional email, depending on Method.</summary>
    public string Reference { get; set; } = "";

    public VerificationStatus Status { get; set; } = VerificationStatus.Pending;
    public DateTime SubmittedAt { get; set; } = AppClock.Now;
    public DateTime? VerifiedAt { get; set; }
    public int? ReviewedById { get; set; }
    public string? ReviewerNote { get; set; }
}
