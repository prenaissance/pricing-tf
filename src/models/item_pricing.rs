use crate::protos::pricing_tf::pricing;
use diesel::prelude::*;

macro_rules! define_pricing_row {
    ($struct_name:ident, $table_module:path) => {
        #[derive(Debug, Queryable, Selectable)]
        #[diesel(table_name = $table_module)]
        #[diesel(check_for_backend(diesel::pg::Pg))]
        pub struct $struct_name {
            pub item_name: String,
            pub buy_listings: serde_json::Value,
            pub sell_listings: serde_json::Value,
            pub updated_at: chrono::DateTime<chrono::Utc>,
        }

        impl From<$struct_name> for pricing::v1::ItemPricing {
            fn from(value: $struct_name) -> Self {
                let buy_listings: Vec<pricing::v1::ListingDetails> =
                    serde_json::from_value(value.buy_listings).unwrap_or_default();
                let sell_listings: Vec<pricing::v1::ListingDetails> =
                    serde_json::from_value(value.sell_listings).unwrap_or_default();

                pricing::v1::ItemPricing {
                    name: value.item_name,
                    buy: buy_listings.get(0).cloned(),
                    sell: sell_listings.get(0).cloned(),
                    buy_listings,
                    sell_listings,
                    updated_at: Some(value.updated_at.into()),
                }
            }
        }
    };
}

define_pricing_row!(ItemPricingRow, crate::schema::mv_prices);
define_pricing_row!(BotItemPricingRow, crate::schema::mv_bot_prices);
