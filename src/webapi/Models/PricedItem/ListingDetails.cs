using PricingTf.Common.Models;

namespace PricingTf.WebApi.Models.PricedItem;

public class ListingDetails
{
    public string ItemName { get; set; } = default!;
    public ListingIntent Intent { get; set; } = default!;
    public Tf2Currency Price { get; set; } = default!;
    public double PriceKeys { get; set; } = default!;
    public double PriceMetal { get; set; } = default!;
    public TradeDetails? TradeDetails { get; set; } = default!;
    public DateTime BumpedAt { get; set; } = DateTime.UtcNow;
}