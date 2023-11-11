namespace PricingTf.Processing.Services

open System.Net.Http
open System.Net.Http.Json

open PricingTf.Processing.Models

module BackpackTfApi =
    open System.Text.Json
    open PricingTf.Processing.Events

    [<Literal>]
    let MANNCO_SUPPLY_CRATE_KEY = "Mann Co. Supply Crate Key"

    type SnapshotListing = { price: Metal; userAgent: obj option }

    type SnapshotResponse = { listings: SnapshotListing list }

    let getKeyExchangeRate (cookie: string) =
        async {
            let queryParams = [ "sku", MANNCO_SUPPLY_CRATE_KEY; "appid", 440 |> string ]

            let query =
                queryParams
                |> List.fold (fun acc (key, value) -> sprintf "%s&%s=%s" acc key value) ""

            let url = sprintf "https://backpack.tf/api/classifieds/listings/snapshot?%s" query
            use httpClient = new HttpClient()
            httpClient.DefaultRequestHeaders.Add("Cookie", cookie)
            let! response = httpClient.GetStringAsync(url) |> Async.AwaitTask

            let json =
                JsonSerializer.Deserialize<SnapshotResponse>(response, BpTfEventsConverters.jsonOptions)

            let cheapestBuyListing =
                json.listings
                |> List.filter (fun x -> Option.isSome (x.userAgent))
                |> List.minBy (fun x -> x.price)

            return cheapestBuyListing.price / 1.0<keys>
        }
