use futures_util::StreamExt;
use tokio_tungstenite::tungstenite::Message;

const WS_URL: &str = "wss://ws.backpack.tf/events";

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenvy::dotenv()?;
    tracing_subscriber::fmt::init();

    let (ws_stream, _response) = tokio_tungstenite::connect_async(WS_URL).await?;
    let (_, mut read) = ws_stream.split();
    tracing::info!("Connected to WS server");

    let read_handle = tokio::spawn(async move {
        while let Some(Ok(msg)) = read.next().await {
            match msg {
                Message::Text(text) => {
                    println!("{}", text);
                }
                Message::Close(_) => {
                    tracing::info!("WS Server closed connection.");
                }
                msg => {
                    tracing::warn!("Unhandled message received: {}", msg);
                }
            }
        }
    });

    read_handle.await?;

    Ok(())
}
