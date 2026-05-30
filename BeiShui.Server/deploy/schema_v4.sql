-- ========== v4.0 语音房间系统 ==========

CREATE TABLE IF NOT EXISTS voice_rooms (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_code VARCHAR(6) NOT NULL UNIQUE,
    name VARCHAR(64) NOT NULL,
    host_user_id BIGINT NOT NULL,
    password VARCHAR(32) DEFAULT '',
    max_users INT DEFAULT 10,
    current_users INT DEFAULT 1,
    status TINYINT DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (host_user_id) REFERENCES users(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS voice_room_members (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    room_code VARCHAR(6) NOT NULL,
    user_id BIGINT NOT NULL,
    joined_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uk_room_user (room_code, user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
