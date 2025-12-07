use std::sync::LazyLock;

use regex::Regex;

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
