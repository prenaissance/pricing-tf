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
                        tracing::error!("Failed to deserialize message: {}", e);
                        continue;
                    }
                };
                let (upserts, deletes) = events
                    .into_iter()
                    .filter(|event| !etl::is_spelled_item(event))
                    .filter(|event| !etl::is_unusual_weapon(event))
                    .partition::<Vec<_>, _>(|event| event.event == ListingType::ListingUpdate);

                let connection = pool.get().await?;

                if !upserts.is_empty() {}
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
