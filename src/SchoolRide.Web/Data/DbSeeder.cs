using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, IConfiguration config)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher<User>>();

        // The prototype uses EnsureCreated for simplicity. Move to EF migrations if the schema must evolve with live data.
        await db.Database.EnsureCreatedAsync();

        var adminEmail = (config["Admin:Email"] ?? "admin@schoolride.local").ToLowerInvariant();
        if (!await db.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var admin = new User
            {
                FullName = "SchoolRide Admin",
                Email = adminEmail,
                Institution = "SchoolRide",
                StudentId = "ADMIN",
                Role = UserRole.Admin,
                VerificationStatus = VerificationStatus.Approved
            };
            admin.PasswordHash = hasher.HashPassword(admin, config["Admin:Password"] ?? "Admin#12345");
            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }

        if (config.GetValue<bool>("SeedDemoData") && !await db.Rides.AnyAsync())
        {
            var driver = NewDemoUser(hasher, "Thato Kgosana", "thato@demo.local", "ict24-901", UserRole.Driver);
            var rider = NewDemoUser(hasher, "Neo Molefe", "neo@demo.local", "ict24-902", UserRole.Passenger);
            db.Users.AddRange(driver, rider);
            await db.SaveChangesAsync();

            var tomorrow = AppClock.Now.Date.AddDays(1);
            db.Rides.AddRange(
                new Ride
                {
                    DriverId = driver.Id, Origin = Place.Mogoditshane, Destination = Place.UniversityOfBotswana,
                    PickupNote = "Mogoditshane main shopping centre", DepartureTime = tomorrow.AddHours(6.75),
                    SeatsTotal = 3, SeatsAvailable = 3, PricePerSeat = 12m, MaxDetourMinutes = 5
                },
                new Ride
                {
                    DriverId = driver.Id, Origin = Place.UniversityOfBotswana, Destination = Place.Mogoditshane,
                    PickupNote = "UB main gate", DepartureTime = tomorrow.AddHours(16.5),
                    SeatsTotal = 3, SeatsAvailable = 3, PricePerSeat = 12m, MaxDetourMinutes = 5
                });
            await db.SaveChangesAsync();
        }
    }

    private static User NewDemoUser(IPasswordHasher<User> hasher, string name, string email, string studentId, UserRole role)
    {
        var u = new User
        {
            FullName = name, Email = email, Institution = "University of Botswana", StudentId = studentId,
            Role = role, VerificationStatus = VerificationStatus.Approved
        };
        u.PasswordHash = hasher.HashPassword(u, "Password#123");
        return u;
    }
}
