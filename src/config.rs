pub struct AppConfig {
    pub port: u16,
    pub db_url: String,
    pub backpack_tf_cookie: String,
    pub listings_ttl_hours: u32,
}

impl AppConfig {
    pub fn from_env() -> Self {
        let port = std::env::var("PORT")
            .unwrap_or("8080".to_string())
            .parse()
            .expect("PORT must be a valid u16");
        let db_url = std::env::var("DATABASE_URL").expect("DATABASE_URL must be set");
        let backpack_tf_cookie =
            std::env::var("BACKPACK_TF_COOKIE").expect("BACKPACK_TF_COOKIE must be set");
        let listings_ttl_hours = std::env::var("LISTINGS_TTL_HOURS")
            .unwrap_or("24".to_string())
            .parse()
            .expect("LISTINGS_TTL_HOURS must be a valid u32");

        AppConfig {
            port,
            db_url,
            backpack_tf_cookie,
            listings_ttl_hours,
        }
    }
}
