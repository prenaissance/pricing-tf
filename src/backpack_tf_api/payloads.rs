use core::fmt;

use serde::Deserialize;

use crate::models::tf2_currency::Tf2Currency;
use crate::models::trade_listing::ListingIntent;

#[derive(Debug)]
pub enum BackpackTfApiError {
    RequestError(reqwest::Error),
    NoSellListings,
}

impl fmt::Display for BackpackTfApiError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            BackpackTfApiError::RequestError(e) => e.fmt(f),
            BackpackTfApiError::NoSellListings => write!(f, "No sell listings found"),
        }
    }
}

impl std::error::Error for BackpackTfApiError {}

impl From<reqwest::Error> for BackpackTfApiError {
    fn from(err: reqwest::Error) -> Self {
        BackpackTfApiError::RequestError(err)
    }
}

#[derive(Deserialize)]
pub struct SnapshotResponse {
    pub listings: Vec<SnapshotListing>,
}

/// More fields are present in the response, add if needed
#[derive(Deserialize)]
pub struct SnapshotListing {
    pub steamid: String,
    /// full metal conversion
    pub price: f64,
    pub intent: ListingIntent,
    pub currencies: Tf2Currency,
}
