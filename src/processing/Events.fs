namespace PricingTf.Processing

open System.Text.Json.Serialization

type Tf2Currency = { metal: int; keys: int }

type Tf2NaturalCurrency =
    { raw: float
      short: string
      long: string }

type ListingIntent =
    | Buy
    | Sell

    static member parse(s: string) =
        match s with
        | "buy" -> Buy
        | "sell" -> Sell
        | _ -> failwith "Invalid listing intent"

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
    { steam: SteamPrice
      community: CommunityPrice
      suggested: SuggestedPrice }

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
    { id: int
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
      bans: int list }

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
      price: Price
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

type PricingEvent =
    { id: int
      steamid: int
      appid: int
      currencies: Tf2Currency
      value: Tf2NaturalCurrency
      tradeOffersPreferred: bool
      buyoutOnly: bool
      details: string
      listedAt: int
      bumpedAt: int
      intent: string // TODO: ListingIntent
      count: int
      status: string
      source: string
      item: ItemListing
      userAgent: UserAgent
      user: User }
