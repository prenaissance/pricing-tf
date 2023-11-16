using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MongoDB.Driver;
using PricingTf.Common.Configuration;
using PricingTf.WebApi.Models.PricedItem;

namespace PricingTf.WebApi.Services;

public class PricingService : Pricing.PricingBase
{
    private readonly ILogger<PricingService> _logger;
    private readonly IMongoCollection<PricedItem> _pricesCollection;
    private readonly IMongoCollection<PricedItem> _botPricesCollection;

    public PricingService(ILogger<PricingService> logger, IMongoDatabase db)
    {
        _logger = logger;
        _pricesCollection = db.GetCollection<PricedItem>(PricingCollection.PricesView);
        _botPricesCollection = db.GetCollection<PricedItem>(PricingCollection.BotPricesView);
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
}
