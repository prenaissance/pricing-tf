namespace PricingTf.Processing.Tests.TestUtils

open PricingTf.Processing.Events
open PricingTf.Common.Models
open System

module TestData =

    let getExamplePricingEvent () =
        { id = Convert.FromHexString "654780b6b179b639d30bf17a"
          event = ListingUpdate
          payload =
            { id = "440_76561199115825381_5f2206613542100070a8b2421512c640"
              steamid = "76561199115825381"
              appid = 440
              currencies =
                { metal = 51.22<metal>
                  keys = 27.0<keys> }
              value =
                { raw = 1518.13
                  short = "27.94 keys"
                  long = "27 keys, 51.22 ref" }
              tradeOffersPreferred = true
              buyoutOnly = true
              details =
                Some "I am buying your Perennial Petals Reel Fly Hat for 27 keys, 51.22 ref, I have 0 / 1. Peace Out"
              listedAt = 1699184822
              bumpedAt = 1699184822
              intent = Buy
              count = 1
              status = "active"
              source = "userAgent"
              item =
                { appid = 440
                  baseName = "Reel Fly Hat"
                  defindex = Some 61
                  id = "5f7b1b4c4dd7f6c3a8a7b2a0"
                  imageUrl = "https://files.backpack.tf/images/440/61.png"
                  marketName = "The Ambassador"
                  name = "The Ambassador"
                  origin = None
                  originalId = "5f7b1b4c4dd7f6c3a8a7b2a0"
                  quality =
                    { id = 6
                      name = "Unique"
                      color = "FFD700" }
                  summary = "Level 1 Pistol"
                  level = 1
                  killstreakTier = 0
                  _class = [ "pistol" ]
                  slot = "secondary"
                  particle = None
                  tradable = true
                  craftable = true
                  sheen = None
                  killstreaker = None }
              userAgent =
                Some
                    { client = "backpack.tf"
                      lastPulse = 1601902804 }
              user =
                { id = "asdasdasdas"
                  name = "backpack.tf"
                  avatar = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avat"
                  avatarFull = "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/avat"
                  premium = false
                  online = false
                  banned = false
                  customNameStyle = ""
                  acceptedSuggestions = 0
                  _class = ""
                  style = ""
                  tradeOfferUrl = ""
                  isMarketplaceSeller = false
                  flagImpersonated = false
                  bans = [] } } }
