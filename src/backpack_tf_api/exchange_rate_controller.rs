use std::sync::Arc;

use super::BackpackTfApi;
use super::payloads::BackpackTfApiError;

#[derive(Debug)]
pub struct ExchangeRate {
    pub rate: f64,
    pub updated_at: chrono::DateTime<chrono::Utc>,
}

impl ExchangeRate {
    pub fn new(rate: f64) -> Self {
        Self {
            rate,
            updated_at: chrono::Utc::now(),
        }
    }
}

pub type CachedExchangeRate = Arc<tokio::sync::Mutex<ExchangeRate>>;

pub struct ExchangeRateController {
    backpack_tf_api: BackpackTfApi,
    pub cached_exchange_rate: CachedExchangeRate,
}

impl ExchangeRateController {
    pub async fn init(backpack_tf_api: BackpackTfApi) -> Result<Self, BackpackTfApiError> {
        let initial_rate = backpack_tf_api.get_key_exchange_rate().await?;
        let cached_exchange_rate =
            Arc::new(tokio::sync::Mutex::new(ExchangeRate::new(initial_rate)));

        Ok(Self {
            backpack_tf_api,
            cached_exchange_rate,
        })
    }

    pub async fn exchange_rate(&self) -> f64 {
        let exchange_rate = self.cached_exchange_rate.lock().await;
        exchange_rate.rate
    }

    /// Polls the Backpack.tf API every 10 minutes to update the cached exchange rate
    pub async fn poll_key_exchange_rate(&self) {
        let mut interval = tokio::time::interval(std::time::Duration::from_mins(10));
        loop {
            interval.tick().await;
            let new_rate = match self.backpack_tf_api.get_key_exchange_rate().await {
                Ok(rate) => rate,
                Err(err) => {
                    tracing::error!(
                        "Failed to fetch key exchange rate: {}. Keeping the old rate cached.",
                        err
                    );
                    continue;
                }
            };
            let mut exchange_rate = self.cached_exchange_rate.lock().await;
            *exchange_rate = ExchangeRate::new(new_rate);
            tracing::info!("Updated key exchange rate: {:.2} ref", new_rate);
        }
    }
}
