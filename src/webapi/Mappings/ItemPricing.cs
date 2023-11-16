using Google.Protobuf.WellKnownTypes;
using PricingTf.WebApi.Models.PricedItem;

namespace PricingTf.WebApi;

public sealed partial class ItemPricing
{
    public static ItemPricing FromPricedItem(PricedItem pricedItem) =>
        new()
        {
            Name = pricedItem.Name,
            Buy = pricedItem.Buy,
            Sell = pricedItem.Sell,
            UpdatedAt = Timestamp.FromDateTime(pricedItem.UpdatedAt)
        };
}