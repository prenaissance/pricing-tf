namespace PricingTf.Common.Models

open MongoDB.Bson
open System

type BlockedUser =
    { [<DefaultValue>]
      id: ObjectId
      steamId: string
      blockedAt: DateTime }
