use std::collections::HashMap;

use crate::backpack_tf_api::payloads::{BackpackTfApiError, SnapshotResponse};
use crate::models::trade_listing::ListingIntent;

const SNAPSHOT_URL: &str = "https://backpack.tf/api/classifieds/listings/snapshot";
pub const MANNCO_SUPPLY_CRATE_KEY: &str = "Mann Co. Supply Crate Key";

#[derive(Debug)]
pub struct BackpackTfApi {
    cookie: String,
    client: reqwest::Client,
}

impl BackpackTfApi {
    pub fn new(cookie: String) -> Self {
        let client = reqwest::Client::builder()
            .user_agent("PricingTF/4.0")
            .build()
            .unwrap();

        BackpackTfApi { cookie, client }
    }

    pub async fn get_key_exchange_rate(&self) -> Result<f64, BackpackTfApiError> {
        let params = [("sku", MANNCO_SUPPLY_CRATE_KEY), ("appid", "440")];

        let response: SnapshotResponse = self
            .client
            .get(SNAPSHOT_URL)
            .query(&params)
            .header("Cookie", &self.cookie)
            .send()
            .await?
            .json()
            .await?;

        let first_price = response
            .listings
            .first()
            .ok_or(BackpackTfApiError::NoSellListings)?
            .price;

        let sell_listings = response
            .listings
            .into_iter()
            .filter(|listing| listing.intent == ListingIntent::Sell);

        let mut counter = HashMap::<u16, u8>::new();
        for listing in sell_listings {
            // convert to metal "cents" to avoid floating point precision issues
            // e.g. 65.33 metal -> 6533
            let price = (listing.price * 100.0).round() as u16;
            let entry = counter.entry(price).or_insert(0);
            *entry += 1;
            if *entry >= 3 {
                return Ok(listing.price);
            }
        }

        // fallback simply the lowest sell price
        Ok(first_price)
    }
}
