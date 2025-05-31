namespace PricingTf.Processing.Events

open System.Text.Json.Serialization
open System.Text.Json

module BpTfEventsConverters =
    open PricingTf.Common.Models
    open PricingTf.Common.Serialization

    type ListingIntentConverter() =
        inherit JsonConverter<ListingIntent>()

        override _.Read(reader, _, _) =
            let value = reader.GetString()

            match value with
            | "buy" -> Buy
            | "sell" -> Sell
            | _ -> raise <| JsonException("Invalid listing intent")

        override _.Write(writer, value, _) =
            let value =
                match value with
                | Buy -> "buy"
                | Sell -> "sell"

            writer.WriteStringValue(value)

    type ListingTypeConverter() =
        inherit JsonConverter<ListingType>()

        override _.Read(reader, _, _) =
            let value = reader.GetString()

            match value with
            | "listing-update" -> ListingUpdate
            | "listing-delete" -> ListingDelete
            | _ -> raise <| JsonException("Invalid listing type")

        override _.Write(writer, value, _) =
            let value =
                match value with
                | ListingUpdate -> "listing-update"
                | ListingDelete -> "listing-delete"

            writer.WriteStringValue(value)

    // if null converts to false
    type BooleanDefaultFalseConverter() =
        inherit JsonConverter<bool>()

        override _.Read(reader, _, _) =
            if reader.TokenType = JsonTokenType.True then
                true
            else
                false

        override _.Write(writer, value, _) = writer.WriteBooleanValue(value)

    let jsonOptions =
        let options = JsonSerializerOptions()
        options.Converters.Add(ListingIntentConverter())
        options.Converters.Add(ListingTypeConverter())
        options.Converters.Add(BooleanDefaultFalseConverter())
        options.Converters.Add(OptionConverterFactory())
        options.WriteIndented <- true
        options
