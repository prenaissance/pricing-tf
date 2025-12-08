use crate::backpack_tf_api::payloads::{BackpackTfApiError, SnapshotResponse};
use crate::models::trade_listing::ListingIntent;

const SNAPSHOT_URL: &str = "https://backpack.tf/api/classifieds/listings/snapshot";
const MANNCO_SUPPLY_CRATE_KEY: &str = "Mann Co. Supply Crate Key";

pub struct BackpackTfApi<'a> {
    cookie: &'a str,
    client: reqwest::Client,
}

impl<'a> BackpackTfApi<'a> {
    pub fn new(cookie: &'a str) -> Self {
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
            .header("Cookie", self.cookie)
            .send()
            .await?
            .json()
            .await?;

        let mut sell_listings_iter = response
            .listings
            .iter()
            .filter(|listing| listing.intent == ListingIntent::Sell);

        let mut price_mode = sell_listings_iter
            .next()
            .ok_or(BackpackTfApiError::NoSellListings)?
            .price;
        let mut current_diff = 1;

        for listing in sell_listings_iter {
            if listing.price == price_mode {
                current_diff += 1;
            } else if current_diff == 0 {
                price_mode = listing.price;
                current_diff = 1;
            } else {
                current_diff -= 1;
            }
        }

        Ok(price_mode)
    }
}
