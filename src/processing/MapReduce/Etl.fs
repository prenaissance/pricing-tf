namespace PricingTf.Processing.MapReduce

module Etl =
    open PricingTf.Processing.Events
    open PricingTf.Processing.Models
    open System

    let splitByUpsertAndDelete events =
        let mapToPayload = fun event -> event.payload

        events
        |> List.partition (fun x -> x.event = ListingUpdate)
        |> fun (upserts, deletes) -> (upserts |> List.map mapToPayload, deletes |> List.map mapToPayload)

    let mapToListing exchangeRate payload =
        { itemName = payload.item.name
          price = Tf2Currency.fromMixed payload.currencies.metal payload.currencies.keys exchangeRate
          description = payload.details |> Option.defaultValue ""
          intent = payload.intent
          bumpedAt =
            payload.bumpedAt
            |> DateTimeOffset.FromUnixTimeSeconds
            |> fun x -> x.UtcDateTime
          isAutomatic = Option.isSome payload.userAgent
          tradeDetails =
            { listingId = payload.id
              tradeOfferUrl = payload.user.tradeOfferUrl } }
