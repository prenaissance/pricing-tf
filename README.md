# Pricing.tf

## Usage

This module is supposed to be self hosted as a service in your own application, but a public hosted version will be available at some point. Several docker images are available.

## Services

### Processing

This service processes the web socket event stream from backpack.tf to produce a filtered and transformed copy of their database, as well as self-refreshing materialized view for the lowest buy price and highest sell price for every item.
If the scopes of the service are not enough for your use-case, fell free to fork this repository and modify the code under `src/processing`.

Image name: `prenaissance/pricing-tf-processing`

Environment variables:

| Name             | Description                           | Default value             |
| ---------------- | ------------------------------------- | ------------------------- |
| MongoDbUrl       | The url of the mongo database         | mongodb://localhost:27017 |
| MongoDbName      | The name of the mongo database        | backpack-tf-replica       |
| BackpackTfCookie | The cookie from a backpack.tf session | **REQUIRED**              |
| ListingsTtlHours | How long to keep stale listings in db | 6                         |

The following collections are aggregated in the provided mongo database:

- `trade-listings` - replica of the backpack.tf listings database, with some information filtered out.
- `tf-prices` - materialized views of best buy and sell prices for each item
- `tf-bots-prices` - materialized views of the best buy and sell prices for each item, taking in account only bot listings

### API

This services provides a gRPC and a REST API to query for item prices or listings. This CRUD service is an extension to the processing service and requires connecting it to the same database.

WIP: REST Api not yet implemented

Image name: `prenaissance/pricing-tf-webapi` Exposed port: `8080`

Environment variables:

| Name             | Description                     | Default value             |
| ---------------- | ------------------------------- | ------------------------- |
| MongoDbUrl       | The url of the mongo database   | mongodb://localhost:27017 |
| MongoDbName      | The name of the mongo database  | backpack-tf-replica       |
| BackpackTfCookie | Used as fallback for key prices | `null`                    |

Contracts:

```protobuf
syntax = "proto3";

option csharp_namespace = "PricingTf.WebApi";
import "google/protobuf/timestamp.proto";
import "google/protobuf/empty.proto";

package pricingTf;

service Pricing {
  rpc GetPricing (ItemRequest) returns (ItemPricing);
  rpc GetBotPricing (ItemRequest) returns (ItemPricing);
  rpc GetKeyExchangeRate (google.protobuf.Empty) returns (KeyExchangeRate);
}

message ItemRequest {
  string name = 1;
}

message TradeDetails {
  string listingId = 1;
  string tradeOfferUrl = 2;
}

message PricingDetails {
  double price = 1;
  TradeDetails tradeDetails = 2;
}

message ItemPricing {
  string name = 1;
  optional PricingDetails buy = 2;
  optional PricingDetails sell = 3;
  google.protobuf.Timestamp updatedAt = 4;
}

enum KeyExchangeSource {
  Listings = 0;
  Snapshot = 1;
}

message KeyExchangeRate {
  double metal = 1;
  google.protobuf.Timestamp updatedAt = 2;
  KeyExchangeSource source = 3;
}
```

## Running locally

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/)
- [Docker Compose](https://docs.docker.com/compose/install/)
- [Dotnet SDK](https://dotnet.microsoft.com/download)

### Local Development

1. Run `docker compose -f=docker-compose.yml -f=docker-compose.dev.yml up -d`
2. For needed services copy `appsetings.json` : `cp src/processing/appsettings.json src/processing/appsettings.Development.json`
3. Replace needed env variables with yours
4. Run needed services, example for processing: `cd src/processing && dotnet run`

### Local Production run

1. Change needed env variables in `docker-compose.prod.yml`
2. Run `docker compose -f=docker-compose.yml -f=docker-compose.prod.yml up -d --build`

## TODO:

- [ ] Add Rest API, probably using grpc json transcoding
- [ ] Add generic unusual items in price aggregations
- [ ] Fix edge cases in which very seldomly the events cannot be deserialized
- [ ] Integrate some statistical analysis for the pricing formulas
