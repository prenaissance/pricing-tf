use diesel::prelude::*;

use crate::protos::pricing_tf::pricing_service;

#[derive(Debug, Queryable, Selectable)]
#[diesel(table_name = crate::schema::mv_prices)]
#[diesel(check_for_backend(diesel::pg::Pg))]
pub struct ItemPricingRow {
    pub item_name: String,
    pub buy_listings: serde_json::Value,
    pub sell_listings: serde_json::Value,
    pub updated_at: chrono::DateTime<chrono::Utc>,
}

impl From<ItemPricingRow> for pricing_service::ItemPricing {
    fn from(value: ItemPricingRow) -> Self {
        let buy_listings: Vec<pricing_service::ListingDetails> =
            serde_json::from_value(value.buy_listings).unwrap_or_default();
        let sell_listings: Vec<pricing_service::ListingDetails> =
            serde_json::from_value(value.sell_listings).unwrap_or_default();

        pricing_service::ItemPricing {
            name: value.item_name,
            buy: buy_listings.get(0).cloned(),
            sell: sell_listings.get(0).cloned(),
            buy_listings,
            sell_listings,
            updated_at: Some(value.updated_at.into()),
        }
    }
}
