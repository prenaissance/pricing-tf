create table blocked_users (
    steamid text primary key,
    blocked_at timestamptz not null default now()
);