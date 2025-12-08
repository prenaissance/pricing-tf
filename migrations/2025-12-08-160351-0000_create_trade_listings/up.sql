CREATE TYPE listing_intent_type AS ENUM ('buy', 'sell');

CREATE TABLE trade_listings (
    -- Id coming from Backpack.tf
    id text primary key not null,

    -- TradeListing fields
    item_name text not null,
    market_name text not null,
    intent listing_intent_type not null,
    
    -- Tf2Currency fields
    original_price_keys double precision not null,
    original_price_metal double precision not null,

    price_keys double precision not null,
    price_metal double precision not null,
    
    bumped_at timestamptz not null,
    is_automatic boolean not null,
    
    -- TradeDetails fields
    trade_details_trade_offer_url text not null,
    trade_details_description text not null,
    
    -- TradeItemDetails fields
    trade_item_details_image_url text not null,
    
    -- ItemQuality fields
    item_quality_name text not null,
    item_quality_color text not null,
    
    -- TradeUserDetails fields
    trade_user_details_name text not null,
    trade_user_details_avatar_thumbnail_url text not null,
    trade_user_details_online boolean not null,
    trade_user_details_steam_id text not null
);
