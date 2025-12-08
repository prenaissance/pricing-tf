use diesel_async::AsyncPgConnection;
use diesel_async::pooled_connection::AsyncDieselConnectionManager;
use diesel_async::pooled_connection::deadpool::Pool;
use pricing_tf::api::{block_user_service::BlockUserService, pricing_service::PricingService};
use pricing_tf::backpack_tf_api::BackpackTfApi;
use pricing_tf::config::AppConfig;
use pricing_tf::protos::pricing_tf::block_user_service::block_user_service_server::BlockUserServiceServer;
use pricing_tf::protos::pricing_tf::pricing_service::pricing_service_server::PricingServiceServer;

const FILE_DESCRIPTOR_SET: &[u8] = tonic::include_file_descriptor_set!("pricing_tf_descriptor");

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenvy::dotenv()?;
    let app_config = AppConfig::from_env();
    tracing_subscriber::fmt()
        .with_max_level(tracing::Level::from(&app_config.log_level))
        .init();
    let addr = format!("0.0.0.0:{}", app_config.port).parse()?;

    let db_config = AsyncDieselConnectionManager::<AsyncPgConnection>::new(&app_config.db_url);
    let pool = Pool::builder(db_config).build()?;

    let ws_processing_handle = tokio::spawn(pricing_tf::processing::worker::run(pool));

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

    if let Err(err) = ws_processing_handle.await? {
        tracing::error!("Websocket Processing worker failed: {}", err);
    }

    Ok(())
}
