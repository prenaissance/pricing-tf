namespace PricingTf.Processing.MapReduce

module Etl =
    open PricingTf.Common.Models
    open PricingTf.Processing.Events
    open System
    open System.Text.RegularExpressions

    let private spelledRegex =
        Regex(
            @"spell|pumpkin|exo|𝐄𝐗𝐎?|𝐏𝐔𝐌𝐏𝐊𝐈𝐍|𝐇𝐅|𝐄𝐱𝐨𝐫𝐜𝐢𝐬𝐦|𝐏𝐁|ꜱᴘᴇʟʟ|𝗦𝗣𝗘𝗟𝗟|𝐒𝐩𝐞𝐥𝐥|𝐒𝐏𝐄𝐋𝐋",
            RegexOptions.Compiled ||| RegexOptions.IgnoreCase
        )

    let filterSpelledEvents events =
        events
        |> Seq.filter (fun x -> not (spelledRegex.IsMatch(x.payload.details |> Option.defaultValue "")))

    let filterUnusualWeaponsEvents events =
        events
        |> Seq.filter (fun x -> (x.payload.item.defindex |> Option.defaultValue 0) <> 134)

    let splitByUpsertAndDelete events =
        let mapToPayload = fun event -> event.payload

        events
        |> List.partition (fun x -> x.event = ListingUpdate)
        |> fun (upserts, deletes) -> (upserts |> List.map mapToPayload, deletes |> List.map mapToPayload)

    let mapToListing exchangeRate payload =
        { itemName = payload.item.name
          marketName = payload.item.marketName
          quality = payload.item.quality.name
          price = Tf2Currency.from payload.currencies.metal payload.currencies.keys
          priceMetal =
            Tf2Currency.from payload.currencies.metal payload.currencies.keys
            |> Tf2Currency.toMetal exchangeRate
          priceKeys =
            Tf2Currency.from payload.currencies.metal payload.currencies.keys
            |> Tf2Currency.toKeys exchangeRate
          description = payload.details |> Option.defaultValue ""
          intent = payload.intent
          bumpedAt =
            payload.bumpedAt
            |> DateTimeOffset.FromUnixTimeSeconds
            |> fun x -> x.UtcDateTime
          isAutomatic = Option.isSome payload.userAgent
          tradeDetails =
            { listingId = payload.id
              tradeOfferUrl = payload.user.tradeOfferUrl
              description = payload.details |> Option.defaultValue ""
              item =
                { name = payload.item.name
                  imageUrl = payload.item.imageUrl
                  quality = payload.item.quality
                  particle = payload.item.particle }
              user =
                { name = payload.user.name
                  avatarThumbnailUrl = payload.user.avatar
                  online = payload.user.online
                  steamId = payload.steamid } } }

    let filterSpelled listings =
        listings |> List.filter (fun x -> not (spelledRegex.IsMatch x.description))
