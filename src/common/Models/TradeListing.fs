namespace PricingTf.Common.Models

open System
open System.Text.Json.Serialization
open MongoDB.Bson
open MongoDB.Bson.Serialization.Attributes

type ItemQuality =
    { [<BsonId>]
      id: int
      name: string
      color: string }

[<CLIMutable>]
type ItemParticle =

    { [<BsonId>]
      id: int
      name: string
      shortName: string
      imageUrl: string
      [<BsonElement "type">]
      [<JsonPropertyName "type">]
      _type: string }

[<CLIMutable>]
type TradeItemDetails =
    { name: string
      imageUrl: string
      quality: ItemQuality
      particle: ItemParticle }

type TradeUserDetails =
    { name: string
      avatarThumbnailUrl: string
      online: bool
      steamId: string }

type TradeDetails =
    { listingId: string
      tradeOfferUrl: string
      description: string
      item: TradeItemDetails
      user: TradeUserDetails }

type ListingIntent =
    | Buy
    | Sell

    static member fromString value =
        match value with
        | "buy" -> Buy
        | "sell" -> Sell
        | _ -> raise <| ArgumentException "Invalid listing intent"

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
