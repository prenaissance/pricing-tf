// @generated automatically by Diesel CLI.

pub mod sql_types {
    #[derive(diesel::query_builder::QueryId, diesel::sql_types::SqlType)]
    #[diesel(postgres_type(name = "listing_intent_type"))]
    pub struct ListingIntentType;
}

diesel::table! {
    use diesel::sql_types::*;
    use super::sql_types::ListingIntentType;

    trade_listings (id) {
        id -> Text,
        item_name -> Text,
        market_name -> Text,
        intent -> ListingIntentType,
        original_price_keys -> Float8,
        original_price_metal -> Float8,
        price_keys -> Float8,
        price_metal -> Float8,
        bumped_at -> Timestamptz,
        is_automatic -> Bool,
        trade_details_trade_offer_url -> Text,
        trade_details_description -> Text,
        trade_item_details_image_url -> Text,
        item_quality_id -> Int4,
        item_quality_name -> Text,
        item_quality_color -> Text,
        trade_user_details_name -> Text,
        trade_user_details_avatar_thumbnail_url -> Text,
        trade_user_details_online -> Bool,
        trade_user_details_steam_id -> Text,
    }
}
