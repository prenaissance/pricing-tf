namespace PricingTf.Processing.Tests.Utils

open Microsoft.VisualStudio.TestTools.UnitTesting
open PricingTf.Processing.Utils

[<TestClass>]
type StringUtilsTests() =
    [<TestMethod>]
    member _.``defaultIfEmpty should return default string when input is empty``() =
        let defaultString = "default"
        let emptyString = ""
        let result = StringUtils.defaultIfEmpty defaultString emptyString

        Assert.AreEqual(defaultString, result)

    [<TestMethod>]
    member _.``defaultIfEmpty should return input string when it is not empty``() =
        let defaultString = "default"
        let inputString = "input"
        let result = StringUtils.defaultIfEmpty defaultString inputString

        Assert.AreEqual(inputString, result)

    [<TestMethod>]
    member _.``defaultIfEmpty should return fallback string when input is null``() =
        let defaultString = "default"
        let inputString = null
        let result = StringUtils.defaultIfEmpty defaultString inputString

        Assert.AreEqual(defaultString, result)
