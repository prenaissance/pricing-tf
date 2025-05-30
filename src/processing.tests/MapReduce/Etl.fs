namespace PricingTf.Processing.Tests.Etl

open Microsoft.VisualStudio.TestTools.UnitTesting
open PricingTf.Common.Models
open PricingTf.Processing.Events
open PricingTf.Processing.MapReduce
open PricingTf.Processing.Tests.TestUtils
open System

[<TestClass>]
type EtlTests() =
    [<TestMethod>]
    member _.``Etl.filterUnusualWeaponsEvents should filter out unusual weapons events``() =
        let item = TestData.getExamplePricingEvent ()

        let unusualItem =
            { item with
                payload =
                    { item.payload with
                        item =
                            { item.payload.item with
                                defindex = Some 134 } } }

        let result = Etl.filterUnusualWeaponsEvents [ item; unusualItem ]
        Assert.AreEqual([ item ], result |> Seq.toList)

    [<TestMethod>]
    member _.``Etl.splitByUpsertAndDelete should split events by upsert and delete``() =
        let upsertEvent = TestData.getExamplePricingEvent ()
        let deleteEvent = TestData.getExamplePricingEvent ()
        let deleteEvent2 = TestData.getExamplePricingEvent ()

        let deleteEvent =
            { deleteEvent with
                event = ListingDelete }

        let deleteEvent2 =
            { deleteEvent2 with
                event = ListingDelete }

        let upserts, deletes =
            Etl.splitByUpsertAndDelete [ upsertEvent; deleteEvent; deleteEvent2 ]

        Assert.AreEqual([ upsertEvent.payload ], upserts)
        Assert.AreEqual([ deleteEvent.payload; deleteEvent2.payload ], deletes)

    [<TestMethod>]
    member _.``Etl.mapToListing should extract the important pricing information from an event``() =
        let payload = TestData.getExamplePricingEvent () |> fun x -> x.payload
        let exchangeRate = 50.0<metal / keys>
        let result = Etl.mapToListing exchangeRate payload

        Assert.AreEqual(
            result,
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
        )

    [<TestMethod>]
    member _.``Etl.filterSpelled should filter out spelled events``() =
        let spelledEvent = TestData.getExamplePricingEvent ()
        let unspelledEvent = TestData.getExamplePricingEvent ()

        let spelledEvent =
            { spelledEvent with
                payload =
                    { spelledEvent.payload with
                        details =
                            Some
                                "I am buying your Exorcism Perennial Petals Reel Fly Hat for 27 keys, 51.22 ref, I have 0 / 1. Peace Out" } }

        let result = Etl.filterSpelledEvents [ spelledEvent; unspelledEvent ]
        Assert.AreEqual([ unspelledEvent ], result |> Seq.toList)
