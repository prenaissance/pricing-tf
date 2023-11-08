namespace PricingTf.Processing.Models

open System
open MongoDB.Bson

type TradeDetails =
    { listingId: string
      tradeOfferUrl: string }

[<CLIMutable>]
type Listing =
    { price: Tf2Currency
      bumpedAt: DateTime
      isAutomatic: bool
      tradeDetails: TradeDetails }

[<CLIMutable>]
type TradeItem =
    {
        id: ObjectId
        name: string
        /// buy - actor making the listing is buying
        buyListings: Listing list
        /// sell - actor making the listing is selling
        sellListings: Listing list
    }
