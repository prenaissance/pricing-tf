use diesel_async::AsyncPgConnection;
use diesel_async::pooled_connection::AsyncDieselConnectionManager;
use diesel_async::pooled_connection::deadpool::Pool;

use pricing_tf::api::{block_user_service::BlockUserService, pricing_service::PricingService};
use pricing_tf::backpack_tf_api::BackpackTfApi;
use pricing_tf::backpack_tf_api::exchange_rate_controller::ExchangeRateController;
use pricing_tf::config::AppConfig;
use pricing_tf::db;
use pricing_tf::protos::pricing_tf::block_user_service::block_user_service_server::BlockUserServiceServer;
use pricing_tf::protos::pricing_tf::pricing_service::pricing_service_server::PricingServiceServer;
use tracing_subscriber::EnvFilter;

const FILE_DESCRIPTOR_SET: &[u8] = tonic::include_file_descriptor_set!("pricing_tf_descriptor");

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenvy::dotenv()?;
    let app_config = AppConfig::from_env();
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::from_default_env())
        .init();
    let addr = format!("0.0.0.0:{}", app_config.port).parse()?;

    let db_config = AsyncDieselConnectionManager::<AsyncPgConnection>::new(&app_config.db_url);
    let pool = Pool::builder(db_config).build()?;

    let backpack_tf_api = BackpackTfApi::new(app_config.backpack_tf_cookie.clone());
    let exchange_rate_controller = ExchangeRateController::init(backpack_tf_api)
        .await
        .inspect_err(|err| {
            tracing::error!(
                "Failed to get an initial key exchange rate: {}. Cannot recover.",
                err
            );
        })?;
    let exchange_rate = exchange_rate_controller.exchange_rate().await;
    tracing::info!("Current key exchange rate: {:.2} ref", exchange_rate);

    let ws_processing_handle = tokio::spawn(pricing_tf::processing::worker::run(
        pool.clone(),
        exchange_rate_controller.cached_exchange_rate.clone(),
    ));

    let materialized_views_handle = tokio::spawn(
        db::workers::run_refresh_materialized_views_worker(pool.clone()),
    );
    let delete_ttl_listings_handle = tokio::spawn(db::workers::run_delete_ttl_listings_worker(
        pool.clone(),
        app_config.listings_ttl_hours,
    ));

    let reflection_service = tonic_reflection::server::Builder::configure()
        .register_encoded_file_descriptor_set(FILE_DESCRIPTOR_SET)
        .build_v1()?;

    let grpc_server_handler = tonic::transport::Server::builder()
        .add_service(PricingServiceServer::new(PricingService::new(
            pool.clone(),
            exchange_rate_controller.cached_exchange_rate.clone(),
        )))
        .add_service(BlockUserServiceServer::new(BlockUserService {}))
        .add_service(reflection_service)
        .serve(addr);

    let exchange_rate_polling_handler =
        tokio::spawn(async move { exchange_rate_controller.poll_key_exchange_rate().await });

    let join_handles = tokio::join!(
        ws_processing_handle,
        grpc_server_handler,
        exchange_rate_polling_handler,
        materialized_views_handle,
        delete_ttl_listings_handle
    );

    if let Err(err) = join_handles.0 {
        tracing::error!("WS processing task failed: {}", err);
    }
    if let Err(err) = join_handles.1 {
        tracing::error!("gRPC server failed: {}", err);
    }
    if let Err(err) = join_handles.2 {
        tracing::error!("Exchange rate polling task failed: {}", err);
    }
    if let Err(err) = join_handles.3 {
        tracing::error!("Materialized views worker failed: {}", err);
    }
    if let Err(err) = join_handles.4 {
        tracing::error!("Delete TTL listings worker failed: {}", err);
    }

    Ok(())
}
