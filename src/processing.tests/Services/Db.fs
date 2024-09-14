namespace PricingTf.Processing.Tests.Services

open Microsoft.VisualStudio.TestTools.UnitTesting
open PricingTf.Common.Models
open PricingTf.Processing.Tests.TestUtils
open PricingTf.Common.Serialization
open MongoDB.Driver
open MongoDB.Bson.Serialization
open System
open NSubstitute
open PricingTf.Processing.Services.Db
open PricingTf.Common.Configuration

[<TestClass>]
type DbTests() =
    [<Literal>]
    let listingTtlHours = 24

    let mutable mockCollection: IMongoCollection<TradeListing> = null
    let mutable mockDb: IMongoDatabase = null

    [<TestInitialize>]
    member _.Setup() =
        BsonSerializer.RegisterSerializer(typeof<ListingIntent>, ListingIntentSerializer())
        mockCollection <- Substitute.For<IMongoCollection<TradeListing>>()
        mockDb <- Substitute.For<IMongoDatabase>()

        mockDb
            .GetCollection<TradeListing>(PricingCollection.TradeListings)
            .Returns(mockCollection)
        |> ignore

    [<TestMethod>]
    member _.``TradeListings.getCollection should return the correct collection``() =
        let collection = TradeListings.getCollection listingTtlHours mockDb

        Assert.IsInstanceOfType<IMongoCollection<TradeListing>>(collection |> Async.RunSynchronously)

        mockDb.Received().GetCollection<TradeListing>(PricingCollection.TradeListings)
        |> ignore

        mockCollection.Received().Indexes.List().ToListAsync() |> ignore

    member _.``TradeListings.upsertListings should insert or update listings``() = ignore

    member _.``TradeListings.deleteListingsByIds should delete listings by ids``() =
        let collection =
            TradeListings.getCollection listingTtlHours mockDb |> Async.RunSynchronously

        collection.Received().DeleteManyAsync(Arg.Any<FilterDefinition<TradeListing>>())
        |> ignore
