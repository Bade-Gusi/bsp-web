-- ========== 雷神对战平台 - 数据库完整建表脚本 ==========

CREATE TABLE IF NOT EXISTS users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(32) NOT NULL UNIQUE,
    password_hash VARCHAR(256) NOT NULL,
    nickname VARCHAR(32) NOT NULL DEFAULT '',
    avatar_url VARCHAR(512) DEFAULT '',
    phone VARCHAR(20) DEFAULT '',
    email VARCHAR(128) DEFAULT '',
    status TINYINT DEFAULT 0,
    mmr INT DEFAULT 1000,
    rank_id INT DEFAULT 1,
    win_count INT DEFAULT 0,
    lose_count INT DEFAULT 0,
    kill_count INT DEFAULT 0,
    headshot_count INT DEFAULT 0,
    total_games INT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    last_login_at DATETIME,
    INDEX idx_mmr (mmr),
    INDEX idx_status (status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS ranks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(32) NOT NULL,
    min_mmr INT NOT NULL,
    max_mmr INT NOT NULL,
    icon_url VARCHAR(256) DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS games (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(64) NOT NULL,
    short_name VARCHAR(16) NOT NULL,
    process_name VARCHAR(64) NOT NULL,
    launcher_args VARCHAR(256) DEFAULT '',
    cover_url VARCHAR(256) DEFAULT '',
    status TINYINT DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS rooms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_code VARCHAR(8) NOT NULL UNIQUE,
    game_id INT NOT NULL,
    host_user_id BIGINT NOT NULL,
    mode TINYINT DEFAULT 0,
    map_name VARCHAR(64) DEFAULT '',
    max_players INT DEFAULT 10,
    current_players INT DEFAULT 1,
    password VARCHAR(32) DEFAULT '',
    status TINYINT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (game_id) REFERENCES games(id),
    FOREIGN KEY (host_user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS room_players (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    team TINYINT DEFAULT 0,
    slot INT DEFAULT 0,
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (room_id) REFERENCES rooms(id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    UNIQUE KEY uk_room_user (room_id, user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS matches (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_id BIGINT,
    game_id INT NOT NULL,
    map_name VARCHAR(64) DEFAULT '',
    mode TINYINT DEFAULT 0,
    status TINYINT DEFAULT 0,
    winner_team TINYINT DEFAULT -1,
    duration_seconds INT DEFAULT 0,
    replay_url VARCHAR(512) DEFAULT '',
    demo_url VARCHAR(512) DEFAULT '',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    ended_at DATETIME,
    FOREIGN KEY (game_id) REFERENCES games(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS match_players (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    match_id BIGINT NOT NULL,
    user_id BIGINT NOT NULL,
    team TINYINT DEFAULT 0,
    kills INT DEFAULT 0,
    deaths INT DEFAULT 0,
    assists INT DEFAULT 0,
    headshots INT DEFAULT 0,
    damage INT DEFAULT 0,
    mvps INT DEFAULT 0,
    score INT DEFAULT 0,
    mmr_change INT DEFAULT 0,
    is_winner BOOLEAN DEFAULT FALSE,
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (match_id) REFERENCES matches(id),
    FOREIGN KEY (user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS friends (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    friend_id BIGINT NOT NULL,
    status TINYINT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (friend_id) REFERENCES users(id),
    UNIQUE KEY uk_user_friend (user_id, friend_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS chat_messages (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    from_user_id BIGINT NOT NULL,
    to_user_id BIGINT,
    room_id BIGINT,
    content TEXT NOT NULL,
    msg_type TINYINT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_room (room_id, created_at),
    INDEX idx_private (from_user_id, to_user_id, created_at),
    FOREIGN KEY (from_user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS anticheat_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id BIGINT NOT NULL,
    match_id BIGINT,
    alert_type VARCHAR(64) NOT NULL,
    severity TINYINT DEFAULT 0,
    details JSON,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user (user_id),
    INDEX idx_match (match_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- ========== 种子数据 ==========
INSERT IGNORE INTO ranks (id, name, min_mmr, max_mmr, icon_url) VALUES
(1, '青铜', 0, 499, '/ranks/bronze.png'),
(2, '白银', 500, 999, '/ranks/silver.png'),
(3, '黄金', 1000, 1499, '/ranks/gold.png'),
(4, '铂金', 1500, 1999, '/ranks/platinum.png'),
(5, '钻石', 2000, 2499, '/ranks/diamond.png'),
(6, '大师', 2500, 2999, '/ranks/master.png'),
(7, '宗师', 3000, 9999, '/ranks/grandmaster.png');

INSERT IGNORE INTO games (id, name, short_name, process_name, launcher_args) VALUES
(1, 'Counter-Strike 2', 'cs2', 'cs2.exe', '-appid 730'),
(2, 'PUBG: BATTLEGROUNDS', 'pubg', 'TslGame.exe', ''),
(3, 'VALORANT', 'valorant', 'VALORANT-Win64-Shipping.exe', ''),
(4, 'Apex Legends', 'apex', 'r5apex.exe', '');
