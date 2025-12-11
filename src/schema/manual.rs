// materialized views

diesel::table! {
    use diesel::sql_types::*;

    mv_prices (item_name) {
        item_name -> Text,
        buy_listings -> Jsonb,
        sell_listings -> Jsonb,
        updated_at -> Timestamptz,
    }
}

diesel::table! {
    use diesel::sql_types::*;

    mv_bot_prices (item_name) {
        item_name -> Text,
        buy_listings -> Jsonb,
        sell_listings -> Jsonb,
        updated_at -> Timestamptz,
    }
}
