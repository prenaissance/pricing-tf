using MongoDB.Bson;

namespace PricingTf.WebApi.Models.PricedItem;

public class PricedItem
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public PricingDetails? Buy { get; set; } = default!;
    public PricingDetails? Sell { get; set; } = default!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}