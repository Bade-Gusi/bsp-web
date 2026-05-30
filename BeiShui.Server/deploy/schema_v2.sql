-- ========== v2.0 新增表 ==========

CREATE TABLE IF NOT EXISTS game_servers (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_code VARCHAR(8) NOT NULL UNIQUE,
    host_user_id BIGINT NOT NULL,
    game_id INT NOT NULL DEFAULT 1,
    map_name VARCHAR(64) NOT NULL DEFAULT 'de_dust2',
    mode TINYINT DEFAULT 0,
    max_players INT DEFAULT 10,
    password VARCHAR(32) DEFAULT '',
    server_ip VARCHAR(64) DEFAULT '',
    server_port INT DEFAULT 27015,
    rcon_password VARCHAR(64) DEFAULT '',
    process_id INT DEFAULT 0,
    status TINYINT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    started_at DATETIME,
    ended_at DATETIME,
    last_heartbeat_at DATETIME,
    crash_count INT DEFAULT 0,
    FOREIGN KEY (host_user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS duel_invites (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    from_user_id BIGINT NOT NULL,
    to_user_id BIGINT NOT NULL,
    game_id INT DEFAULT 1,
    map_name VARCHAR(64) DEFAULT 'de_dust2',
    status TINYINT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME,
    FOREIGN KEY (from_user_id) REFERENCES users(id),
    FOREIGN KEY (to_user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
