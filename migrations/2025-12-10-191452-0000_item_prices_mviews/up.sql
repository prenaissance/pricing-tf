create materialized view mv_prices as
with
formatted_listings as (
    select 
        item_name,
        intent,
        bumped_at,
        price_keys,
        jsonb_build_object(
            'price', jsonb_build_object(
                'keys', original_price_keys, 
                'metal', original_price_metal
            ),
            'trade_details', jsonb_build_object(
                'trade_offer_url', trade_details_trade_offer_url,
                'description', trade_details_description
            ),
            'bumped_at', bumped_at,
            'price_keys', price_keys,
            'price_metal', price_metal
        ) as listing_json
    from trade_listings
)
select 
    item_name,
    jsonb_agg(listing_json order by price_keys DESC, bumped_at DESC) 
        filter (where intent = 'buy') as buy_listings,
    jsonb_agg(listing_json order by price_keys ASC, bumped_at DESC) 
        filter (where intent = 'sell') as sell_listings,
    MAX(bumped_at) as updated_at
from formatted_listings
group by item_name;

-- Unique index for concurrent refreshes
create unique index uq_mv_prices_item_name on mv_prices (item_name);

create materialized view mv_bot_prices as
with
formatted_listings as (
    select 
        item_name,
        intent,
        is_automatic,
        bumped_at,
        price_keys,
        jsonb_build_object(
            'price', jsonb_build_object(
                'keys', original_price_keys, 
                'metal', original_price_metal
            ),
            'trade_details', jsonb_build_object(
                'trade_offer_url', trade_details_trade_offer_url,
                'description', trade_details_description
            ),
            'bumped_at', bumped_at,
            'price_keys', price_keys,
            'price_metal', price_metal
        ) as listing_json
    from trade_listings
    where is_automatic = true
)
select 
    item_name,
    jsonb_agg(listing_json order by price_keys DESC, bumped_at DESC) 
        filter (where intent = 'buy') as buy_listings,
    jsonb_agg(listing_json order by price_keys ASC, bumped_at DESC) 
        filter (where intent = 'sell') as sell_listings,
    MAX(bumped_at) as updated_at
from formatted_listings
group by item_name;

-- Unique index for concurrent refreshes
create unique index uq_mv_bot_prices_item_name on mv_bot_prices (item_name);