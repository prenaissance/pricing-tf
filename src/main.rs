use futures_util::StreamExt;
use pricing_tf::{config::AppConfig, processing::pricing_event::PricingEvent};
use tokio_tungstenite::tungstenite::Message;

const WS_URL: &str = "wss://ws.backpack.tf/events";

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenvy::dotenv()?;
    let app_config = AppConfig::from_env();
    tracing_subscriber::fmt()
        .with_max_level(tracing::Level::from(&app_config.log_level))
        .init();

    let (rx, tx) = tokio::sync::mpsc::unbounded_channel::<PricingEvent>();
    let (ws_stream, _response) = tokio_tungstenite::connect_async(WS_URL).await?;
    let (_, mut read) = ws_stream.split();
    tracing::info!("Connected to WS server");

    let read_handle = tokio::spawn(async move {
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
                    dbg!(&events[0]);
                }
                Message::Close(_) => {
                    tracing::info!("WS Server closed connection.");
                }
                msg => {
                    if msg.is_empty() {
                        tracing::warn!("Received empty message");
                        continue;
                    }
                    tracing::warn!("Unhandled message received: {}", msg);
                }
            }
        }
    });

    read_handle.await?;

    Ok(())
}
