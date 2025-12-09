use std::pin::Pin;

use crate::backpack_tf_api::exchange_rate_controller::CachedExchangeRate;
use crate::backpack_tf_api::facade::MANNCO_SUPPLY_CRATE_KEY;
use crate::db::AsyncDbPool;
use crate::models::trade_listing::TradeListingRow;
use crate::models::trade_listing::{ListingIntent, TradeListing};
use crate::protos::pricing_tf::pricing_service;
use crate::schema::trade_listings;

use diesel::prelude::*;
use diesel_async::RunQueryDsl;
use futures_util::Stream;
use tonic::{Request, Response, Status};

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
        todo!()
    }

    async fn get_bot_pricing(
        &self,
        request: Request<pricing_service::ItemRequest>,
    ) -> Result<Response<pricing_service::ItemPricing>, Status> {
        todo!()
    }

    async fn delete_bot_pricing(
        &self,
        request: Request<pricing_service::DeleteBotPricingRequest>,
    ) -> Result<Response<pricing_service::DeleteBotPricingResponse>, Status> {
        todo!()
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
                updated_at: Some(prost_types::Timestamp {
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
                    updated_at: Some(prost_types::Timestamp {
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
        todo!()
    }
}
