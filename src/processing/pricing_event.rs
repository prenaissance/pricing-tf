use serde::Deserialize;

#[derive(Debug, Deserialize)]
pub struct PricingEvent {
    pub id: String,
    pub event: ListingType,
    pub payload: PricingEventPayload,
}

#[derive(Debug, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "kebab-case")]
pub enum ListingType {
    ListingUpdate,
    ListingDelete,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct PricingEventPayload {
    /// Backpack.tf listing ID
    pub id: String,
    pub steamid: String,
    pub appid: u32,
    pub currencies: Tf2Currency,
    pub value: Tf2NaturalCurrency,
    #[serde(default)]
    pub trade_offers_preferred: bool,
    #[serde(default)]
    pub buyout_only: bool,
    #[serde(default)]
    pub details: String,
    pub listed_at: u64,
    pub bumped_at: u64,
    pub intent: ListingIntent,
    pub count: u32,
    pub status: String,
    pub source: String,
    pub item: ItemListing,
    pub user_agent: Option<UserAgent>,
    pub user: User,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ItemListing {
    pub appid: u32,
    pub base_name: String,
    pub defindex: Option<u32>,
    pub id: String,
    pub image_url: String,
    pub market_name: String,
    pub name: String,
    pub origin: Option<Origin>,
    pub original_id: Option<String>,
    pub quality: ItemQuality,
    pub summary: String,
    /// 1-100
    pub level: Option<u8>,
    pub killstreak_tier: Option<u8>,
    #[serde(default)]
    pub class: Vec<Tf2CharacterClass>,
    pub slot: Option<String>,
    #[serde(default)]
    pub tradable: bool,
    #[serde(default)]
    pub craftable: bool,
    pub sheen: Option<Sheen>,
    pub killstreaker: Option<Killstreaker>,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct User {
    pub id: String,
    pub name: String,
    pub avatar: String,
    pub avatar_full: String,
    pub premium: bool,
    pub online: bool,
    pub banned: bool,
    pub custom_name_style: String,
    pub class: String,
    pub style: String,
    pub trade_offer_url: String,
    // pub bans: todo get possible structure
}

#[derive(Debug, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "lowercase")]
pub enum ListingIntent {
    Buy,
    Sell,
}

#[derive(Debug, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UserAgent {
    pub client: String,
    pub last_pulse: u64,
}

#[derive(Debug, Deserialize)]
pub struct Tf2Currency {
    #[serde(default)]
    pub keys: f64,
    #[serde(default)]
    pub metal: f64,
}

#[derive(Debug, Deserialize)]
pub struct Tf2NaturalCurrency {
    pub raw: f64,
    pub short: String,
    pub long: String,
}

#[derive(Debug, Deserialize)]
pub struct Origin {
    pub id: u32,
    pub name: String,
}

#[derive(Debug, Deserialize)]
pub struct ItemQuality {
    pub id: u32,
    pub name: String,
    pub color: String,
}

#[derive(Debug, Deserialize)]
pub struct Sheen {
    pub id: u32,
    pub name: String,
}

#[derive(Debug, Deserialize)]
pub struct Killstreaker {
    pub id: u32,
    pub name: String,
}

#[derive(Debug, Deserialize, PartialEq, Eq)]
#[serde(rename_all = "PascalCase")]
pub enum Tf2CharacterClass {
    Scout,
    Soldier,
    Pyro,
    Demoman,
    Heavy,
    Engineer,
    Medic,
    Sniper,
    Spy,
}

#[cfg(test)]
mod tests {

    use super::*;

    #[test]
    fn deserialized_listing_update_event() {
        let json_string = r##"
        {
            "id": "69346e7883418419600ebe14",
            "event": "listing-update",
            "payload": {
                "id": "440_76561199072611396_26b1c3911028c585c5bb08dd3a9b16b7",
                "steamid": "76561199072611396",
                "appid": 440,
                "currencies": {
                    "keys": 1975
                },
                "value": {
                    "raw": 116189.25,
                    "short": "1975 keys",
                    "long": "1975 keys"
                },
                "tradeOffersPreferred": true,
                "buyoutOnly": true,
                "details": "1975 keys for YOUR item. Stock: 0/1.⚡️24/7 INSTANT. 5k+ keys stock (auto-refill). Offer⚡or chat💬me: sell_Time_Warp_Polar_Pullover",
                "listedAt": 1765043831,
                "bumpedAt": 1765043606,
                "intent": "buy",
                "count": 1,
                "status": "active",
                "source": "userAgent",
                "item": {
                    "appid": 440,
                    "baseName": "Polar Pullover",
                    "defindex": 30329,
                    "id": "",
                    "imageUrl": "https://steamcdn-a.akamaihd.net/apps/440/icons/xms2013_polar_pullover.f78eca28eeb5c9899be79313cbaa48b5d9831c51.png",
                    "marketName": "Unusual Polar Pullover",
                    "name": "Time Warp Polar Pullover",
                    "origin": null,
                    "originalId": "",
                    "quality": {
                        "id": 5,
                        "name": "Unusual",
                        "color": "#8650AC"
                    },
                    "summary": "Level 1-100 Hat",
                    "price": {
                        "steam": {
                            "currency": "usd",
                            "short": "$677.97",
                            "long": "24,653.45 ref, 419.06 keys",
                            "raw": 18992.845285714284,
                            "value": 67797
                        },
                        "community": {
                            "value": 3950,
                            "valueHigh": 3950,
                            "currency": "keys",
                            "raw": 232378.5,
                            "short": "3950 keys",
                            "long": "232,378.50 ref, $6390.41",
                            "usd": 6390.40875,
                            "updatedAt": 1630887782,
                            "difference": 196870.85
                        },
                        "suggested": {
                            "raw": 232378.5,
                            "short": "3950 keys",
                            "long": "232,378.50 ref, $6390.41",
                            "usd": 6390.40875
                        }
                    },
                    "slot": "misc",
                    "particle": {
                        "id": 70,
                        "name": "Time Warp",
                        "shortName": "tw",
                        "imageUrl": "/images/440/particles/70_94x94.png",
                        "type": "standard"
                    },
                    "tradable": true,
                    "craftable": true,
                    "priceindex": "70"
                },
                "userAgent": {
                    "client": "ScrapyardBot - fastest trades at best prices, and this is not just marketing fluff like some do ;-)",
                    "lastPulse": 1765043549
                },
                "user": {
                    "id": "76561199072611396",
                    "name": "6ScrapyardBot⚡️24/7",
                    "avatar": "https://avatars.steamstatic.com/51524702a4181c3102717b5acb13756d7b2f5ea3_medium.jpg",
                    "avatarFull": "https://avatars.steamstatic.com/51524702a4181c3102717b5acb13756d7b2f5ea3_full.jpg",
                    "premium": true,
                    "online": true,
                    "banned": false,
                    "customNameStyle": "awesome3",
                    "class": "awesome3",
                    "style": "",
                    "role": null,
                    "tradeOfferUrl": "https://steamcommunity.com/tradeoffer/new/?partner=1112345668&token=5L2-Mo4F",
                    "bans": []
                }
            }
        }"##;

        let event: PricingEvent = serde_json::from_str(json_string).unwrap();
        assert_eq!(event.id, "69346e7883418419600ebe14");
        assert_eq!(event.event, ListingType::ListingUpdate);
        assert_eq!(
            event.payload.id,
            "440_76561199072611396_26b1c3911028c585c5bb08dd3a9b16b7"
        );
        assert_eq!(event.payload.steamid, "76561199072611396");
        assert_eq!(event.payload.appid, 440);
        assert_eq!(event.payload.currencies.keys, 1975.0);
        assert_eq!(event.payload.value.raw, 116189.25);
        assert_eq!(event.payload.trade_offers_preferred, true);
        assert_eq!(event.payload.item.class, vec![]);
    }

    #[test]
    fn deserialized_listing_delete_event() {
        let json_string = r##"
        {
            "id": "69346e788db038246902b9d5",
            "event": "listing-delete",
            "payload": {
                "id": "440_76561199103740298_605f67acc80a9b35345514725f0e2a5f",
                "steamid": "76561199103740298",
                "appid": 440,
                "currencies": {
                    "metal": 21.77,
                    "keys": 21
                },
                "value": {
                    "raw": 1257.2,
                    "short": "21.37 keys",
                    "long": "21 keys, 21.77 ref"
                },
                "tradeOffersPreferred": true,
                "buyoutOnly": true,
                "details": "24/7 BOT Buying for 21 keys 21.77 ref! Add me and type !sell Chicken Kiev or send me a trade offer! My owner can buy more items!",
                "listedAt": 1765043410,
                "bumpedAt": 1765043559,
                "intent": "buy",
                "count": 1,
                "status": "active",
                "source": "userAgent",
                "item": {
                    "appid": 440,
                    "baseName": "Chicken Kiev",
                    "defindex": 30238,
                    "id": "",
                    "imageUrl": "https://steamcdn-a.akamaihd.net/apps/440/icons/hw2013_heavy_robin.76f9924b153b853b37c3c68e8ba29ae690ce3f48.png",
                    "marketName": "The Chicken Kiev",
                    "name": "The Chicken Kiev",
                    "origin": null,
                    "originalId": "",
                    "quality": {
                        "id": 6,
                        "name": "Unique",
                        "color": "#FFD700"
                    },
                    "summary": "Level 1-100 Bird Head",
                    "price": {
                        "community": {
                            "value": 27.5,
                            "valueHigh": 28.6,
                            "currency": "keys",
                            "raw": 1650.1815,
                            "short": "27.5–28.6 keys",
                            "long": "1,650.18 ref, $45.38",
                            "usd": 45.824893125,
                            "updatedAt": 1759737980,
                            "difference": 2.8860000000001946
                        },
                        "suggested": {
                            "raw": 1650.1815,
                            "short": "28.05 keys",
                            "long": "1,650.18 ref, $45.38",
                            "usd": 45.379991249999996
                        }
                    },
                    "class": [
                        "Heavy"
                    ],
                    "slot": "misc",
                    "tradable": true,
                    "craftable": true
                },
                "userAgent": {
                    "client": "Gladiator.tf - Rent your own bot from 6 keys per month",
                    "lastPulse": 1765043799
                },
                "user": {
                    "id": "76561199103740298",
                    "name": "Philibert BOT || [⇄] 24/7",
                    "avatar": "https://avatars.steamstatic.com/519c33595765431a196da4be6f43844c67c17f41_medium.jpg",
                    "avatarFull": "https://avatars.steamstatic.com/519c33595765431a196da4be6f43844c67c17f41_full.jpg",
                    "premium": true,
                    "online": true,
                    "banned": false,
                    "customNameStyle": "awesome6",
                    "class": "awesome6",
                    "style": "",
                    "role": null,
                    "tradeOfferUrl": "https://steamcommunity.com/tradeoffer/new/?partner=1143474570&token=-7RfTiY6",
                    "bans": []
                }
            }
        }
        "##;

        let event: PricingEvent = serde_json::from_str(json_string).unwrap();
        assert_eq!(event.id, "69346e788db038246902b9d5");
        assert_eq!(event.event, ListingType::ListingDelete);
        assert_eq!(
            event.payload.id,
            "440_76561199103740298_605f67acc80a9b35345514725f0e2a5f"
        );
        assert_eq!(event.payload.steamid, "76561199103740298");
        assert_eq!(event.payload.appid, 440);
        assert_eq!(event.payload.currencies.keys, 21.0);
        assert_eq!(event.payload.currencies.metal, 21.77);
        assert_eq!(event.payload.value.raw, 1257.2);
        assert_eq!(event.payload.trade_offers_preferred, true);
        assert_eq!(event.payload.item.class, vec![Tf2CharacterClass::Heavy]);
    }
}
