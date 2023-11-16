using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using PricingTf.Common.Models;
using PricingTf.Common.Serialization;
using PricingTf.WebApi;
using PricingTf.WebApi.Configuration;
using PricingTf.WebApi.Models.PricedItem;
using PricingTf.WebApi.Services;

var conventionPack = new ConventionPack { new CamelCaseElementNameConvention() };
ConventionRegistry.Register("camelCase", conventionPack, t => true);
BsonSerializer.RegisterSerializer(new ListingIntentSerializer());

var builder = WebApplication.CreateBuilder(args);
var mongoDbConfiguration = builder.Configuration.Get<MongoDbConfiguration>()!;
var mongoClient = new MongoClient(mongoDbConfiguration.MongoDbUrl);
var mongoDatabase = mongoClient.GetDatabase(mongoDbConfiguration.MongoDbName);

builder.Services.AddSingleton(mongoClient);
builder.Services.AddSingleton(mongoDatabase);
builder.Services.Configure<MongoDbConfiguration>(builder.Configuration);
builder.Services.Configure<BackpackTfConfiguration>(builder.Configuration);
builder.Services.AddGrpc().AddJsonTranscoding();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<PricingService>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
