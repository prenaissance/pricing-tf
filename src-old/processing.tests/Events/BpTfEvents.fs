namespace PricingTf.Processing.Tests.Events

open Microsoft.VisualStudio.TestTools.UnitTesting
open PricingTf.Processing.Events
open System.Collections.Generic

[<TestClass>]
type EventsTests() =
    static member deserilizedData: IEnumerable<obj[]> =
        [ [| "listing-update"; ListingUpdate |]; [| "listing-delete"; ListingDelete |] ]

    [<TestMethod>]
    [<DynamicData(nameof (EventsTests.deserilizedData))>]
    member _.``EventsTests.parse should deserialize ListingType discriminated union``(input, expected) =
        let result = ListingType.parse input

        Assert.AreEqual(expected, result)

    [<TestMethod>]
    member _.``EventsTests.parse should throw exception when input is invalid``() =
        let input = "invalid"

        Assert.ThrowsException<System.Exception>(fun () -> ListingType.parse input |> ignore)
        |> ignore
