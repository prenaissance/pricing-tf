using Google.Protobuf.WellKnownTypes;
using PricingTf.WebApi.Models.PricedItem;

namespace PricingTf.WebApi.Protos;

public sealed partial class ItemPricing
{
    public static ItemPricing FromPricedItem(PricedItem pricedItem) =>
        new()
        {
            Name = pricedItem.Name,
            Buy = pricedItem.BuyListing is not null
                ? ListingDetails.FromListingDetails(pricedItem.BuyListing)
                : null,
            BuyListings = {
                pricedItem!.BuyListings
                    .Select(ListingDetails.FromListingDetails)
                    .ToArray()
            },
            Sell = pricedItem.SellListing is not null
                ? ListingDetails.FromListingDetails(pricedItem.SellListing)
                : null,
            SellListings = {
                pricedItem!.SellListings
                    .Select(ListingDetails.FromListingDetails)
                    .ToArray()
            },
            UpdatedAt = Timestamp.FromDateTime(pricedItem.UpdatedAt)
        };
}