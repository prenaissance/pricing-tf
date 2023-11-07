namespace PricingTf.Processing.Models

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
    let fromKeys keys exchangeRate =
        { metal = keys * exchangeRate
          keys = keys }

    let fromMetal metal exchangeRate =
        { metal = metal
          keys = metal / exchangeRate }

    let fromMixed metal keys (exchangeRate: ExchangeRate) =
        { metal = metal + keys * exchangeRate
          keys = keys + metal / exchangeRate }
