using PricingTf.Common.Models;

namespace PricingTf.WebApi.Models.PricedItem;

public class TradeDetails
{
    public string ListingId { get; set; } = default!;
    public string TradeOfferUrl { get; set; } = default!;
    public string Description { get; set; } = default!;
    public TradeItemDetails Item { get; set; } = default!;
    public TradeUserDetails User { get; set; } = default!;
}