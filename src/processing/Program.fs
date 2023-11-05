open System
open Websocket.Client
open System.Threading
open System.Text.Json.Nodes
open System.Text.Json
open PricingTf.Processing.Events

let getExamplePricingEvent () =
    { id = Convert.FromHexString "654780b6b179b639d30bf17a"
      event = ListingUpdate
      payload =
        { id = "440_76561199115825381_5f2206613542100070a8b2421512c640"
          steamid = "76561199115825381"
          appid = 440
          currencies = { metal = 51.22; keys = 27 }
          value =
            { raw = 1518.13
              short = "27.94 keys"
              long = "27 keys, 51.22 ref" }
          tradeOffersPreferred = true
          buyoutOnly = true
          details = "I am buying your Perennial Petals Reel Fly Hat for 27 keys, 51.22 ref, I have 0 / 1. Peace Out"
          listedAt = 1699184822
          bumpedAt = 1699184822
          intent = Buy
          count = 1
          status = "active"
          source = "userAgent"
          item =
            { appid = 440
              baseName = "Reel Fly Hat"
              defindex = 61
              id = "5f7b1b4c4dd7f6c3a8a7b2a0"
              imageUrl = "https://files.backpack.tf/images/440/61.png"
              marketName = "The Ambassador"
              name = "The Ambassador"
              origin = None
              originalId = "5f7b1b4c4dd7f6c3a8a7b2a0"
              price =
                { steam =
                    { currency = "metal"
                      short = "0.00"
                      long = "0.00 ref"
                      raw = 0.0
                      value = 0.0 }
                  community =
                    { value = 0.0
                      valueHigh = 0.0
                      currency = "metal"
                      raw = 0.0
                      short = "0.00"
                      long = "0.00 ref"
                      usd = 0.0
                      updatedAt = 0
                      difference = 0.0 }
                  suggested =
                    { raw = 0.0
                      short = "0.00"
                      long = "0.00 ref"
                      usd = 0.0 } }
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

let wsUrl = "wss://ws.backpack.tf/events"

type ParsedPair = PricingEvent * JsonNode

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
            printfn "Event: %A" (JsonValue.Parse x)
            None)
    |> Observable.filter (fun x -> x.IsSome)
    |> Observable.map (fun x -> x.Value)

let wsEventStream = getWsEventStream wsUrl

let countSubscription declaration count =
    printfn "%s events: %d" declaration count

wsEventStream
|> Observable.scan (fun acc x -> acc + (x |> List.length)) 0
|> Observable.subscribe (countSubscription "Total")
|> ignore


wsEventStream
|> Observable.scan
    (fun (accUpdate, accDelete) x ->
        let updateCount, deleteCount =
            x
            |> List.partition (fun x -> x.event = ListingUpdate)
            |> (fun (x, y) -> (x |> List.length, y |> List.length))

        (accUpdate + updateCount, accDelete + deleteCount))
    (0, 0)
|> Observable.subscribe (fun (updateCount, deleteCount) ->
    printfn "Update events: %d" updateCount
    printfn "Delete events: %d" deleteCount)
|> ignore


let exitEvent = new ManualResetEvent(false)
exitEvent.WaitOne() |> ignore
