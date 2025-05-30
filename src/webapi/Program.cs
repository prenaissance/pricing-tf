using System.Security.AccessControl;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.FSharp.Core;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using PricingTf.Common.Serialization;
using PricingTf.WebApi.Configuration;
using PricingTf.WebApi.Services;
using PricingTf.Common.Models;

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

builder.Services.AddGrpcHealthChecks()
                .AddCheck("Server", () => HealthCheckResult.Healthy());
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddGrpcReflection();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<PricingService>();
app.MapGrpcHealthChecksService()
    .AllowAnonymous();
app.MapGrpcReflectionService();

app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.MapGet("/healthz/liveness", () => "OK");

app.Run();
