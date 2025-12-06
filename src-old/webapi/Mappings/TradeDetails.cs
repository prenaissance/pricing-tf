namespace PricingTf.WebApi.Protos;

public sealed partial class TradeDetails
{
    public static TradeDetails FromTradeDetails(Models.PricedItem.TradeDetails tradeDetails)
    {
        TradeItemDetails item = new()
        {
            Name = tradeDetails.Item.Name,
            ImageUrl = tradeDetails.Item.ImageUrl,
            Quality = new ItemQuality
            {
                Id = tradeDetails.Item.Quality.id,
                Name = tradeDetails.Item.Quality.name,
                Color = tradeDetails.Item.Quality.color,
            },
            Particle = tradeDetails.Item.Particle is not null
                ? new ItemParticle
                {
                    Id = tradeDetails.Item.Particle.id,
                    Name = tradeDetails.Item.Particle.name,
                    ImageUrl = tradeDetails.Item.Particle.imageUrl,
                    ShortName = tradeDetails.Item.Particle.shortName,
                }
                : null,
        };
        TradeUserDetails user = new()
        {
            SteamId = tradeDetails.User.steamId,
            Name = tradeDetails.User.name,
            AvatarThumbnailUrl = tradeDetails.User.avatarThumbnailUrl,
            Online = tradeDetails.User.online,
        };

        return new TradeDetails
        {
            ListingId = tradeDetails.ListingId,
            TradeOfferUrl = tradeDetails.TradeOfferUrl,
            Description = tradeDetails.Description,
            Item = item,
            User = user,
        };
    }
}