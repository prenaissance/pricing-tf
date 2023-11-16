namespace PricingTf.WebApi.Configuration;

public record MongoDbConfiguration(
    string MongoDbUrl = "mongodb://localhost:27117",
    string MongoDbName = "backpack-tf-replica"
);