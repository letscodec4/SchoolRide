using System.ComponentModel.DataAnnotations;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.ViewModels;

public class RegisterViewModel
{
    [Required, StringLength(100), Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";

    [Required]
    public string Institution { get; set; } = "";

    [Required, StringLength(30), Display(Name = "Student ID number")]
    public string StudentId { get; set; } = "";

    [Phone, StringLength(20), Display(Name = "Phone (optional)")]
    public string? Phone { get; set; }

    [Display(Name = "I want to")]
    public UserRole Role { get; set; } = UserRole.Passenger;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class VerificationSubmitViewModel
{
    [Display(Name = "Verification method")]
    public VerificationMethod Method { get; set; } = VerificationMethod.StudentIdCard;

    [Required, StringLength(120)]
    [Display(Name = "Reference (ID number, portal username, phone or institutional email)")]
    public string Reference { get; set; } = "";
}

public class VerificationIndexViewModel
{
    public VerificationStatus Status { get; set; }
    public List<Verification> History { get; set; } = new();
    public VerificationSubmitViewModel Form { get; set; } = new();
}
