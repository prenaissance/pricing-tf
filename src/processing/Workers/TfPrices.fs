namespace PricingTf.Processing.Workers

module TfPrices =
    open MongoDB.Driver
    open MongoDB.Bson
    open PricingTf.Processing.Models

    [<Literal>]
    let private filterOutHumanListingsStage =
        """
{
  $match: {
    isAutomatic: true,
  }
}"""

    [<Literal>]
    let private groupStage =
        """
{
  $group: {
    _id: "$itemName",
    buyListings: {
      $push: {
        $cond: {
          if: {
            $eq: ["$intent", "buy"],
          },
          then: "$$ROOT",
          else: null,
        },
      },
    },
    sellListings: {
      $push: {
        $cond: {
          if: {
            $eq: ["$intent", "sell"],
          },
          then: "$$ROOT",
          else: null,
        },
      },
    },
  },
}"""

    [<Literal>]
    let private projectFilteredListingsStage =
        """
{
  $project: {
    buyListings: {
      $filter: {
        input: "$buyListings",
        as: "listing",
        cond: {
          $ne: ["$$listing", null],
        },
      },
    },
    sellListings: {
      $filter: {
        input: "$sellListings",
        as: "listing",
        cond: {
          $ne: ["$$listing", null],
        },
      },
    },
  },
}"""

    [<Literal>]
    let private addPricesFieldsStage =
        """
{
  $addFields: {
    minBuyPrice: {
      $ifNull: [
        {
          $min: "$sellListings.price.keys",
        },
        9999,
      ],
    },
    maxSellPrice: {
      $ifNull: [
        {
          $max: "$buyListings.price.keys",
        },
        0,
      ],
    },
  },
}"""

    [<Literal>]
    let private projectFinalListingStage =
        """
{
  $project: {
    buyListing: {
      $cond: {
        if: {
          $and: [
            {
              $ne: ["$minBuyPrice", 9999],
            },
            {
              $ne: ["$sellListings", []],
            },
          ],
        },
        then: {
          $arrayElemAt: [
            {
              $filter: {
                input: "$sellListings",
                as: "listing",
                cond: {
                  $and: [
                    {
                      $ne: ["$$listing", null],
                    },
                    {
                      $eq: [
                        "$$listing.price.keys",
                        "$minBuyPrice",
                      ],
                    },
                  ],
                },
              },
            },
            0,
          ],
        },
        else: null,
      },
    },
    sellListing: {
      $cond: {
        if: {
          $and: [
            {
              $ne: ["$maxSellPrice", 0],
            },
            {
              $ne: ["$buyListings", []],
            },
          ],
        },
        then: {
          $arrayElemAt: [
            {
              $filter: {
                input: "$buyListings",
                as: "listing",
                cond: {
                  $and: [
                    {
                      $ne: ["$$listing", null],
                    },
                    {
                      $eq: [
                        "$$listing.price.keys",
                        "$maxSellPrice",
                      ],
                    },
                  ],
                },
              },
            },
            0,
          ],
        },
        else: null,
      },
    },
  },
}"""

    [<Literal>]
    let private projectFinalDataStage =
        """
{
  $project: {
    name: "$_id",
    buy: {
      $cond: {
        if: {
          $eq: ["$buyListing", null],
        },
        then: null,
        else: {
          price: "$buyListing.price.keys",
          tradeDetails:
            "$buyListing.tradeDetails",
        },
      },
    },
    sell: {
      $cond: {
        if: {
          $eq: ["$sellListing", null],
        },
        then: null,
        else: {
          price: "$sellListing.price.keys",
          tradeDetails:
            "$sellListing.tradeDetails",
        },
      },
    },
  },
}"""

    let private getOutputViewStage viewName =
        sprintf
            """
            {
              $out: "%s",
            }"""
            viewName

    let refreshPricesView (collection: IMongoCollection<TradeListing>) =
        let pipeline =
            EmptyPipelineDefinition<TradeListing>()
                .AppendStage(groupStage)
                .AppendStage(projectFilteredListingsStage)
                .AppendStage(addPricesFieldsStage)
                .AppendStage(projectFinalListingStage)
                .AppendStage(projectFinalDataStage)
                .AppendStage(getOutputViewStage "tf-prices")

        let options = AggregateOptions()
        options.AllowDiskUse <- true

        collection.AggregateToCollectionAsync(pipeline, options = options)
        |> Async.AwaitTask

    let refreshBotsPricesView (collection: IMongoCollection<TradeListing>) =
        let pipeline =
            EmptyPipelineDefinition<TradeListing>()
                .AppendStage(filterOutHumanListingsStage)
                .AppendStage(groupStage)
                .AppendStage(projectFilteredListingsStage)
                .AppendStage(addPricesFieldsStage)
                .AppendStage(projectFinalListingStage)
                .AppendStage(projectFinalDataStage)
                .AppendStage(getOutputViewStage "tf-bots-prices")

        let options = AggregateOptions()
        options.AllowDiskUse <- true

        collection.AggregateToCollectionAsync(pipeline, options = options)
        |> Async.AwaitTask
