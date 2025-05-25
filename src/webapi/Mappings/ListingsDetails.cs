using Google.Protobuf.WellKnownTypes;

namespace PricingTf.WebApi.Protos;

public sealed partial class ListingDetails
{
    public static ListingDetails FromListingDetails(Models.PricedItem.ListingDetails listingDetails)
    {
        return new ListingDetails
        {
            Price = new Tf2Currency
            {
                Keys = listingDetails.Price.keys,
                Metal = listingDetails.Price.metal,
            },
            PriceKeys = listingDetails.PriceKeys,
            PriceMetal = listingDetails.PriceMetal,
            TradeDetails = listingDetails.TradeDetails is not null
                ? new TradeDetails
                {
                    ListingId = listingDetails.TradeDetails.listingId,
                    TradeOfferUrl = listingDetails.TradeDetails.tradeOfferUrl,
                }
                : null,
            BumpedAt = Timestamp.FromDateTime(listingDetails.BumpedAt)
        };
    }
}