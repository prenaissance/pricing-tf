namespace PricingTf.Processing.Services

open System

module Db =
    open MongoDB.Driver

    let connectToMongoDb (connectionString: string) =
        let client = new MongoClient(connectionString)
        let database = client.GetDatabase("backpack-tf-replica")
        database

    module TradeItems =
        open MongoDB.Driver
        open PricingTf.Processing.Models

        let private nameIndexKey = IndexKeysDefinitionBuilder<TradeItem>().Ascending("name")

        let private buyBumpedAtIndexKey =
            IndexKeysDefinitionBuilder<TradeItem>().Descending("buyListings.bumpedAt")

        let private sellBumpedAtIndexKey =
            IndexKeysDefinitionBuilder<TradeItem>().Descending("sellListings.bumpedAt")

        let private sellPriceIndexKey =
            IndexKeysDefinitionBuilder<TradeItem>().Ascending("sellListings.price.keys")

        let private buyPriceIndexKey =
            IndexKeysDefinitionBuilder<TradeItem>().Ascending("buyListings.price.keys")

        let private buyListingIdKey =
            IndexKeysDefinitionBuilder<TradeItem>()
                .Hashed("buyListings.tradeDetails.listingId")

        let private sellListingIdKey =
            IndexKeysDefinitionBuilder<TradeItem>()
                .Hashed("sellListings.tradeDetails.listingId")

        let private indices =
            [ nameIndexKey
              buyBumpedAtIndexKey
              sellBumpedAtIndexKey
              sellPriceIndexKey
              buyPriceIndexKey ]
            |> List.map CreateIndexModel

        let getCollection (database: IMongoDatabase) =
            async {
                let collection = database.GetCollection<TradeItem>("trade-items")

                do! collection.Indexes.CreateManyAsync(indices) |> Async.AwaitTask |> Async.Ignore

                return collection
            }
