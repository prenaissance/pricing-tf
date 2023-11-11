namespace PricingTf.Processing.Utils

module StringUtils =
    open System

    let defaultIfEmpty defaultString string =
        if String.IsNullOrWhiteSpace(string) then
            defaultString
        else
            string
