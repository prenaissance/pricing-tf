# Pricing.tf

## Usage

This module is supposed to be self hosted as a service in your own application, but a public hosted version will be available at some point. Several docker images are available.

## Services

### Processing

This service processes the web socket event stream from backpack.tf to produce a filtered and transformed copy of their database, as well as self-refreshing materialized view for the lowest buy price and highest sell price for every item.
If the scopes of the service are not enough for your use-case, fell free to fork this repository and modify the code under `src/processing`.

Image name: `prenaissance/pricing-tf-processing`

Environment variables:

---

| Name             | Description                           | Default value             |
| ---------------- | ------------------------------------- | ------------------------- |
| MongoDbUrl       | The url of the mongo database         | mongodb://localhost:27017 |
| MongoDbName      | The name of the mongo database        | backpack-tf-replica       |
| BackpackTfCookie | The cookie from a backpack.tf session | **REQUIRED**              |

### API

This services provides a gRPC and a REST API to query for item prices or listings.

WIP: Not yet implemented
