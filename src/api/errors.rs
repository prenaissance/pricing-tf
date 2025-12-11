use diesel_async::pooled_connection::deadpool::PoolError;
use tonic::Status;

pub fn pool_error_to_status(error: PoolError) -> Status {
    Status::internal(format!("Database pool error: {}", error))
}

pub fn diesel_error_to_status(error: diesel::result::Error) -> Status {
    tracing::error!("Database query error: {}", error);
    Status::internal(format!("Database query error"))
}
