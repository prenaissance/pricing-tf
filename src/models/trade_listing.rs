use chrono::{DateTime, Utc};
use diesel::prelude::*;
use diesel_derive_enum::DbEnum;
use serde::{Deserialize, Serialize};

use crate::models::tf2_currency::Tf2Currency;

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct ItemQuality {
    pub name: String,
    pub color: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct TradeItemDetails {
    pub image_url: String,
    pub quality: ItemQuality,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct TradeUserDetails {
    pub name: String,
    pub avatar_thumbnail_url: String,
    pub online: bool,
    pub steam_id: String,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct TradeDetails {
    pub trade_offer_url: String,
    pub description: String,
    pub item: TradeItemDetails,
    pub user: TradeUserDetails,
}

#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize, DbEnum)]
#[serde(rename_all = "lowercase")]
#[ExistingTypePath = "crate::schema::sql_types::ListingIntentType"]
#[DbValueStyle = "snake_case"]
pub enum ListingIntent {
    Buy,
    Sell,
}

#[derive(Debug, Serialize, Deserialize, Clone)]
pub struct TradeListing {
    /// Unique identifier coming from Backpack.tf
    pub id: String,
    pub item_name: String,
    pub market_name: String,
    pub intent: ListingIntent,
    pub original_price: Tf2Currency,
    pub price_metal: f64,
    pub price_keys: f64,
    pub bumped_at: DateTime<Utc>,
    pub is_automatic: bool,
    pub trade_details: TradeDetails, // The nested struct
}

#[derive(Debug, Queryable, Selectable)]
#[diesel(table_name = crate::schema::trade_listings)]
#[diesel(check_for_backend(diesel::pg::Pg))]
pub struct TradeListingRow {
    pub id: String,
    pub item_name: String,
    pub market_name: String,
    pub intent: ListingIntent, // Uses the DbEnum type
    pub original_price_keys: f64,
    pub original_price_metal: f64,
    pub price_keys: f64,
    pub price_metal: f64,
    pub bumped_at: DateTime<Utc>,
    pub is_automatic: bool,

    pub trade_details_trade_offer_url: String,
    pub trade_details_description: String,

    pub trade_item_details_image_url: String,

    pub item_quality_name: String,
    pub item_quality_color: String,

    pub trade_user_details_name: String,
    pub trade_user_details_avatar_thumbnail_url: String,
    pub trade_user_details_online: bool,
    pub trade_user_details_steam_id: String,
}

impl From<TradeListingRow> for TradeListing {
    fn from(row: TradeListingRow) -> Self {
        // Reconstruct nested structs
        let item_quality = ItemQuality {
            name: row.item_quality_name,
            color: row.item_quality_color,
        };

        let trade_item_details = TradeItemDetails {
            image_url: row.trade_item_details_image_url,
            quality: item_quality,
        };

        let trade_user_details = TradeUserDetails {
            name: row.trade_user_details_name,
            avatar_thumbnail_url: row.trade_user_details_avatar_thumbnail_url,
            online: row.trade_user_details_online,
            steam_id: row.trade_user_details_steam_id,
        };

        let trade_details = TradeDetails {
            trade_offer_url: row.trade_details_trade_offer_url,
            description: row.trade_details_description,
            item: trade_item_details,
            user: trade_user_details,
        };

        TradeListing {
            id: row.id,
            item_name: row.item_name,
            market_name: row.market_name,
            intent: row.intent,
            original_price: Tf2Currency::new(row.original_price_keys, row.original_price_metal),
            price_metal: row.price_metal,
            price_keys: row.price_keys,
            bumped_at: row.bumped_at,
            is_automatic: row.is_automatic,
            trade_details,
        }
    }
}

#[derive(Insertable)]
#[diesel(table_name = crate::schema::trade_listings)]
pub struct NewTradeListing {
    pub id: String,
    pub item_name: String,
    pub market_name: String,
    pub intent: ListingIntent,
    pub original_price_keys: f64,
    pub original_price_metal: f64,
    pub price_metal: f64,
    pub price_keys: f64,
    pub bumped_at: DateTime<Utc>,
    pub is_automatic: bool,

    pub trade_details_trade_offer_url: String,
    pub trade_details_description: String,

    pub trade_item_details_image_url: String,

    pub item_quality_name: String,
    pub item_quality_color: String,

    pub trade_user_details_name: String,
    pub trade_user_details_avatar_thumbnail_url: String,
    pub trade_user_details_online: bool,
    pub trade_user_details_steam_id: String,
}

impl From<TradeListing> for NewTradeListing {
    fn from(model: TradeListing) -> Self {
        NewTradeListing {
            id: model.id,
            item_name: model.item_name,
            market_name: model.market_name,

            intent: model.intent,
            original_price_keys: model.original_price.keys,
            original_price_metal: model.original_price.metal,
            price_keys: model.price_keys,
            price_metal: model.price_metal,
            bumped_at: model.bumped_at,
            is_automatic: model.is_automatic,

            trade_details_trade_offer_url: model.trade_details.trade_offer_url,
            trade_details_description: model.trade_details.description,
            trade_item_details_image_url: model.trade_details.item.image_url,
            item_quality_name: model.trade_details.item.quality.name,
            item_quality_color: model.trade_details.item.quality.color,

            trade_user_details_name: model.trade_details.user.name,
            trade_user_details_avatar_thumbnail_url: model.trade_details.user.avatar_thumbnail_url,
            trade_user_details_online: model.trade_details.user.online,
            trade_user_details_steam_id: model.trade_details.user.steam_id,
        }
    }
}
