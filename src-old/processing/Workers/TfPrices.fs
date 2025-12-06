namespace PricingTf.Processing.Workers

module TfPrices =
    open MongoDB.Driver
    open PricingTf.Common.Models
    open PricingTf.Common.Configuration

    [<Literal>]
    let private filterOutHumanListingsStage =
        """
{
  $match: {
    isAutomatic: true,
  }
}"""

    [<Literal>]
    let private matchUnusualsStage =
        """
{
  $match: {
    item: {
      quality: "Unusual",
    },
  },
}"""

    [<Literal>]
    let private groupUnusualsStage =
        """
{
  $group: {
    _id: "$marketName",
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
    let private sortPricesStage =
        """
{
  $addFields: {
    buyListings: {
      $sortArray: {
        input: "$buyListings",
        sortBy: {
          priceKeys: -1,
          bumpedAt: -1
        }
      }
    },
    sellListings: {
      $sortArray: {
        input: "$sellListings",
        sortBy: {
          priceKeys: 1,
          bumpedAt: -1
        }
      }
    }
  }
}"""

    [<Literal>]
    let private projectFinalListingStage =
        """
{
  $addFields: {
    name: "$_id",
    buyListing: {
      $ifNull: [
        { $arrayElemAt: ["$buyListings", 0] },
        null
      ]
    },
    sellListing: {
      $ifNull: [
        { $arrayElemAt: ["$sellListings", 0] },
        null
      ]
    }
  }
}"""

    [<Literal>]
    let private pickListingFieldsStage =
        """
{
  $project: {
    _id: 0,
    itemName: 1,
    intent: 1,
    price: 1,
    priceKeys: 1,
    priceMetal: 1,
    tradeDetails: 1,
    bumpedAt: 1,
  }
}"""

    [<Literal>]
    let private updatedAtStage =
        """
{
  $addFields: {
    updatedAt: {
      $min: [
        "$sellListing.bumpedAt",
        "$buyListing.bumpedAt"
      ]
    }
  }
}"""

    let private getOutputViewStage viewName =
        sprintf
            """
            {
              $out: "%s",
            }"""
            viewName

    let private getMergeOutputViewStage viewName =
        sprintf
            """
            {
              $merge: {
                into: "%s",
                on: "_id",
                whenMatched: "replace",
                whenNotMatched: "insert",
              },
            }"""
            viewName

    let refreshPricesView (collection: IMongoCollection<TradeListing>) =
        let pipeline =
            EmptyPipelineDefinition<TradeListing>()
                .AppendStage(groupStage)
                .AppendStage(pickListingFieldsStage)
                .AppendStage(projectFilteredListingsStage)
                .AppendStage(sortPricesStage)
                .AppendStage(projectFinalListingStage)
                .AppendStage(updatedAtStage)
                .AppendStage(getOutputViewStage PricingCollection.PricesView)

        let unusualsPipeline =
            EmptyPipelineDefinition<TradeListing>()
                .AppendStage(matchUnusualsStage)
                .AppendStage(pickListingFieldsStage)
                .AppendStage(groupUnusualsStage)
                .AppendStage(projectFilteredListingsStage)
                .AppendStage(sortPricesStage)
                .AppendStage(projectFinalListingStage)
                .AppendStage(updatedAtStage)
                .AppendStage(getMergeOutputViewStage PricingCollection.PricesView)

        let options = AggregateOptions()
        options.AllowDiskUse <- true

        async {
            do!
                collection.AggregateToCollectionAsync(pipeline, options = options)
                |> Async.AwaitTask

            do!
                collection.AggregateToCollectionAsync(unusualsPipeline, options = options)
                |> Async.AwaitTask
        }

    let refreshBotsPricesView (collection: IMongoCollection<TradeListing>) =
        let pipeline =
            EmptyPipelineDefinition<TradeListing>()
                .AppendStage(filterOutHumanListingsStage)
                .AppendStage(pickListingFieldsStage)
                .AppendStage(groupStage)
                .AppendStage(projectFilteredListingsStage)
                .AppendStage(sortPricesStage)
                .AppendStage(projectFinalListingStage)
                .AppendStage(updatedAtStage)
                .AppendStage(getOutputViewStage PricingCollection.BotPricesView)

        let unusualsPipeline =
            EmptyPipelineDefinition<TradeListing>()
                .AppendStage(filterOutHumanListingsStage)
                .AppendStage(pickListingFieldsStage)
                .AppendStage(matchUnusualsStage)
                .AppendStage(groupUnusualsStage)
                .AppendStage(projectFilteredListingsStage)
                .AppendStage(sortPricesStage)
                .AppendStage(projectFinalListingStage)
                .AppendStage(updatedAtStage)
                .AppendStage(getMergeOutputViewStage PricingCollection.BotPricesView)

        let options = AggregateOptions()
        options.AllowDiskUse <- true

        collection.AggregateToCollectionAsync(pipeline, options = options)
        |> Async.AwaitTask
