namespace PricingTf.WebApi.Configuration;

public record MongoDbConfiguration(
    string MongoDbUrl,
    string MongoDbName
);