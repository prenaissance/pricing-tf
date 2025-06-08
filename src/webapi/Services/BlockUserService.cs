using Grpc.Core;
using MongoDB.Driver;
using PricingTf.Common.Configuration;
using PricingTf.Common.Models;
using PricingTf.WebApi.Protos;

namespace PricingTf.WebApi.Services;

public class BlockUserService : Protos.BlockUserService.BlockUserServiceBase
{
    private readonly ILogger<BlockUserService> logger;
    private readonly IMongoCollection<BlockedUser> blockedUsersCollection;
    private readonly IMongoCollection<TradeListing> tradeListingsCollection;

    public BlockUserService(ILogger<BlockUserService> logger, IMongoDatabase db)
    {
        this.logger = logger;
        blockedUsersCollection = db.GetCollection<BlockedUser>(PricingCollection.BlockedUsers);
        tradeListingsCollection = db.GetCollection<TradeListing>(PricingCollection.TradeListings);
    }

    public override async Task<BlockUserResponse> BlockUser(BlockUserRequest request, ServerCallContext context)
    {
        BlockedUser blockedUser = new(
            steamId: request.SteamId,
            blockedAt: DateTime.UtcNow
        );

        await blockedUsersCollection.UpdateOneAsync(
            (blockedUser) => blockedUser.steamId == request.SteamId,
            Builders<BlockedUser>.Update.Set(b => b.blockedAt, blockedUser.blockedAt),
            new() { IsUpsert = true }
        );

        logger.LogInformation("User {SteamId} blocked at {BlockedAt}", request.SteamId, blockedUser.blockedAt);

        var deletionResult = await tradeListingsCollection.DeleteManyAsync(
            Builders<TradeListing>.Filter.Eq(t => t.tradeDetails.user.steamId, request.SteamId),
            context.CancellationToken
        );

        return new()
        {
            Success = true,
            DeletedListingCount = (int)deletionResult.DeletedCount,
        };
    }

    public override async Task<UnblockUserResponse> UnblockUser(UnblockUserRequest request, ServerCallContext context)
    {
        var deletionResult = await blockedUsersCollection.DeleteOneAsync(
            Builders<BlockedUser>.Filter.Eq(b => b.steamId, request.SteamId),
            context.CancellationToken
        );

        if (deletionResult.DeletedCount == 0)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"User {request.SteamId} is not blocked"));
        }

        logger.LogInformation("User {SteamId} unblocked", request.SteamId);

        return new()
        {
            Success = true,
        };
    }
}