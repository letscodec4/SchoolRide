using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Data;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.Services;

public static class ClaimsExtensions
{
    public static int UserId(this ClaimsPrincipal p) => int.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

/// <summary>Blocks the action until the signed-in student has an approved verification.
/// Verification is a hard gate for posting, searching and booking (design review, section 1).</summary>
public class RequireVerifiedAttribute : TypeFilterAttribute
{
    public RequireVerifiedAttribute() : base(typeof(RequireVerifiedFilter)) { }

    public class RequireVerifiedFilter(AppDbContext db) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var principal = context.HttpContext.User;
            if (principal.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            var id = principal.UserId();
            var status = await db.Users.Where(u => u.Id == id).Select(u => u.VerificationStatus).FirstOrDefaultAsync();
            if (status != VerificationStatus.Approved)
            {
                if (context.Controller is Controller c)
                    c.TempData["Warning"] = "Please verify your student status first. It only takes a minute.";
                context.Result = new RedirectToActionResult("Index", "Verification", null);
                return;
            }

            await next();
        }
    }
}
