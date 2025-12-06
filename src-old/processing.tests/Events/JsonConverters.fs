namespace PricingTf.Processing.Tests.Events

open Microsoft.VisualStudio.TestTools.UnitTesting
open PricingTf.Processing.Events
open System.Text.Json
open PricingTf.Processing.Tests.TestUtils

[<TestClass>]
type JsonConvertersTests() =
    let jsonOptions = BpTfEventsConverters.jsonOptions




    [<TestMethod>]
    member _.``should serialize and deserialize a pricing event without throwing``() =
        let pricingEvent = TestData.getExamplePricingEvent ()
        let serialized = JsonSerializer.Serialize(pricingEvent, jsonOptions)
        JsonSerializer.Deserialize<PricingEvent>(serialized, jsonOptions) |> ignore
