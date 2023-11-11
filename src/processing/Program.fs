open System
open System.IO
open System.Threading
open System.Text.Json

open Websocket.Client
open Microsoft.Extensions.Configuration

open PricingTf.Processing.Models
open PricingTf.Processing.Events
open PricingTf.Processing.Services
open PricingTf.Processing.MapReduce
open PricingTf.Processing.Utils

let getExamplePricingEvent () =
    { id = Convert.FromHexString "654780b6b179b639d30bf17a"
      event = ListingUpdate
      payload =
        { id = "440_76561199115825381_5f2206613542100070a8b2421512c640"
          steamid = "76561199115825381"
          appid = 440
          currencies =
            { metal = 51.22<metal>
              keys = 27.0<keys> }
          value =
            { raw = 1518.13
              short = "27.94 keys"
              long = "27 keys, 51.22 ref" }
          tradeOffersPreferred = true
          buyoutOnly = true
          details =
            Some "I am buying your Perennial Petals Reel Fly Hat for 27 keys, 51.22 ref, I have 0 / 1. Peace Out"
          listedAt = 1699184822
          bumpedAt = 1699184822
          intent = Buy
          count = 1
          status = "active"
          source = "userAgent"
          item =
            { appid = 440
              baseName = "Reel Fly Hat"
              defindex = Some 61
              id = "5f7b1b4c4dd7f6c3a8a7b2a0"
              imageUrl = "https://files.backpack.tf/images/440/61.png"
              marketName = "The Ambassador"
              name = "The Ambassador"
              origin = None
              originalId = "5f7b1b4c4dd7f6c3a8a7b2a0"
              quality =
                { id = 6
                  name = "Unique"
                  color = "FFD700" }
              summary = "Level 1 Pistol"
              level = 1
              killstreakTier = 0
              _class = [ "pistol" ]
              slot = "secondary"
              tradable = true
              craftable = true
              sheen = None
              killstreaker = None }
          userAgent =
            Some
                { client = "backpack.tf"
                  lastPulse = 1601902804 }
          user =
            { id = "asdasdasdas"
              name = "backpack.tf"
              avatar = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avat"
              avatarFull = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avat"
              premium = false
              online = false
              banned = false
              customNameStyle = ""
              acceptedSuggestions = 0
              _class = ""
              style = ""
              tradeOfferUrl = ""
              isMarketplaceSeller = false
              flagImpersonated = false
              bans = [] } } }

[<CLIMutable>]
type Config =
    { MongoDbUrl: string
      MongoDbName: string
      BackpackTfCookie: string }

let configuration =
    let builder = ConfigurationBuilder()

    builder
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
        .AddJsonFile("appsettings.Development.json", optional = true)
        .AddEnvironmentVariables()
    |> ignore

    let config = builder.Build().Get<Config>()

    { config with
        MongoDbUrl = config.MongoDbUrl |> StringUtils.defaultIfEmpty "mongodb://localhost:27017"
        MongoDbName = config.MongoDbName |> StringUtils.defaultIfEmpty "backpack-tf-replica" }

printfn "Cookies: %s" configuration.BackpackTfCookie

[<Literal>]
let wsUrl = "wss://ws.backpack.tf/events"

let exchangeRate =
    BackpackTfApi.getKeyExchangeRate configuration.BackpackTfCookie
    |> Async.RunSynchronously

let db = Db.connectToMongoDb configuration.MongoDbUrl configuration.MongoDbName


let tradeListingsCollection =
    db |> Db.TradeListings.getCollection |> Async.RunSynchronously

printfn "Exchange rate: %f" exchangeRate

let getWsEventStream (url: string) =
    let client = new WebsocketClient(Uri(url))
    client.ReconnectTimeout <- TimeSpan.FromSeconds(30.0)

    client.ReconnectionHappened
    |> Observable.subscribe (fun x -> printfn "Reconnection happened: %A" x)
    |> ignore

    client.Start() |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    client.MessageReceived
    |> Observable.map (fun x -> x.Text)
    |> Observable.map (fun x ->
        try
            let parsed =
                JsonSerializer.Deserialize<PricingEvent list>(x, BpTfEventsConverters.jsonOptions)

            Some parsed
        with e ->
            printfn "Failed to parse event: %A" e
            None)
    |> Observable.filter (fun x -> x.IsSome)
    |> Observable.map (fun x -> x.Value)

let wsEventStream = getWsEventStream wsUrl

// wsEventStream
// |> Observable.map (fun x -> x |> List.take 3)
// |> Observable.subscribe (printfn "%A")
// |> ignore

let countStream =
    wsEventStream |> Observable.scan (fun acc x -> acc + List.length x) 0

countStream
|> Observable.subscribe (fun x -> printfn "Total events: %d" x)
|> ignore

let mutable processedCount = 0

wsEventStream
|> Observable.subscribe (fun events ->
    try
        async {
            let filteredEvents =
                events
                |> Etl.filterSpelledEvents
                |> Etl.filterUnusualWeaponsEvents
                |> Seq.toList

            let upsert, delete = Etl.splitByUpsertAndDelete filteredEvents
            let upsertListings = upsert |> List.map (Etl.mapToListing exchangeRate)

            let upsertListingsTask =
                tradeListingsCollection |> Db.TradeListings.upsertListings upsertListings

            let deleteListingsTask =
                delete |> List.map (fun x -> x.id) |> Db.TradeListings.deleteListingsByIds
                <| tradeListingsCollection

            do!
                [ upsertListingsTask |> Async.Ignore; deleteListingsTask |> Async.Ignore ]
                |> Async.Parallel
                |> Async.Ignore

            processedCount <- processedCount + 1
            printfn "Processed batch %d" processedCount

        }
        |> Async.Start
    with e ->
        printfn "Failed to process event: %A" e)
|> ignore


let exitEvent = new ManualResetEvent(false)
exitEvent.WaitOne() |> ignore
