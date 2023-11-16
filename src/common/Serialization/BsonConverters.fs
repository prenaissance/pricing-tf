namespace PricingTf.Common.Serialization

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
