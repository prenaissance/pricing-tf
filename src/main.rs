use diesel_async::AsyncPgConnection;
use diesel_async::async_connection_wrapper::AsyncConnectionWrapper;
use diesel_async::pooled_connection::AsyncDieselConnectionManager;
use diesel_async::pooled_connection::deadpool::Pool;

use diesel_migrations::{EmbeddedMigrations, MigrationHarness, embed_migrations};
use pricing_tf::api::{block_user_service::BlockUserService, pricing_service::PricingService};
use pricing_tf::backpack_tf_api::BackpackTfApi;
use pricing_tf::backpack_tf_api::exchange_rate_controller::ExchangeRateController;
use pricing_tf::config::AppConfig;
use pricing_tf::db;
use pricing_tf::protos::pricing_tf::blocked_users::v1::block_user_service_server::BlockUserServiceServer;
use pricing_tf::protos::pricing_tf::pricing::v1::pricing_service_server::PricingServiceServer;
use tracing_subscriber::EnvFilter;

const FILE_DESCRIPTOR_SET: &[u8] = tonic::include_file_descriptor_set!("pricing_tf_descriptor");
const MIGRATIONS: EmbeddedMigrations = embed_migrations!();

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    dotenvy::dotenv().ok();
    let app_config = AppConfig::from_env();
    tracing_subscriber::fmt()
        .with_env_filter(EnvFilter::from_default_env())
        .init();
    let addr = format!("0.0.0.0:{}", app_config.port).parse()?;

    let db_config = AsyncDieselConnectionManager::<AsyncPgConnection>::new(&app_config.db_url);
    let pool = Pool::builder(db_config).build()?;
    let mut sync_connection: AsyncConnectionWrapper<_> = pool.get().await?.into();
    let migrations_result = tokio::task::spawn_blocking(move || {
        sync_connection
            .run_pending_migrations(MIGRATIONS)
            .expect("Failed to run migrations within spawned task");
    })
    .await;
    if let Err(err) = &migrations_result {
        tracing::error!("Failed to run database migrations: {}", err);
        migrations_result?;
    }

    let blocked_user_steam_ids = db::blocked_users::load_all_blocked_steam_ids(&pool).await?;

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
        blocked_user_steam_ids.clone(),
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

    let (_, health_service) = tonic_health::server::health_reporter();

    let grpc_server_handler = tonic::transport::Server::builder()
        .add_service(PricingServiceServer::new(PricingService::new(
            pool.clone(),
            exchange_rate_controller.cached_exchange_rate.clone(),
        )))
        .add_service(BlockUserServiceServer::new(BlockUserService::new(
            pool.clone(),
            blocked_user_steam_ids.clone(),
        )))
        .add_service(reflection_service)
        .add_service(health_service)
        .serve(addr);

    let exchange_rate_polling_handler =
        tokio::spawn(async move { exchange_rate_controller.poll_key_exchange_rate().await });

    tokio::select! {
        res = grpc_server_handler => {
            tracing::error!("gRPC server handler exited unexpectedly: {:?}", res);
        }
        res = ws_processing_handle => {
            tracing::error!("WebSocket processing handler exited unexpectedly: {:?}", res);
        }
        res = materialized_views_handle => {
            tracing::error!("Materialized views worker exited unexpectedly: {:?}", res);
        }
        res = delete_ttl_listings_handle => {
            tracing::error!("Delete TTL listings worker exited unexpectedly: {:?}", res);
        }
        res = exchange_rate_polling_handler => {
            tracing::error!("Exchange rate polling handler exited unexpectedly: {:?}", res);
        }
    };

    Ok(())
}
