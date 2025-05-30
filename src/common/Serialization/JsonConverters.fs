namespace PricingTf.Common.Serialization

open System
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

type OptionConverterFactory() =
    inherit JsonConverterFactory()

    override _.CanConvert(t: Type) =
        t.IsGenericType && t.GetGenericTypeDefinition() = typedefof<option<_>>

    override _.CreateConverter(typeToConvert: Type, options: JsonSerializerOptions) =
        let innerType = typeToConvert.GetGenericArguments().[0]
        let convType = typedefof<OptionConverter<_>>.MakeGenericType innerType
        Activator.CreateInstance convType :?> JsonConverter
