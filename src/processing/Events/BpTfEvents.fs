namespace PricingTf.Processing.Events

open System.Text.Json.Serialization
open PricingTf.Processing.Models

type Tf2NaturalCurrency =
    { raw: float
      short: string
      long: string }

type SteamPrice =
    { currency: string
      short: string
      long: string
      raw: float
      value: float }

type CommunityPrice =
    { value: float
      valueHigh: float
      currency: string
      raw: float
      short: string
      long: string
      usd: float
      updatedAt: int
      difference: float }

type SuggestedPrice =
    { raw: float
      short: string
      long: string
      usd: float }

type Price =
    { steam: SteamPrice option
      community: CommunityPrice option
      suggested: SuggestedPrice option }

type Quality =
    { id: int; name: string; color: string }

type Particle =
    { id: int
      name: string
      shortName: string
      imageUrl: string
      [<JsonPropertyName "type">]
      _type: string }

type Origin = { id: int; name: string }

type Sheen = { id: int; name: string }

type Killstreaker = { id: int; name: string }

type UserAgent = { client: string; lastPulse: int }

type User =
    { id: string
      name: string
      avatar: string
      avatarFull: string
      premium: bool
      online: bool
      banned: bool
      customNameStyle: string
      acceptedSuggestions: int
      [<JsonPropertyName "class">]
      _class: string
      style: string
      tradeOfferUrl: string
      isMarketplaceSeller: bool
      flagImpersonated: bool
      bans: obj }

type ItemListing =
    { appid: int
      baseName: string
      defindex: int
      id: string
      imageUrl: string
      marketName: string
      name: string
      origin: Origin option
      originalId: string
      // causes some deserialization issue for some reason
      // price: Price
      quality: Quality
      summary: string
      level: int
      killstreakTier: int
      [<JsonPropertyName "class">]
      _class: string list
      slot: string
      tradable: bool
      craftable: bool
      sheen: Sheen option
      killstreaker: Killstreaker option }

type PricingEventPayload =
    { id: string
      steamid: string
      appid: int
      currencies: Tf2Currency
      value: Tf2NaturalCurrency
      tradeOffersPreferred: bool
      buyoutOnly: bool
      details: string option
      listedAt: int
      bumpedAt: int
      intent: ListingIntent
      count: int
      status: string
      source: string
      item: ItemListing
      userAgent: UserAgent option
      user: User }

type ListingType =
    | ListingUpdate
    | ListingDelete

    static member parse =
        function
        | "listing-update" -> ListingUpdate
        | "listing-delete" -> ListingDelete
        | _ -> failwith "Invalid listing type"

type PricingEvent =
    { id: byte[]
      event: ListingType
      payload: PricingEventPayload }
