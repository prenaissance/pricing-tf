use futures_util::StreamExt;
use tokio_tungstenite::tungstenite::Message;

use pricing_tf::api::{block_user_service::BlockUserService, pricing_service::PricingService};
use pricing_tf::backpack_tf_api::BackpackTfApi;
use pricing_tf::config::AppConfig;
use pricing_tf::processing::etl;
use pricing_tf::processing::pricing_event::{ListingType, PricingEvent};
use pricing_tf::protos::pricing_tf::block_user_service::block_user_service_server::BlockUserServiceServer;
use pricing_tf::protos::pricing_tf::pricing_service::pricing_service_server::PricingServiceServer;

const WS_URL: &str = "wss://ws.backpack.tf/events";
const FILE_DESCRIPTOR_SET: &[u8] = tonic::include_file_descriptor_set!("pricing_tf_descriptor");

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenvy::dotenv()?;
    let app_config = AppConfig::from_env();
    tracing_subscriber::fmt()
        .with_max_level(tracing::Level::from(&app_config.log_level))
        .init();
    let addr = format!("0.0.0.0:{}", app_config.port).parse()?;

    let (_tx, _rx) = tokio::sync::mpsc::unbounded_channel::<PricingEvent>();
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
                    let (upserts, deletes) = events
                        .into_iter()
                        .filter(|event| !etl::is_spelled_item(event))
                        .filter(|event| !etl::is_unusual_weapon(event))
                        .partition::<Vec<_>, _>(|event| event.event == ListingType::ListingUpdate);
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
    });

    let backpack_tf_api = BackpackTfApi::new(&app_config.backpack_tf_cookie);
    let exchange_rate = backpack_tf_api.get_key_exchange_rate().await?;
    tracing::info!("Current key exchange rate: {:.2} ref", exchange_rate);

    let reflection_service = tonic_reflection::server::Builder::configure()
        .register_encoded_file_descriptor_set(FILE_DESCRIPTOR_SET)
        .build_v1()?;

    tonic::transport::Server::builder()
        .add_service(PricingServiceServer::new(PricingService {}))
        .add_service(BlockUserServiceServer::new(BlockUserService {}))
        .add_service(reflection_service)
        .serve(addr)
        .await?;

    read_handle.await?;

    Ok(())
}
