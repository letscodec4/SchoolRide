using Microsoft.Extensions.Options;
using SchoolRide.Web.Models;
using SchoolRide.Web.Services;
using Xunit;

namespace SchoolRide.Tests;

public class FareServiceTests
{
    private static FareService Make() => new(Options.Create(new FareOptions()));

    [Fact]
    public void Quote_splits_total_into_platform_fee_and_driver_earnings()
    {
        var q = Make().Quote(12m, 2);
        Assert.Equal(24.00m, q.Total);
        Assert.Equal(2.40m, q.PlatformFee);
        Assert.Equal(21.60m, q.DriverEarnings);
    }

    [Fact]
    public void Price_limits_are_inclusive()
    {
        var f = Make();
        Assert.False(f.IsPriceAllowed(4.99m));
        Assert.True(f.IsPriceAllowed(5m));
        Assert.True(f.IsPriceAllowed(25m));
        Assert.False(f.IsPriceAllowed(25.01m));
    }

    [Fact]
    public void Farther_areas_get_higher_suggestions()
    {
        var f = Make();
        var near = f.Suggest(Place.Block8, Place.UniversityOfBotswana);
        var mid = f.Suggest(Place.Mogoditshane, Place.UniversityOfBotswana);
        var far = f.Suggest(Place.Phakalane, Place.UniversityOfBotswana);
        Assert.True(near < mid && mid < far);
    }

    [Fact]
    public void Suggestions_always_stay_inside_the_allowed_range()
    {
        var f = Make();
        foreach (var a in Enum.GetValues<Place>())
            foreach (var b in Enum.GetValues<Place>())
                Assert.True(f.IsPriceAllowed(f.Suggest(a, b)));
    }

    [Fact]
    public void Only_campus_places_are_flagged_as_campuses()
    {
        Assert.True(Place.BAC.IsCampus());
        Assert.False(Place.Gabane.IsCampus());
    }
}
