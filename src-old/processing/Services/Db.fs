namespace PricingTf.Processing.Services

open MongoDB.Bson
open MongoDB.Driver
open MongoDB.Bson.Serialization
open PricingTf.Common.Configuration
open PricingTf.Common.Models
open PricingTf.Common.Serialization

module Db =
    let connectToMongoDb (connectionString: string) dbName =
        BsonSerializer.RegisterSerializer(typeof<ListingIntent>, ListingIntentSerializer())

        BsonSerializer.RegisterGenericSerializerDefinition(typedefof<option<_>>, typedefof<OptionSerializer<_>>)

        let client = new MongoClient(connectionString)
        let database = client.GetDatabase dbName
        database

    module TradeListings =
        open System

        let private automaticIndexModel =
            let indexKey = IndexKeysDefinitionBuilder<TradeListing>().Ascending("isAutomatic")
            CreateIndexModel indexKey

        let private nameIndexModel =
            let indexKey = IndexKeysDefinitionBuilder<TradeListing>().Ascending("itemName")
            CreateIndexModel indexKey

        let private nameAndIntentIndexModel =
            let indexKey =
                IndexKeysDefinitionBuilder<TradeListing>()
                    .Ascending("itemName")
                    .Ascending("intent")

            CreateIndexModel indexKey

        let private listingIdIndexModel =
            let indexKey =
                IndexKeysDefinitionBuilder<TradeListing>().Ascending("tradeDetails.listingId")

            let options = CreateIndexOptions(Unique = true)

            CreateIndexModel(indexKey, options)

        let private getBumpedAtIndex listingsTtlHours =
            // ttl index
            let indexKey = IndexKeysDefinitionBuilder<TradeListing>().Ascending("bumpedAt")

            let options =
                new CreateIndexOptions(ExpireAfter = TimeSpan.FromHours(int listingsTtlHours))

            CreateIndexModel(indexKey, options)

        let getCollection listingsTtlHours (database: IMongoDatabase) =
            async {
                let collection =
                    database.GetCollection<TradeListing> PricingCollection.TradeListings

                // drop existing bumpedAt index if the ttl is different
                let! existingIndexes = collection.Indexes.List().ToListAsync() |> Async.AwaitTask

                let bumpedAtIndex =
                    existingIndexes
                    |> Seq.tryFind (fun x -> x.GetElement("name").Value.AsString = "bumpedAt_1")

                match bumpedAtIndex with
                | Some index ->
                    let ttlHours = index.GetElement("expireAfterSeconds").Value.AsInt32 / 3600

                    if ttlHours <> listingsTtlHours then
                        do! collection.Indexes.DropOneAsync "bumpedAt_1" |> Async.AwaitTask |> Async.Ignore
                | None -> ()

                let indices =
                    [ automaticIndexModel
                      nameIndexModel
                      nameAndIntentIndexModel
                      listingIdIndexModel
                      getBumpedAtIndex listingsTtlHours ]

                do! collection.Indexes.CreateManyAsync indices |> Async.AwaitTask |> Async.Ignore

                return collection
            }

        let upsertListings listings (collection: IMongoCollection<TradeListing>) =
            // update if listingId already exists
            let bulkOperations =
                listings
                |> List.map (fun x ->
                    let filter =
                        Builders<TradeListing>.Filter
                            .Eq(_.tradeDetails.listingId, x.tradeDetails.listingId)

                    let update =
                        Builders<TradeListing>.Update
                            .SetOnInsert(_.id, ObjectId.GenerateNewId())
                            .Set(_.bumpedAt, x.bumpedAt)
                            .Set(_.price, x.price)
                            .Set(_.priceMetal, x.priceMetal)
                            .Set(_.priceKeys, x.priceKeys)
                            .Set(_.description, x.description)
                            .SetOnInsert(_.itemName, x.itemName)
                            .SetOnInsert(_.marketName, x.marketName)
                            .SetOnInsert(_.quality, x.quality)
                            .SetOnInsert(_.intent, x.intent)
                            .SetOnInsert(_.isAutomatic, x.isAutomatic)
                            .SetOnInsert(_.tradeDetails, x.tradeDetails)

                    UpdateOneModel<TradeListing>(filter, update, IsUpsert = true) :> WriteModel<TradeListing>)

            let bulkWriteOptions = new BulkWriteOptions(IsOrdered = false)

            collection.BulkWriteAsync(bulkOperations, bulkWriteOptions) |> Async.AwaitTask

        let deleteListingsByIds listingIds (collection: IMongoCollection<TradeListing>) =
            let filter = Builders<TradeListing>.Filter.In(_.tradeDetails.listingId, listingIds)

            collection.DeleteManyAsync filter |> Async.AwaitTask

    module BlockedUsers =
        open System

        let getCollection (database: IMongoDatabase) =
            async {
                let hasCollection =
                    database.ListCollectionNamesAsync()
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    |> _.ToEnumerable()
                    |> Seq.exists ((=) PricingCollection.BlockedUsers)

                let collection = database.GetCollection<BlockedUser> PricingCollection.BlockedUsers

                if not hasCollection then
                    let options = CreateCollectionOptions()
                    options.ChangeStreamPreAndPostImagesOptions <- ChangeStreamPreAndPostImagesOptions(Enabled = true)

                    do!
                        database.CreateCollectionAsync(PricingCollection.BlockedUsers, options)
                        |> Async.AwaitTask


                    // create index on steamId
                    let indexKey = IndexKeysDefinitionBuilder<BlockedUser>().Ascending "steamId"
                    let indexOptions = new CreateIndexOptions(Unique = true)
                    let indexModel = CreateIndexModel(indexKey, indexOptions)

                    do! collection.Indexes.CreateOneAsync indexModel |> Async.AwaitTask |> Async.Ignore

                return collection
            }

        let upsertBlockedUser steamId (collection: IMongoCollection<BlockedUser>) =
            collection.UpdateOneAsync(
                Builders<BlockedUser>.Filter.Eq((fun x -> x.steamId), steamId),
                Builders<BlockedUser>.Update
                    .SetOnInsert(_.id, ObjectId.GenerateNewId())
                    .Set(_.blockedAt, DateTime.UtcNow),
                new UpdateOptions(IsUpsert = true)
            )
            |> Async.AwaitTask

        let unblockUser steamId (collection: IMongoCollection<BlockedUser>) =
            collection.DeleteOneAsync(Builders<BlockedUser>.Filter.Eq((fun x -> x.steamId), steamId))
            |> Async.AwaitTask

        let getBlockedUsers (collection: IMongoCollection<BlockedUser>) =
            collection.Find(FilterDefinition<BlockedUser>.Empty).ToListAsync()
            |> Async.AwaitTask
