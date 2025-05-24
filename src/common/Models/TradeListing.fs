namespace PricingTf.Common.Models

open System
open MongoDB.Bson

type TradeDetails =
    { listingId: string
      tradeOfferUrl: string }

type ListingIntent =
    | Buy
    | Sell

    static member fromString value =
        match value with
        | "buy" -> Buy
        | "sell" -> Sell
        | _ -> raise <| ArgumentException("Invalid listing intent")

    override this.ToString() =
        match this with
        | Buy -> "buy"
        | Sell -> "sell"

    static member toString =
        function
        | Buy -> "buy"
        | Sell -> "sell"

[<CLIMutable>]
type TradeListing =
    { [<DefaultValue>]
      id: ObjectId
      quality: string
      itemName: string
      marketName: string
      description: string
      intent: ListingIntent
      price: Tf2Currency
      priceMetal: Metal
      priceKeys: Keys
      bumpedAt: DateTime
      isAutomatic: bool
      tradeDetails: TradeDetails }
