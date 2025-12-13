use std::{collections::HashSet, sync::Arc};

use diesel_async::RunQueryDsl as _;

use crate::{models::blocked_user::BlockedUser, schema::blocked_users};

use super::AsyncDbPool;

pub async fn load_all_blocked_steam_ids(
    pool: &AsyncDbPool,
) -> Result<Arc<tokio::sync::Mutex<HashSet<String>>>, Box<dyn std::error::Error>> {
    let mut connection = pool.get().await?;
    let blocked_users: Vec<BlockedUser> = blocked_users::table.load(&mut connection).await?;

    let blocked_steam_ids = blocked_users.into_iter().map(|user| user.steamid).collect();
    Ok(Arc::new(tokio::sync::Mutex::new(blocked_steam_ids)))
}
