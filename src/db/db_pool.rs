use diesel_async::AsyncPgConnection;
use diesel_async::pooled_connection::deadpool::Pool;

pub type AsyncDbPool = Pool<AsyncPgConnection>;
