namespace PricingTf.Processing.Models

open System
open MongoDB.Bson

type TradeDetails =
    { listingId: string
      tradeOfferUrl: string }

type ListingIntent =
    | Buy
    | Sell

[<CLIMutable>]
type TradeListing =
    { [<DefaultValue>]
      id: ObjectId
      itemName: string
      intent: ListingIntent
      price: Tf2Currency
      bumpedAt: DateTime
      isAutomatic: bool
      tradeDetails: TradeDetails }

[<CLIMutable>]
type TradeItem =
    {
        id: ObjectId
        name: string
        /// buy - actor making the listing is buying
        buyListings: TradeListing list
        /// sell - actor making the listing is selling
        sellListings: TradeListing list
    }
