namespace PricingTf.Common.Serialization

open System.Text.Json
open System.Text.Json.Serialization

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
