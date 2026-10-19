using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SchoolRide.Web.Models;

public enum UserRole { Passenger, Driver, Both, Admin }

public enum VerificationMethod
{
    [Display(Name = "Student ID card")] StudentIdCard,
    [Display(Name = "Student portal login")] PortalLogin,
    [Display(Name = "Phone number and ID upload")] PhoneAndIdUpload,
    [Display(Name = "Institutional email")] InstitutionalEmail
}

public enum VerificationStatus { NotSubmitted, Pending, Approved, Rejected }

public enum RideStatus { Open, Completed, Cancelled }

public enum BookingStatus { Pending, Accepted, Rejected, Expired, Cancelled, Completed }

/// <summary>
/// Coded pick-up / drop-off locations. A fixed list instead of free text makes
/// "compatible ride" a simple lookup (design review, section 2).
/// The first group are the home areas most survey respondents came from; the second group are campuses.
/// </summary>
public enum Place
{
    [Display(Name = "Mogoditshane")] Mogoditshane,
    [Display(Name = "Gabane")] Gabane,
    [Display(Name = "Phakalane")] Phakalane,
    [Display(Name = "Gaborone West")] GaboroneWest,
    [Display(Name = "Tlokweng")] Tlokweng,
    [Display(Name = "Broadhurst")] Broadhurst,
    [Display(Name = "Block 8")] Block8,
    [Display(Name = "Block 9")] Block9,

    [Display(Name = "University of Botswana")] UniversityOfBotswana,
    [Display(Name = "Botswana Accountancy College")] BAC,
    [Display(Name = "Botho University")] BothoUniversity,
    [Display(Name = "Gaborone University College of Law & Professional Studies")] GUCLPS,
    [Display(Name = "Botswana University of Agriculture & Natural Resources")] BUAN,
    [Display(Name = "Ba Isago University")] BaIsago
}

public static class EnumExtensions
{
    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }

    public static bool IsCampus(this Place p) => p >= Place.UniversityOfBotswana;

    /// <summary>Rough distance band from central Gaborone (0 = close, 3 = far). Used only for fare suggestions.</summary>
    public static int Zone(this Place p) => p switch
    {
        Place.GaboroneWest or Place.Block8 or Place.Block9 or Place.Broadhurst => 1,
        Place.Tlokweng or Place.Mogoditshane => 2,
        Place.Phakalane or Place.Gabane => 3,
        Place.BUAN => 1,
        _ => 0
    };

    public static string BadgeClass(this BookingStatus s) => s switch
    {
        BookingStatus.Pending => "text-bg-warning",
        BookingStatus.Accepted => "text-bg-success",
        BookingStatus.Completed => "text-bg-primary",
        BookingStatus.Rejected => "text-bg-danger",
        _ => "text-bg-secondary"
    };

    public static string BadgeClass(this VerificationStatus s) => s switch
    {
        VerificationStatus.Approved => "text-bg-success",
        VerificationStatus.Pending => "text-bg-warning",
        VerificationStatus.Rejected => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}

public static class Institutions
{
    public static readonly string[] All =
    {
        "University of Botswana",
        "Botswana Accountancy College (BAC)",
        "Botho University",
        "Gaborone University College of Law & Professional Studies",
        "Botswana University of Agriculture and Natural Resources",
        "Botswana International University of Science and Technology",
        "Ba Isago University",
        "Other"
    };
}

/// <summary>Botswana does not observe daylight saving, so Central Africa Time is a fixed UTC+2.</summary>
public static class AppClock
{
    public static DateTime Now => DateTime.UtcNow.AddHours(2);
}
