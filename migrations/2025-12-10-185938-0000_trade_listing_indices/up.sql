-- Speeds up the grouping (By Item Name)
CREATE INDEX idx_listings_general_buy ON trade_listings (item_name, price_keys DESC, bumped_at DESC)
WHERE
    intent = 'buy';

CREATE INDEX idx_listings_general_sell ON trade_listings (item_name, price_keys ASC, bumped_at DESC)
WHERE
    intent = 'sell';

-- Speeds up the bot prices grouping (By Market Name)
CREATE INDEX idx_listings_bots_buy ON trade_listings (market_name, price_keys DESC, bumped_at DESC)
WHERE
    intent = 'buy'
    AND is_automatic = true;

CREATE INDEX idx_listings_bots_sell ON trade_listings (market_name, price_keys ASC, bumped_at DESC)
WHERE
    intent = 'sell'
    AND is_automatic = true;

-- Speeds up bot filtering
CREATE INDEX idx_listings_automatic ON trade_listings (is_automatic);

-- Speeds up deleting stale listings
CREATE INDEX idx_listings_bumped_at ON trade_listings (bumped_at DESC);