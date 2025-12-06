namespace PricingTf.Common.Serialization

open MongoDB.Bson
open MongoDB.Bson.Serialization
open MongoDB.Bson.Serialization.Serializers
open PricingTf.Common.Models

type ListingIntentSerializer() =
    inherit SerializerBase<ListingIntent>()

    override _.Serialize(context, _, value) =
        let writer = context.Writer
        writer.WriteString(ListingIntent.toString value)

    override _.Deserialize(context, args) =
        let reader = context.Reader
        let value = reader.ReadString()
        ListingIntent.fromString value

/// Serializes F# option<'T> as either a null or a bare 'T.
type OptionSerializer<'T>() =
    inherit SerializerBase<'T option>()

    override _.Serialize(context, args, value) =
        let writer = context.Writer

        match value with
        | Some x ->
            // write the inner value with its own serializer
            BsonSerializer.Serialize(writer, typeof<'T>, x)
        | None -> writer.WriteNull()

    override _.Deserialize(context, args) =
        let reader = context.Reader

        match reader.CurrentBsonType with
        | BsonType.Null ->
            reader.ReadNull() |> ignore
            None
        | _ ->
            // deserialize the inner T
            let inner = BsonSerializer.Deserialize<'T>(reader)
            Some inner
