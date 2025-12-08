use std::sync::LazyLock;

use chrono::{DateTime, Utc};
use regex::Regex;

use crate::models::trade_listing::NewTradeListing;
use crate::processing::pricing_event::PricingEvent;

pub fn is_spelled_item(event: &PricingEvent) -> bool {
    static SPELLED_REGEX: LazyLock<Regex> = LazyLock::new(|| {
        Regex::new(r"(?i)spell|pumpkin|exo|𝐄𝐗𝐎?|𝐏𝐔𝐌𝐏𝐊𝐈𝐍|𝐇𝐅|𝐄𝐱𝐨𝐫𝐜𝐢𝐬𝐦|𝐏𝐁|ꜱᴘᴇʟʟ|𝗦𝗣𝗘𝗟𝗟|𝐒𝐩𝐞𝐥𝐥|𝐒𝐏𝐄𝐋𝐋")
            .unwrap()
    });
    SPELLED_REGEX.is_match(&event.payload.details)
}

pub fn is_unusual_weapon(event: &PricingEvent) -> bool {
    event
        .payload
        .item
        .defindex
        .is_some_and(|defindex| defindex == 134)
}

impl PricingEvent {
    fn to_trade_listing(self, exchange_rate: f64) -> NewTradeListing {
        let original_price = self.payload.currencies;
        NewTradeListing {
            id: self.payload.id,
            item_name: self.payload.item.name,
            market_name: self.payload.item.market_name,
            original_price_keys: original_price.keys,
            original_price_metal: original_price.metal,
            price_keys: original_price.to_keys(exchange_rate),
            price_metal: original_price.to_metal(exchange_rate),
            intent: self.payload.intent,
            bumped_at: DateTime::<Utc>::from_timestamp_secs(self.payload.bumped_at)
                .expect("Invalid $.payload.bumped_at unhandled: {}"),
            is_automatic: self.payload.user_agent.is_some(),
            trade_details_trade_offer_url: self.payload.user.trade_offer_url,
            trade_details_description: self.payload.details,
            trade_item_details_image_url: self.payload.item.image_url,
            item_quality_name: self.payload.item.quality.name,
            item_quality_color: self.payload.item.quality.color,
            trade_user_details_name: self.payload.user.name,
            trade_user_details_avatar_thumbnail_url: self.payload.user.avatar,
            trade_user_details_online: self.payload.user.online,
            trade_user_details_steam_id: self.payload.steamid,
        }
    }
}
