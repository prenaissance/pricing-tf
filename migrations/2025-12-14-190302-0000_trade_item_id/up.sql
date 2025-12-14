alter table trade_listings
    add column trade_item_details_id text default '' not null;

drop index uq_mv_prices_item_name;
drop index uq_mv_bot_prices_item_name;
drop materialized view mv_prices;
drop materialized view mv_bot_prices;

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
                'listing_id', id,
                'trade_offer_url', trade_details_trade_offer_url,
                'description', trade_details_description,
                'item', jsonb_build_object(
                    'id', trade_item_details_id,
                    'name', item_name,
                    'image_url', trade_item_details_image_url,
                    'quality', jsonb_build_object(
                        'id', item_quality_id,
                        'name', item_quality_name,
                        'color', item_quality_color
                    )
                ),
                'user', jsonb_build_object(
                    'name', trade_user_details_name,
                    'avatar_thumbnail_url', trade_user_details_avatar_thumbnail_url,
                    'online', trade_user_details_online,
                    'steam_id', trade_user_details_steam_id
                )
            ),
            'bumped_at', bumped_at,
            'price_keys', price_keys,
            'price_metal', price_metal
        ) as listing_json
    from trade_listings
)
select 
    item_name,
    coalesce(jsonb_agg(listing_json order by price_keys DESC, bumped_at DESC) 
        filter (where intent = 'buy'), '[]'::jsonb) as buy_listings,
    coalesce(jsonb_agg(listing_json order by price_keys ASC, bumped_at DESC) 
        filter (where intent = 'sell'), '[]'::jsonb) as sell_listings,
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
                'listing_id', id,
                'trade_offer_url', trade_details_trade_offer_url,
                'description', trade_details_description,
                'item', jsonb_build_object(
                    'id', trade_item_details_id,
                    'name', item_name,
                    'image_url', trade_item_details_image_url,
                    'quality', jsonb_build_object(
                        'id', item_quality_id,
                        'name', item_quality_name,
                        'color', item_quality_color
                    )
                ),
                'user', jsonb_build_object(
                    'name', trade_user_details_name,
                    'avatar_thumbnail_url', trade_user_details_avatar_thumbnail_url,
                    'online', trade_user_details_online,
                    'steam_id', trade_user_details_steam_id
                )
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
    coalesce(jsonb_agg(listing_json order by price_keys DESC, bumped_at DESC) 
        filter (where intent = 'buy'), '[]'::jsonb) as buy_listings,
    coalesce(jsonb_agg(listing_json order by price_keys ASC, bumped_at DESC) 
        filter (where intent = 'sell'), '[]'::jsonb) as sell_listings,
    MAX(bumped_at) as updated_at
from formatted_listings
group by item_name;

-- Unique index for concurrent refreshes
create unique index uq_mv_bot_prices_item_name on mv_bot_prices (item_name);