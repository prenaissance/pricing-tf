use std::pin::Pin;

use super::errors::{diesel_error_to_status, pool_error_to_status};
use crate::backpack_tf_api::exchange_rate_controller::CachedExchangeRate;
use crate::backpack_tf_api::facade::MANNCO_SUPPLY_CRATE_KEY;
use crate::db::AsyncDbPool;
use crate::models::item_pricing::{BotItemPricingRow, ItemPricingRow};
use crate::models::trade_listing::{ListingIntent, TradeListing, TradeListingRow};
use crate::protos::pricing_tf::pricing_service;
use crate::schema::{mv_bot_prices, mv_prices, trade_listings};

use diesel::prelude::*;
use diesel_async::RunQueryDsl;
use futures_util::Stream;
use tonic::{Request, Response, Status};

const DEFAULT_STALE_LIMIT_SECS: i64 = 60 * 60 * 6; // 6 hours

pub struct PricingService {
    pool: AsyncDbPool,
    exchange_rate: CachedExchangeRate,
}

impl PricingService {
    pub fn new(pool: AsyncDbPool, exchange_rate: CachedExchangeRate) -> Self {
        Self {
            pool,
            exchange_rate,
        }
    }

    async fn get_exchange_rate_from_listings(&self) -> Option<TradeListing> {
        let mut connection = self.pool.get().await.ok()?;
        trade_listings::table
            .filter(trade_listings::item_name.eq(MANNCO_SUPPLY_CRATE_KEY))
            .filter(trade_listings::bumped_at.gt(chrono::Utc::now() - chrono::Duration::hours(1)))
            .filter(trade_listings::intent.eq(ListingIntent::Sell))
            .filter(trade_listings::is_automatic.eq(true))
            .order_by(trade_listings::price_metal)
            .select(TradeListingRow::as_select())
            .limit(1)
            .first(&mut connection)
            .await
            .ok()
            .map(TradeListing::from)
    }
}

#[tonic::async_trait]
impl pricing_service::pricing_service_server::PricingService for PricingService {
    async fn get_pricing(
        &self,
        request: Request<pricing_service::ItemRequest>,
    ) -> Result<Response<pricing_service::ItemPricing>, Status> {
        let name = request.into_inner().name;
        if name.is_empty() {
            return Err(Status::invalid_argument("Item name cannot be empty"));
        }

        let mut connection = self.pool.get().await.map_err(pool_error_to_status)?;
        mv_prices::table
            .filter(mv_prices::item_name.eq(name))
            .select(ItemPricingRow::as_select())
            .first::<ItemPricingRow>(&mut connection)
            .await
            .optional()
            .map_err(diesel_error_to_status)?
            .map(|row| Response::new(row.into()))
            .ok_or(Status::not_found("Not found"))
    }

    async fn get_bot_pricing(
        &self,
        request: Request<pricing_service::ItemRequest>,
    ) -> Result<Response<pricing_service::ItemPricing>, Status> {
        let name = request.into_inner().name;
        if name.is_empty() {
            return Err(Status::invalid_argument("Item name cannot be empty"));
        }

        let mut connection = self.pool.get().await.map_err(pool_error_to_status)?;
        mv_bot_prices::table
            .filter(mv_bot_prices::item_name.eq(name))
            .select(BotItemPricingRow::as_select())
            .first::<BotItemPricingRow>(&mut connection)
            .await
            .optional()
            .map_err(diesel_error_to_status)?
            .map(|row| Response::new(row.into()))
            .ok_or(Status::not_found("Not found"))
    }

    async fn delete_bot_pricing(
        &self,
        request: Request<pricing_service::DeleteBotPricingRequest>,
    ) -> Result<Response<pricing_service::DeleteBotPricingResponse>, Status> {
        let name = request.into_inner().name;
        if name.is_empty() {
            return Err(Status::invalid_argument("Item name cannot be empty"));
        }

        let mut connection = self.pool.get().await.map_err(pool_error_to_status)?;
        diesel::delete(mv_bot_prices::table.filter(mv_bot_prices::item_name.eq(name)))
            .execute(&mut connection)
            .await
            .map_err(diesel_error_to_status)?;

        Ok(Response::new(pricing_service::DeleteBotPricingResponse {}))
    }

    async fn get_key_exchange_rate(
        &self,
        _request: Request<()>,
    ) -> Result<Response<pricing_service::KeyExchangeRateResponse>, Status> {
        let listing = self.get_exchange_rate_from_listings().await;
        match listing {
            Some(listing) => Ok(Response::new(pricing_service::KeyExchangeRateResponse {
                metal: listing.original_price.metal,
                source: pricing_service::KeyExchangeSource::Listings as i32,
                updated_at: Some(prost_wkt_types::Timestamp {
                    seconds: listing.bumped_at.timestamp(),
                    nanos: 0,
                }),
            })),
            None => {
                tracing::debug!(
                    "Not enough information collected from events for key rate, using backpack.tf API."
                );
                let exchange_rate = self.exchange_rate.lock().await;
                Ok(Response::new(pricing_service::KeyExchangeRateResponse {
                    metal: exchange_rate.rate,
                    source: pricing_service::KeyExchangeSource::Snapshot as i32,
                    updated_at: Some(prost_wkt_types::Timestamp {
                        seconds: exchange_rate.updated_at.timestamp(),
                        nanos: 0,
                    }),
                }))
            }
        }
    }

    type GetAllBotPricingsStream =
        Pin<Box<dyn Stream<Item = Result<pricing_service::ItemPricing, Status>> + Send>>;

    async fn get_all_bot_pricings(
        &self,
        request: Request<pricing_service::GetAllBotPricingsRequest>,
    ) -> Result<Response<Self::GetAllBotPricingsStream>, Status> {
        use futures_util::stream::StreamExt;
        let limit = request
            .into_inner()
            .stale_limit
            .map(|x| x.seconds)
            .unwrap_or(DEFAULT_STALE_LIMIT_SECS);
        let from_timestamp = chrono::Utc::now() - chrono::Duration::seconds(limit);
        let mut connection = self.pool.get().await.map_err(pool_error_to_status)?;

        let query = mv_bot_prices::table
            .filter(mv_bot_prices::updated_at.gt(from_timestamp))
            .select(BotItemPricingRow::as_select())
            .limit(limit);

        let stream = query
            .load_stream::<BotItemPricingRow>(&mut connection)
            .await
            .map_err(diesel_error_to_status)?
            .map(|res| match res {
                Ok(row) => Ok(row.into()),
                Err(e) => Err(diesel_error_to_status(e)),
            });

        Ok(Response::new(
            Box::pin(stream) as Self::GetAllBotPricingsStream
        ))
    }
}
