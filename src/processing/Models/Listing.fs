namespace PricingTf.Processing.Models

open System
open MongoDB.Bson

type ListingIntent =
    | Buy
    | Sell

type TradeDetails =
    { listingId: string
      tradeOfferUrl: string }

[<CLIMutable>]
type Listing =
    { id: ObjectId
      intent: ListingIntent
      price: Tf2Currency
      bumpedAt: DateTime
      isAutomatic: bool
      tradeDetails: TradeDetails }
