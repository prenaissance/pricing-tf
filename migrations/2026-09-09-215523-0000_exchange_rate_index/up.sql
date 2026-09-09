CREATE INDEX idx_listings_exchange_rate ON trade_listings (item_name, price_metal ASC) WHERE intent = 'sell' AND is_automatic = true;
CREATE INDEX idx_listings_steam_id ON trade_listings (trade_user_details_steam_id);
