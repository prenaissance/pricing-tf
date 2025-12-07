use crate::protos::pricing_tf::block_user_service;
use tonic::{Request, Response, Status};

#[derive(Debug)]
pub struct BlockUserService {}

#[tonic::async_trait]
impl block_user_service::block_user_service_server::BlockUserService for BlockUserService {
    async fn block_user(
        &self,
        request: Request<block_user_service::BlockUserRequest>,
    ) -> Result<Response<block_user_service::BlockUserResponse>, Status> {
        todo!()
    }

    async fn unblock_user(
        &self,
        request: Request<block_user_service::UnblockUserRequest>,
    ) -> Result<Response<block_user_service::UnblockUserResponse>, Status> {
        todo!()
    }
}
