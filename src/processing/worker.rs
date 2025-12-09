use futures_util::StreamExt;
use tokio_tungstenite::tungstenite::Message;

use crate::db::AsyncDbPool;

use super::etl;
use super::pricing_event::{ListingType, PricingEvent};

const WS_URL: &str = "wss://ws.backpack.tf/events";

pub async fn run(pool: AsyncDbPool) -> Result<(), Box<dyn std::error::Error + Send + Sync>> {
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
                        tracing::error!(
                            "Failed to deserialize message: {}\nJSON stored in /var/log/pricing_tf/{}",
                            e,
                            file_name
                        );
                        tokio::fs::create_dir_all("/var/log/pricing_tf")
                            .await
                            .expect("Failed to create /var/log/pricing_tf directory");
                        tokio::fs::write(
                            format!("/var/log/pricing_tf/{}", file_name),
                            text.as_bytes(),
                        )
                        .await
                        .expect("Failed to write log to /var/log/pricing_tf");

                        continue;
                    }
                };
                let (upserts, deletes) = events
                    .into_iter()
                    .filter(|event| !etl::is_spelled_item(event))
                    .filter(|event| !etl::is_unusual_weapon(event))
                    .partition::<Vec<_>, _>(|event| event.event == ListingType::ListingUpdate);

                let connection = pool.get().await?;

                if !upserts.is_empty() {
                    use crate::schema::trade_listings::dsl::*;
                }
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
