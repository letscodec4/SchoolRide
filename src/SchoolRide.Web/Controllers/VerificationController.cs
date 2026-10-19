using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;
using SchoolRide.Web.Services;
using SchoolRide.Web.ViewModels;

namespace SchoolRide.Web.Controllers;

[Authorize]
public class VerificationController(AppDbContext db) : Controller
{
    [Authorize(Roles = "Passenger,Driver,Both")]
    public async Task<IActionResult> Index() => View(await BuildIndexAsync(new VerificationSubmitViewModel()));

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Passenger,Driver,Both")]
    public async Task<IActionResult> Submit([Bind(Prefix = "Form")] VerificationSubmitViewModel form)
    {
        var userId = User.UserId();
        var user = await db.Users.FirstAsync(u => u.Id == userId);

        if (user.VerificationStatus is VerificationStatus.Approved or VerificationStatus.Pending)
        {
            TempData["Warning"] = user.VerificationStatus == VerificationStatus.Approved
                ? "You are already verified."
                : "Your verification is already waiting for review.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid) return View("Index", await BuildIndexAsync(form));

        db.Verifications.Add(new Verification
        {
            UserId = userId,
            Method = form.Method,
            Reference = form.Reference.Trim()
        });
        user.VerificationStatus = VerificationStatus.Pending;
        await db.SaveChangesAsync();

        TempData["Success"] = "Verification submitted. You will be able to ride or drive as soon as it is approved.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Admin review ----------
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Review()
    {
        var pending = await db.Verifications
            .Include(v => v.User)
            .Where(v => v.Status == VerificationStatus.Pending)
            .OrderBy(v => v.SubmittedAt)
            .ToListAsync();
        return View(pending);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Decide(int id, bool approve, string? note)
    {
        var v = await db.Verifications.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
        if (v == null || v.Status != VerificationStatus.Pending) return NotFound();

        v.Status = approve ? VerificationStatus.Approved : VerificationStatus.Rejected;
        v.VerifiedAt = AppClock.Now;
        v.ReviewedById = User.UserId();
        v.ReviewerNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        v.User.VerificationStatus = v.Status;
        await db.SaveChangesAsync();

        TempData["Success"] = $"{v.User.FullName} was {(approve ? "approved" : "rejected")}.";
        return RedirectToAction(nameof(Review));
    }

    private async Task<VerificationIndexViewModel> BuildIndexAsync(VerificationSubmitViewModel form)
    {
        var id = User.UserId();
        return new VerificationIndexViewModel
        {
            Status = await db.Users.Where(u => u.Id == id).Select(u => u.VerificationStatus).FirstAsync(),
            History = await db.Verifications.Where(v => v.UserId == id).OrderByDescending(v => v.SubmittedAt).ToListAsync(),
            Form = form
        };
    }
}
