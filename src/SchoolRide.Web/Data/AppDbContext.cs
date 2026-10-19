using Microsoft.EntityFrameworkCore;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Verification> Verifications => Set<Verification>();
    public DbSet<Ride> Rides => Set<Ride>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Rating> Ratings => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => new { u.Institution, u.StudentId }).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
            e.Property(u => u.VerificationStatus).HasConversion<string>();
        });

        b.Entity<Verification>(e =>
        {
            e.Property(v => v.Method).HasConversion<string>();
            e.Property(v => v.Status).HasConversion<string>();
        });

        b.Entity<Ride>(e =>
        {
            e.Property(r => r.Origin).HasConversion<string>();
            e.Property(r => r.Destination).HasConversion<string>();
            e.Property(r => r.Status).HasConversion<string>();
        });

        b.Entity<Booking>(e => e.Property(x => x.Status).HasConversion<string>());

        b.Entity<Rating>(e =>
        {
            e.HasIndex(r => new { r.BookingId, r.RaterId }).IsUnique();
            e.HasOne(r => r.Rater).WithMany().HasForeignKey(r => r.RaterId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.RatedUser).WithMany().HasForeignKey(r => r.RatedUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
