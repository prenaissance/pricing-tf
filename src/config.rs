use serde::Deserialize;

#[derive(Debug, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum LogLevel {
    Error,
    Warn,
    Info,
    Debug,
    Trace,
}

impl From<&LogLevel> for tracing::Level {
    fn from(level: &LogLevel) -> Self {
        match level {
            LogLevel::Error => tracing::Level::ERROR,
            LogLevel::Warn => tracing::Level::WARN,
            LogLevel::Info => tracing::Level::INFO,
            LogLevel::Debug => tracing::Level::DEBUG,
            LogLevel::Trace => tracing::Level::TRACE,
        }
    }
}

pub struct AppConfig {
    pub db_url: String,
    pub backpack_tf_cookie: String,
    pub listings_ttl_hours: u32,
    pub log_level: LogLevel,
}

impl AppConfig {
    pub fn from_env() -> Self {
        let db_url = std::env::var("DATABASE_URL").expect("DATABASE_URL must be set");
        let backpack_tf_cookie =
            std::env::var("BACKPACK_TF_COOKIE").expect("BACKPACK_TF_COOKIE must be set");
        let listings_ttl_hours = std::env::var("LISTINGS_TTL_HOURS")
            .unwrap_or("24".to_string())
            .parse()
            .expect("LISTINGS_TTL_HOURS must be a valid u32");
        let log_level = std::env::var("LOG_LEVEL")
            .unwrap_or("info".to_string())
            .to_lowercase();
        let log_level: LogLevel = serde_json::from_str(&log_level).unwrap_or(LogLevel::Info);

        AppConfig {
            db_url,
            backpack_tf_cookie,
            listings_ttl_hours,
            log_level,
        }
    }
}
