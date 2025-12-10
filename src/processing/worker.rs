use futures_util::StreamExt;
use tokio_tungstenite::tungstenite::Message;

use crate::backpack_tf_api::exchange_rate_controller::CachedExchangeRate;
use crate::db::AsyncDbPool;

use super::etl;
use super::pricing_event::{ListingType, PricingEvent};

const WS_URL: &str = "wss://ws.backpack.tf/events";

pub async fn run(
    pool: AsyncDbPool,
    cached_exchange_rate: CachedExchangeRate,
) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
    let (ws_stream, _response) = tokio_tungstenite::connect_async(WS_URL).await?;
    tracing::info!("Connected to WS server");
    let (_, mut read) = ws_stream.split();
    while let Some(Ok(msg)) = read.next().await {
        match msg {
            Message::Text(text) => {
                let events: Vec<PricingEvent> = match serde_json::from_str(&text) {
                    Ok(ev) => ev,
                    Err(e) => {
                        let file_name = format!(
                            "failed_ws_message_{}.json",
                            chrono::Utc::now().timestamp_millis()
                        );
                        tokio::fs::create_dir_all("/tmp/pricing_tf")
                            .await
                            .expect("Failed to create /tmp/pricing_tf directory");
                        tokio::fs::write(format!("/tmp/pricing_tf/{}", file_name), text.as_bytes())
                            .await
                            .expect("Failed to write log to /tmp/pricing_tf");
                        tracing::error!(
                            "Failed to deserialize message: {}\nJSON stored in /tmp/pricing_tf/{}",
                            e,
                            file_name
                        );

                        continue;
                    }
                };
                let (upserts, deletes) = events
                    .into_iter()
                    .filter(|event| !etl::is_spelled_item(event))
                    .filter(|event| !etl::is_unusual_weapon(event))
                    .partition::<Vec<_>, _>(|event| event.event == ListingType::ListingUpdate);

                let upserts_len = upserts.len();
                let deletes_len = deletes.len();

                let mut connection = pool.get().await?;

                if !upserts.is_empty() {
                    let exchange_rate = cached_exchange_rate.lock().await.rate;

                    let new_listings: Vec<_> = upserts
                        .into_iter()
                        .map(|event| event.to_trade_listing(exchange_rate))
                        .collect();

                    etl::upsert_trade_listings(&mut connection, new_listings).await?;
                }
                if !deletes.is_empty() {
                    let ids_to_delete: Vec<String> =
                        deletes.into_iter().map(|event| event.payload.id).collect();

                    etl::delete_trade_listings(&mut connection, ids_to_delete).await?;
                }

                tracing::debug!(
                    "Successfully processed an event batch of {} upserts and {} deletes",
                    upserts_len,
                    deletes_len
                );
            }
            Message::Close(_) => {
                tracing::info!("WS Server closed connection.");
            }
            msg => {
                if msg.is_empty() {
                    tracing::trace!("Received empty message");
                    continue;
                }
                tracing::warn!("Unhandled message received: {}", msg);
            }
        }
    }
    Ok(())
}
