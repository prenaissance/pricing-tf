use std::pin::Pin;

use crate::protos::pricing_tf::pricing_service;
use futures_util::Stream;
use tonic::{Request, Response, Status};

#[derive(Debug)]
pub struct PricingService {}

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
        todo!()
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
