# SchoolRide

A student-only ride-sharing website for Gaborone's tertiary institutions, built with C# on ASP.NET Core 8 (MVC), Entity Framework Core and SQLite.

## Run it

You need the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (VS Code with the C# extension, or Visual Studio 2022).

```bash
cd src/SchoolRide.Web
dotnet run
```

Open the HTTPS address printed in the terminal. The SQLite database (`schoolride.db`) is created on first run.

Run the unit tests from the solution folder with `dotnet test`.

### Demo accounts (Development mode only)

| Role | Email | Password |
| --- | --- | --- |
| Admin | admin@schoolride.local | Admin#12345 |
| Driver (verified) | thato@demo.local | Password#123 |
| Passenger (verified) | neo@demo.local | Password#123 |

Change the admin login in `appsettings.json` before showing the site to anyone else. Demo data comes from `appsettings.Development.json`.

### Try the full flow

1. Sign up as a passenger and a driver (two browsers, or one normal and one private window).
2. Submit verification for each, then log in as admin and approve them under *Review verifications*.
3. Driver: *Post a ride*. Passenger: *Find a ride*, open it, *Send request*.
4. Driver: *Requests*, accept. Driver: *My rides*, *Mark trip complete*.
5. Both sides rate each other. The admin dashboard shows driver response time, lapsed requests and commission.

## Project layout

```
SchoolRide/
  SchoolRide.sln
  src/SchoolRide.Web/
    Program.cs                  Startup, cookie login, services
    Models/                     User, Verification, Ride, Booking, Rating, enums (match the ERD)
    Data/                       AppDbContext, DbSeeder
    Services/                   FareService, BookingExpiryService, verified-only filter
    Controllers/                Account, Verification, Rides, Bookings, Ratings, Admin, Home
    ViewModels/                 Form and page models
    Views/                      Razor pages (Bootstrap 5 from a CDN)
    wwwroot/css/site.css
  tests/SchoolRide.Tests/       xUnit tests for fare and place logic
```

## How the code follows the design review

| Design decision | Where it lives |
| --- | --- |
| Verification built into sign-up; nobody rides or posts unverified | `RequireVerifiedAttribute`, `VerificationController` |
| Verification as its own entity with a method and status | `Verification` model; methods follow the survey (ID card first) |
| Ratings in both directions, at most two per booking | `Rating` model, unique index on (booking, rater) |
| Fare shown before booking, capped well under a taxi (survey mean P44.30) | `FareService`, `Fare` section of `appsettings.json` |
| Coded corridors instead of free-text locations | `Place` enum (Mogoditshane, Gabane, Phakalane, Gaborone West, Tlokweng and campuses) |
| Driver sets a maximum detour | `Ride.MaxDetourMinutes` |
| PENDING bookings need a timeout | `Booking.ExpiresAt`, `BookingExpiryService` (default 30 min) |
| Log driver response time and lapsed bookings | `Booking.RespondedAt`, admin dashboard |
| Commission revenue model (ToR deliverable) | `FareService` platform fee (10%), shown on the dashboard |

## Known limits of this prototype

- **Payments are not integrated.** Passengers pay the driver directly; the platform fee is calculated and reported but not collected.
- **Verification is manual.** An admin approves each submission. Nothing checks IDs with an institution, which the review marked as unconfirmed.
- **Search is a database lookup** over posted rides, not the dynamic matching in Yao and Bekhor (2021). Say so in the report.
- **No on-demand request or saved regular journey.** The review recommends adding one or recording why version 1 leaves it out.
- **No email or SMS notifications.** Drivers see requests on the Requests page.
- The suggested fare is a simple distance-band rule to calibrate with pilot data.
- Uses `EnsureCreated`. Switch to EF migrations if the schema will change after real users exist.
- Road traffic, insurance and licensing compliance for student drivers is out of scope, as the ToR states.
