using Google.Protobuf.WellKnownTypes;
using PricingTf.WebApi.Models.PricedItem;

namespace PricingTf.WebApi;

public sealed partial class ItemPricing
{
    public static ItemPricing FromPricedItem(PricedItem pricedItem) =>
        new()
        {
            Name = pricedItem.Name,
            Buy = pricedItem.BuyListing,
            BuyListings = { pricedItem.BuyListings },
            Sell = pricedItem.SellListing,
            SellListings = { pricedItem.SellListings },
            UpdatedAt = Timestamp.FromDateTime(pricedItem.UpdatedAt)
        };
}