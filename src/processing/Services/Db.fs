namespace PricingTf.Processing.Services

module Db =
    open MongoDB.Driver
    open MongoDB.Bson.Serialization
    open PricingTf.Common.Models
    open PricingTf.Common.Serialization

    let connectToMongoDb (connectionString: string) dbName =
        BsonSerializer.RegisterSerializer(typeof<ListingIntent>, ListingIntentSerializer())

        let client = new MongoClient(connectionString)
        let database = client.GetDatabase dbName
        database

    module TradeListings =
        open PricingTf.Common.Models
        open MongoDB.Bson
        open System
        open PricingTf.Common.Configuration

        let private nameIndexModel =
            let indexKey = IndexKeysDefinitionBuilder<TradeListing>().Hashed("listingName")
            CreateIndexModel(indexKey)

        let private nameAndIntentIndexModel =
            let indexKey =
                IndexKeysDefinitionBuilder<TradeListing>()
                    .Ascending("listingName")
                    .Hashed("intent")

            CreateIndexModel(indexKey)

        let private listingIdIndexModel =
            let indexKey =
                IndexKeysDefinitionBuilder<TradeListing>().Ascending("tradeDetails.listingId")

            let options = new CreateIndexOptions()
            options.Unique <- true

            CreateIndexModel(indexKey, options)

        let private bumpedAtIndexModel =
            // ttl index
            let indexKey = IndexKeysDefinitionBuilder<TradeListing>().Ascending("bumpedAt")

            let options = new CreateIndexOptions()
            options.ExpireAfter <- TimeSpan.FromDays(1.)

            CreateIndexModel(indexKey, options)

        let private indices =
            [ nameIndexModel
              nameAndIntentIndexModel
              listingIdIndexModel
              bumpedAtIndexModel ]

        let getCollection (database: IMongoDatabase) =
            async {
                let collection =
                    database.GetCollection<TradeListing>(PricingCollection.TradeListings)

                do! collection.Indexes.CreateManyAsync(indices) |> Async.AwaitTask |> Async.Ignore

                return collection
            }

        let upsertListings listings (collection: IMongoCollection<TradeListing>) =
            // update if listingId already exists
            let bulkOperations =
                listings
                |> List.map (fun x ->
                    let filter =
                        Builders<TradeListing>.Filter
                            .Eq((fun x -> x.tradeDetails.listingId), x.tradeDetails.listingId)

                    let update =
                        Builders<TradeListing>.Update
                            .SetOnInsert((fun x -> x.id), ObjectId.GenerateNewId())
                            .Set((fun x -> x.bumpedAt), x.bumpedAt)
                            .Set((fun x -> x.price), x.price)
                            .Set((fun x -> x.description), x.description)
                            .SetOnInsert((fun x -> x.itemName), x.itemName)
                            .SetOnInsert((fun x -> x.intent), x.intent)
                            .SetOnInsert((fun x -> x.isAutomatic), x.isAutomatic)
                            .SetOnInsert((fun x -> x.tradeDetails), x.tradeDetails)

                    let upsertOneModel = UpdateOneModel<TradeListing>(filter, update)
                    upsertOneModel.IsUpsert <- true
                    upsertOneModel :> WriteModel<TradeListing>)

            let bulkWriteOptions = new BulkWriteOptions()
            bulkWriteOptions.IsOrdered <- false

            collection.BulkWriteAsync(bulkOperations, bulkWriteOptions) |> Async.AwaitTask

        let deleteListingsByIds listingIds (collection: IMongoCollection<TradeListing>) =
            let filter =
                Builders<TradeListing>.Filter
                    .In((fun x -> x.tradeDetails.listingId), listingIds)

            collection.DeleteManyAsync(filter) |> Async.AwaitTask
