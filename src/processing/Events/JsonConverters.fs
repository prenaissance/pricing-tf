namespace PricingTf.Processing.Events

open System.Text.Json.Serialization
open System.Text.Json

module BpTfEventsConverters =
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

    type OptionConverter<'T>() =
        inherit JsonConverter<'T option>()

        override _.Read(reader, _, _) =
            match reader.TokenType with
            | JsonTokenType.Null
            | JsonTokenType.None -> None
            | _ ->
                let value = JsonSerializer.Deserialize<'T>(&reader)
                Some value

        override _.Write(writer, value, _) =
            match value with
            | Some value -> JsonSerializer.Serialize(writer, value)
            | None -> writer.WriteNullValue()

    let jsonOptions =
        let options = JsonSerializerOptions()
        options.Converters.Add(ListingIntentConverter())
        options.Converters.Add(ListingTypeConverter())
        options.Converters.Add(BooleanDefaultFalseConverter())
        options.Converters.Add(OptionConverter<SteamPrice>())
        options.Converters.Add(OptionConverter<CommunityPrice>())
        options.Converters.Add(OptionConverter<SuggestedPrice>())
        options.Converters.Add(OptionConverter<Origin>())
        options.WriteIndented <- true
        options
