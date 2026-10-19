using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;
using SchoolRide.Web.ViewModels;

namespace SchoolRide.Web.Controllers;

public class AccountController(AppDbContext db, IPasswordHasher<User> hasher) : Controller
{
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm)
    {
        if (vm.Role == UserRole.Admin)
            ModelState.AddModelError(nameof(vm.Role), "Please choose passenger, driver or both.");

        var email = vm.Email.Trim().ToLowerInvariant();
        var studentId = vm.StudentId.Trim();

        if (await db.Users.AnyAsync(u => u.Email == email))
            ModelState.AddModelError(nameof(vm.Email), "An account with this email already exists.");
        if (await db.Users.AnyAsync(u => u.Institution == vm.Institution && u.StudentId == studentId))
            ModelState.AddModelError(nameof(vm.StudentId), "This student ID is already registered at that institution.");

        if (!ModelState.IsValid) return View(vm);

        var user = new User
        {
            FullName = vm.FullName.Trim(),
            Email = email,
            Institution = vm.Institution,
            StudentId = studentId,
            Phone = vm.Phone?.Trim(),
            Role = vm.Role
        };
        user.PasswordHash = hasher.HashPassword(user, vm.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await SignInAsync(user);
        TempData["Success"] = "Welcome to SchoolRide! Verify your student status to start riding or driving.";
        return RedirectToAction("Index", "Verification");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var email = vm.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        var ok = user != null &&
                 hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) != PasswordVerificationResult.Failed;
        if (!ok)
        {
            ModelState.AddModelError("", "Email or password is incorrect.");
            return View(vm);
        }

        await SignInAsync(user!);
        if (!string.IsNullOrEmpty(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl)) return Redirect(vm.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    private Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
