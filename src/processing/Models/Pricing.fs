namespace PricingTf.Processing.Models

open MongoDB.Bson

[<CLIMutable>]
type Listing = { id: ObjectId; listingId: int }
