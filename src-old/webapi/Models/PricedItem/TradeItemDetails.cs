using PricingTf.Common.Models;

namespace PricingTf.WebApi.Models.PricedItem;

public class TradeItemDetails
{
    public string Name { get; set; } = default!;
    public string ImageUrl { get; set; } = default!;
    public ItemQuality Quality { get; set; } = default!;
    public ItemParticle? Particle { get; set; } = default!;
}