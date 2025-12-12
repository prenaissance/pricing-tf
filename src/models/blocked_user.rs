use diesel::{Insertable, Queryable};

#[derive(Debug, Queryable, Insertable)]
#[diesel(table_name = crate::schema::blocked_users)]
#[diesel(check_for_backend(diesel::pg::Pg))]
pub struct BlockedUser {
    pub steamid: String,
    pub blocked_at: chrono::DateTime<chrono::Utc>,
}
