use std::collections::HashSet;
use std::sync::Arc;

use super::errors::{diesel_error_to_status, pool_error_to_status};
use crate::db::AsyncDbPool;
use crate::models::blocked_user::BlockedUser;
use crate::protos::pricing_tf::blocked_users::v1 as blocked_users_v1;
use crate::schema::{blocked_users, trade_listings};
use diesel::dsl::exists;
use diesel::prelude::*;
use diesel_async::scoped_futures::ScopedFutureExt as _;
use diesel_async::{AsyncConnection, RunQueryDsl};
use tonic::{Request, Response, Status};

pub struct BlockUserService {
    pool: AsyncDbPool,
    blocked_user_steam_ids: Arc<tokio::sync::Mutex<HashSet<String>>>,
}

impl BlockUserService {
    pub fn new(
        pool: AsyncDbPool,
        blocked_user_steam_ids: Arc<tokio::sync::Mutex<HashSet<String>>>,
    ) -> Self {
        Self {
            pool,
            blocked_user_steam_ids,
        }
    }
}

#[tonic::async_trait]
impl blocked_users_v1::block_user_service_server::BlockUserService for BlockUserService {
    async fn block_user(
        &self,
        request: Request<blocked_users_v1::BlockUserRequest>,
    ) -> Result<Response<blocked_users_v1::BlockUserResponse>, Status> {
        let steam_id = request.into_inner().steam_id;
        let mut connection = self.pool.get().await.map_err(pool_error_to_status)?;

        let is_already_blocked: bool = diesel::select(exists(
            blocked_users::table.filter(blocked_users::steamid.eq(steam_id.clone())),
        ))
        .get_result(&mut connection)
        .await
        .map_err(diesel_error_to_status)?;

        if is_already_blocked {
            return Err(Status::already_exists("This user was already blocked"));
        }

        let mut blocked_user_steam_ids = self.blocked_user_steam_ids.lock().await;

        let deleted_count = connection
            .transaction(|conn| {
                async {
                    let blocked_user = BlockedUser {
                        steamid: steam_id.clone(),
                        blocked_at: chrono::Utc::now(),
                    };
                    diesel::insert_into(blocked_users::table)
                        .values(&blocked_user)
                        .execute(conn)
                        .await?;

                    let deleted_count =
                        diesel::delete(trade_listings::table.filter(
                            trade_listings::trade_user_details_steam_id.eq(steam_id.clone()),
                        ))
                        .execute(conn)
                        .await?;

                    Ok(deleted_count)
                }
                .scope_boxed()
            })
            .await
            .map_err(diesel_error_to_status)?;

        blocked_user_steam_ids.insert(steam_id);
        tracing::info!(
            "Blocked user and deleted {} of their listings",
            deleted_count
        );
        Ok(Response::new(blocked_users_v1::BlockUserResponse {
            success: true,
            deleted_listing_count: deleted_count as u32,
        }))
    }

    async fn unblock_user(
        &self,
        request: Request<blocked_users_v1::UnblockUserRequest>,
    ) -> Result<Response<blocked_users_v1::UnblockUserResponse>, Status> {
        let steam_id = request.into_inner().steam_id;
        let mut connection = self.pool.get().await.map_err(pool_error_to_status)?;

        let delete_count = diesel::delete(
            blocked_users::table.filter(blocked_users::steamid.eq(steam_id.clone())),
        )
        .execute(&mut connection)
        .await
        .map_err(diesel_error_to_status)?;

        let mut blocked_user_steam_ids = self.blocked_user_steam_ids.lock().await;
        blocked_user_steam_ids.remove(&steam_id);

        Ok(Response::new(blocked_users_v1::UnblockUserResponse {
            success: delete_count > 0,
        }))
    }
}
