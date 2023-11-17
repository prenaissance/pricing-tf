using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using PricingTf.Common.Configuration;
using PricingTf.Common.Models;
using PricingTf.Processing.Services;
using PricingTf.WebApi.Configuration;
using PricingTf.WebApi.Models.PricedItem;

namespace PricingTf.WebApi.Services;

public class PricingService : Pricing.PricingBase
{
    private readonly ILogger<PricingService> _logger;
    private readonly IMongoCollection<PricedItem> _pricesCollection;
    private readonly IMongoCollection<PricedItem> _botPricesCollection;
    private readonly IMongoCollection<TradeListing> _tradeListingsCollection;
    private readonly BackpackTfConfiguration _backpackTfConfiguration;

    public PricingService(ILogger<PricingService> logger, IMongoDatabase db, IOptions<BackpackTfConfiguration> backpackTfConfiguration)
    {
        _logger = logger;
        _pricesCollection = db.GetCollection<PricedItem>(PricingCollection.PricesView);
        _botPricesCollection = db.GetCollection<PricedItem>(PricingCollection.BotPricesView);
        _tradeListingsCollection = db.GetCollection<TradeListing>(PricingCollection.TradeListings);
        _backpackTfConfiguration = backpackTfConfiguration.Value;
    }

    public override async Task<ItemPricing> GetPricing(ItemRequest request, ServerCallContext context)
    {
        var item = await _pricesCollection.Find(x => x.Id == request.Name).FirstOrDefaultAsync()
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Item {request.Name} not found"));

        return ItemPricing.FromPricedItem(item);
    }

    public override async Task<ItemPricing> GetBotPricing(ItemRequest request, ServerCallContext context)
    {
        var item = await _botPricesCollection.Find(x => x.Id == request.Name).FirstOrDefaultAsync()
            ?? throw new RpcException(new Status(StatusCode.NotFound, $"Item {request.Name} not found"));

        return ItemPricing.FromPricedItem(item);
    }

    private async Task<KeyExchangeRate?> GetExchangeFromListings()
    {
        var builder = Builders<TradeListing>.Filter;
        var listings = await _tradeListingsCollection
            .Find(builder.And(
                builder.Eq("itemName", BackpackTfApi.MANNCO_SUPPLY_CRATE_KEY),
                builder.Gte("bumpedAt", DateTime.UtcNow.AddHours(-1)),
                builder.Eq("isAutomatic", true),
                builder.Eq("intent", "sell")
            )).ToListAsync();

        var listing = listings.MinBy(x => x.price.metal);
        if (listing is null)
        {
            return null;
        }

        return new()
        {
            Metal = listing.price.metal,
            UpdatedAt = Timestamp.FromDateTime(listing.bumpedAt),
            Source = KeyExchangeSource.Listings
        };
    }


    public override async Task<KeyExchangeRate> GetKeyExchangeRate(Empty request, ServerCallContext context)
    {
        var listingPrice = await GetExchangeFromListings();
        if (listingPrice is not null)
        {
            return listingPrice;
        }
        _logger.LogDebug("Not enough information from collected events, falling back to backpack.tf snapshot api");
        string? cookie = _backpackTfConfiguration.BackpackTfCookie;
        if (string.IsNullOrEmpty(cookie))
        {
            throw new RpcException(new Status(
                StatusCode.Unavailable,
                "Could not get information from local listings and no cookie provided"));
        }

        return new()
        {
            Metal = await BackpackTfApi.GetKeyExchangeRateAsync(cookie),
            UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
            Source = KeyExchangeSource.Snapshot
        };
    }
}
