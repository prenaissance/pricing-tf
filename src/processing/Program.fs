open System
open Websocket.Client
open System.Threading
open System.Text.Json.Nodes

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

type PricingEvent =
    { id: int
      steamid: int
      appid: int
      currencies: Tf2Currency
      value: Tf2NaturalCurrency
      tradeOffersPreferred: bool
      details: string
      listedAt: int
      bumpedAt: int
      intent: string // TODO: ListingIntent
      count: int
      status: string
      source: string

    }

let wsUrl = "wss://ws.backpack.tf/events"

let getWsEventStream (url: string) =
    let client = new WebsocketClient(Uri(url))
    client.ReconnectTimeout <- TimeSpan.FromSeconds(30.0)

    client.ReconnectionHappened
    |> Observable.subscribe (fun x -> printfn "Reconnection happened: %A" x)
    |> ignore

    client.Start() |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    client.MessageReceived
    |> Observable.map (fun x -> x.Text)
    |> Observable.map JsonValue.Parse

let wsEventStream = getWsEventStream wsUrl

// wsEventStream
// |> Observable.scan (fun acc _ -> acc + 1) 0
// |> Observable.subscribe (fun x -> printfn "%d events" x)
// |> ignore
wsEventStream
|> Observable.map (fun x -> x.AsArray()[0])
|> Observable.subscribe (fun x -> printfn "%A" x)
|> ignore

let exitEvent = new ManualResetEvent(false)
exitEvent.WaitOne() |> ignore
