use std::time::Duration;

use diesel::prelude::*;
use diesel_async::RunQueryDsl;

use crate::schema::trade_listings;

use super::AsyncDbPool;

const REFRESH_MATERIALIZED_VIEWS_INTERVAL_SECS: u64 = 60;
const DELETE_TTL_LISTINGS_INTERVAL_MINS: u64 = 5;

pub async fn run_refresh_materialized_views_worker(pool: AsyncDbPool) {
    let mut interval = tokio::time::interval(Duration::from_secs(
        REFRESH_MATERIALIZED_VIEWS_INTERVAL_SECS,
    ));

    loop {
        interval.tick().await;
        let mut connection = match pool.get().await {
            Ok(conn) => conn,
            Err(err) => {
                tracing::error!(
                    "Failed to connect to DB for refreshing materialized views: {}",
                    err
                );
                continue;
            }
        };

        let mv_prices_query =
            diesel::sql_query("refresh materialized view concurrently \"mv_prices\"");
        let mv_bot_prices_query =
            diesel::sql_query("refresh materialized view concurrently \"mv_bot_prices\"");

        match mv_prices_query.execute(&mut connection).await {
            Ok(_) => {
                tracing::debug!("Successfully refreshed materialized view mv_prices");
            }
            Err(err) => {
                tracing::error!("Failed to refresh materialized view mv_prices: {}", err);
            }
        }

        match mv_bot_prices_query.execute(&mut connection).await {
            Ok(_) => {
                tracing::debug!("Successfully refreshed materialized view mv_bot_prices");
            }
            Err(err) => {
                tracing::error!("Failed to refresh materialized view mv_bot_prices: {}", err);
            }
        }
    }
}

pub async fn run_delete_ttl_listings_worker(pool: AsyncDbPool, listings_ttl_hours: u32) {
    let mut interval =
        tokio::time::interval(Duration::from_mins(DELETE_TTL_LISTINGS_INTERVAL_MINS));

    loop {
        interval.tick().await;
        let mut connection = match pool.get().await {
            Ok(conn) => conn,
            Err(err) => {
                tracing::error!("Failed to connect to DB for deleting TTL listings: {}", err);
                continue;
            }
        };

        let result = diesel::delete(
            trade_listings::table.filter(
                trade_listings::bumped_at
                    .lt(chrono::Utc::now() - chrono::Duration::hours(listings_ttl_hours as i64)),
            ),
        )
        .execute(&mut connection)
        .await;

        match result {
            Ok(deleted_count) => {
                if deleted_count == 0 {
                    tracing::debug!("No expired TTL listings to delete");
                } else {
                    tracing::info!(
                        "Successfully deleted {} expired TTL listings",
                        deleted_count
                    );
                }
            }
            Err(err) => {
                tracing::error!("Failed to delete expired TTL listings: {}", err);
            }
        }
    }
}
