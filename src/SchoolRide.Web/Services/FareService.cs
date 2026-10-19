using Microsoft.Extensions.Options;
using SchoolRide.Web.Models;

namespace SchoolRide.Web.Services;

public class FareOptions
{
    /// <summary>Share of each fare kept by the platform (the commission in the business model).</summary>
    public decimal PlatformFeeRate { get; set; } = 0.10m;
    public decimal MinPricePerSeat { get; set; } = 5m;
    /// <summary>Hard ceiling per seat. Survey means were P19.30 (combi) and P44.30 (taxi), so the cap keeps rides well under a taxi.</summary>
    public decimal MaxPricePerSeat { get; set; } = 25m;
    public decimal BasePrice { get; set; } = 8m;
    public decimal PricePerZone { get; set; } = 4m;
    public int BookingTimeoutMinutes { get; set; } = 30;
}

public record FareQuote(decimal Total, decimal PlatformFee, decimal DriverEarnings);

public class FareService(IOptions<FareOptions> options)
{
    private readonly FareOptions _o = options.Value;

    public decimal MinPrice => _o.MinPricePerSeat;
    public decimal MaxPrice => _o.MaxPricePerSeat;
    public int BookingTimeoutMinutes => _o.BookingTimeoutMinutes;

    public bool IsPriceAllowed(decimal pricePerSeat) => pricePerSeat >= _o.MinPricePerSeat && pricePerSeat <= _o.MaxPricePerSeat;

    public FareQuote Quote(decimal pricePerSeat, int seats)
    {
        var total = Math.Round(pricePerSeat * seats, 2);
        var fee = Math.Round(total * _o.PlatformFeeRate, 2);
        return new FareQuote(total, fee, total - fee);
    }

    /// <summary>
    /// Starting suggestion for a driver, based on the farther of the two places from central Gaborone.
    /// This is a placeholder rule to be calibrated with pilot data (open question: what fare will drivers accept?).
    /// </summary>
    public decimal Suggest(Place origin, Place destination)
    {
        var zone = Math.Max(origin.Zone(), destination.Zone());
        var price = _o.BasePrice + _o.PricePerZone * zone;
        return Math.Clamp(price, _o.MinPricePerSeat, _o.MaxPricePerSeat);
    }
}
