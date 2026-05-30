-- ========== v3.0 字段修改 ==========
-- users 表已有 phone 字段，新增 steam_id 字段

ALTER TABLE users ADD COLUMN IF NOT EXISTS steam_id VARCHAR(20) DEFAULT NULL AFTER phone;
ALTER TABLE users ADD UNIQUE INDEX IF NOT EXISTS idx_steam_id (steam_id);
ALTER TABLE users ADD INDEX IF NOT EXISTS idx_phone (phone);
