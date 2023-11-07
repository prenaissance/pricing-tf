namespace PricingTf.Processing.Services

open System

module Db =
    open MongoDB.Driver

    let connectToMongoDb (connectionString: string) =
        let client = new MongoClient(connectionString)
        let database = client.GetDatabase("backpack-tf-replica")
        database

    module Listings =
        open MongoDB.Driver
        open PricingTf.Processing.Models

        let private bumpedAtIndexModel =
            let index = Builders<Listing>.IndexKeys.Ascending("bumpedAt")
            let options = CreateIndexOptions()
            options.ExpireAfter <- TimeSpan.FromHours(24.0)
            CreateIndexModel<Listing>(index, options)

        let getCollection (database: IMongoDatabase) =
            async {
                let collection = database.GetCollection<Listing>("listings")

                let indices = [ bumpedAtIndexModel ]
                do! collection.Indexes.CreateManyAsync(indices) |> Async.AwaitTask |> Async.Ignore

                return collection
            }
