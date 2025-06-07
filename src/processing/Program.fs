open System
open System.IO
open System.Threading
open System.Text.Json

open Websocket.Client
open Microsoft.Extensions.Configuration

open PricingTf.Processing.Actors
open PricingTf.Processing.Events
open PricingTf.Processing.Services
open PricingTf.Processing.MapReduce
open PricingTf.Processing.Utils
open PricingTf.Processing.Workers
open MongoDB.Driver
open PricingTf.Common.Models

[<CLIMutable>]
type Config =
    { MongoDbUrl: string
      MongoDbName: string
      BackpackTfCookie: string
      ListingsTtlHours: int }

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
        MongoDbUrl =
            config.MongoDbUrl
            |> StringUtils.defaultIfEmpty "mongodb://localhost:27117?replicaSet=rs0&connectTimeoutMS=5000"
        MongoDbName = config.MongoDbName |> StringUtils.defaultIfEmpty "backpack-tf-replica"
        ListingsTtlHours =
            if config.ListingsTtlHours > 0 then
                config.ListingsTtlHours
            else
                6 }

[<Literal>]
let wsUrl = "wss://ws.backpack.tf/events"

let getExchangeRate () =
    let rate = BackpackTfApi.GetKeyExchangeRate configuration.BackpackTfCookie
    printfn "Current exchange rate: %f" rate
    rate

let mutable exchangeRate = getExchangeRate ()

let db = Db.connectToMongoDb configuration.MongoDbUrl configuration.MongoDbName

let blockedUsersCollection =
    db |> Db.BlockedUsers.getCollection |> Async.RunSynchronously

let blockedUsersAgent =
    BlockedUsersAgent(
        blockedUsersCollection
        |> Db.BlockedUsers.getBlockedUsers
        |> Async.RunSynchronously
        |> Seq.map _.steamId
    )

let blockedUsersCursor =
    blockedUsersCollection.Watch(EmptyPipelineDefinition<ChangeStreamDocument<BlockedUser>>())

blockedUsersCursor.ForEachAsync(fun change ->
    async {
        match change.OperationType with
        | ChangeStreamOperationType.Insert ->
            let user = change.FullDocument
            blockedUsersAgent.Add user
            printfn "Added blocked user: %s" user.steamId
        | ChangeStreamOperationType.Delete ->
            let user = change.FullDocument
            blockedUsersAgent.Remove user
            printfn "Removed blocked user: %s" user.steamId
        | _ -> ()
    }
    |> Async.Start)
|> ignore

let tradeListingsCollection =
    db
    |> Db.TradeListings.getCollection configuration.ListingsTtlHours
    |> Async.RunSynchronously

let getWsEventStream (url: string) =
    let client = new WebsocketClient(Uri url)
    client.ReconnectTimeout <- TimeSpan.FromSeconds(30.0)

    client.ReconnectionHappened
    |> Observable.subscribe (fun x -> printfn "WS Reconnection happened: %A" x)
    |> ignore

    client.Start() |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    client.MessageReceived
    |> Observable.map _.Text
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
    |> Observable.map (fun events ->
        events
        |> List.filter (fun event ->
            not (
                blockedUsersAgent.GetAll()
                |> Async.RunSynchronously
                |> Set.contains event.payload.steamid
            )))

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

let materializeViewsCallback () =
    [ TfPrices.refreshPricesView tradeListingsCollection
      TfPrices.refreshBotsPricesView tradeListingsCollection ]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.Start


let timer = new Timers.Timer(TimeSpan.FromMinutes 1)
timer.AutoReset <- true

timer.Elapsed.Add(fun _ ->
    try
        materializeViewsCallback ()
        printfn "Materialized views refreshed at %s" (DateTimeOffset.UtcNow.ToString "o")
    with e ->
        printfn "Failed to refresh materialized views: %A" e)

timer.Start()

let exchangeRateTimer = new Timers.Timer(TimeSpan.FromMinutes 15.)
exchangeRateTimer.AutoReset <- true

exchangeRateTimer.Elapsed.Add(fun _ -> exchangeRate <- getExchangeRate ())
exchangeRateTimer.Start()

let exitEvent = new ManualResetEvent false
exitEvent.WaitOne() |> ignore
