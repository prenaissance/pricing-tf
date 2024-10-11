using System.Text.Json;
using dotnet_etcd;
using Grpc.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;
using PricingTf.Common.Serialization;
using PricingTf.WebApi.Configuration;
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

string GetHostname()
{
    var host = builder.Configuration.GetValue<string>("HOST");
    if (!string.IsNullOrEmpty(host))
    {
        return host;
    }
    var hostname = builder.Configuration.GetValue<string>("HOSTNAME");
    if (!string.IsNullOrEmpty(hostname))
    {
        return hostname;
    }

    return Environment.MachineName;
}

// service registration
async Task RegisterService()
{
    var etcdHosts = builder.Configuration.GetValue<string>("ETCD_HOSTS");
    if (string.IsNullOrEmpty(etcdHosts))
    {
        Console.WriteLine("ETCD_HOSTS is not set, skipping service registration");
        return;
    }

    var etcdClient = new EtcdClient(etcdHosts, configureChannelOptions: options =>
    {
        options.Credentials = ChannelCredentials.Insecure;
    });
    await etcdClient.PutAsync($"/services/pricing-tf/{GetHostname()}", JsonSerializer.Serialize(
        new
        {
            host = GetHostname(),
            healthCheck = new
            {
                port = 8080,
                path = "/healthz/liveness",
                delaySeconds = 10
            }
        }
    ));
    Console.WriteLine($"Service registered at /services/pricing-tf/{GetHostname()}");
}

await RegisterService();
app.Run();
