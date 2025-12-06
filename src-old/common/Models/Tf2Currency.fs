namespace PricingTf.Common.Models

[<Measure>]
type keys

[<Measure>]
type metal

type Keys = float<keys>
type Metal = float<metal>
type ExchangeRate = float<metal / keys>

[<CLIMutable>]
type Tf2Currency = { metal: Metal; keys: Keys }

module Tf2Currency =
    /// Provides both precalculated values
    let from metal keys = 
        { metal = metal
          keys = keys }

    let toMetal exchangeRate currency =
        currency.metal + currency.keys * exchangeRate

    let toKeys exchangeRate currency =
        currency.keys + currency.metal / exchangeRate
