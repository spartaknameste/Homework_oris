-- Alias Game Database Schema
-- PostgreSQL

-- Create database (run as superuser)
-- CREATE DATABASE alias_game;
-- CREATE USER alias_user WITH PASSWORD 'alias_password';
-- GRANT ALL PRIVILEGES ON DATABASE alias_game TO alias_user;

-- Connect to alias_game database before running this script

-- Users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    email VARCHAR(100) UNIQUE,
    is_admin BOOLEAN DEFAULT FALSE,
    is_banned BOOLEAN DEFAULT FALSE,
    ban_reason TEXT,
    ban_until TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login TIMESTAMP,
    games_played INTEGER DEFAULT 0,
    games_won INTEGER DEFAULT 0,
    total_score INTEGER DEFAULT 0
);

-- Categories table
CREATE TABLE IF NOT EXISTS categories (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Words table
CREATE TABLE IF NOT EXISTS words (
    id SERIAL PRIMARY KEY,
    word_text VARCHAR(100) NOT NULL,
    category_id INTEGER NOT NULL REFERENCES categories(id) ON DELETE CASCADE,
    difficulty INTEGER DEFAULT 1 CHECK (difficulty >= 1 AND difficulty <= 5),
    times_used INTEGER DEFAULT 0,
    times_guessed INTEGER DEFAULT 0,
    times_skipped INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(word_text, category_id)
);

-- Game history table
CREATE TABLE IF NOT EXISTS game_history (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    game_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    team_name VARCHAR(100),
    final_score INTEGER NOT NULL,
    is_winner BOOLEAN DEFAULT FALSE,
    words_explained INTEGER DEFAULT 0,
    words_guessed INTEGER DEFAULT 0,
    game_duration_seconds INTEGER DEFAULT 0
);

-- Game settings presets table
CREATE TABLE IF NOT EXISTS game_settings_presets (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    round_time_seconds INTEGER DEFAULT 60,
    total_rounds INTEGER DEFAULT 10,
    score_to_win INTEGER DEFAULT 50,
    last_word_time_seconds INTEGER DEFAULT 10,
    allow_manual_score_change BOOLEAN DEFAULT TRUE,
    allow_host_pass_turn BOOLEAN DEFAULT TRUE,
    skip_penalty INTEGER DEFAULT 0,
    is_default BOOLEAN DEFAULT FALSE,
    created_by_user_id INTEGER REFERENCES users(id)
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_words_category ON words(category_id);
CREATE INDEX IF NOT EXISTS idx_words_active ON words(is_active);
CREATE INDEX IF NOT EXISTS idx_game_history_user ON game_history(user_id);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- Insert default admin user (password: admin123)
INSERT INTO users (username, password_hash, is_admin) 
VALUES ('admin', '$2a$11$rK7StGLAqHRrG8K8YgK6/.HxHF6gKJDCDFQ5.vVL0L0QXJY0kKzXu', TRUE)
ON CONFLICT (username) DO NOTHING;

-- Insert default settings preset
INSERT INTO game_settings_presets (name, is_default)
VALUES ('Стандартные настройки', TRUE)
ON CONFLICT DO NOTHING;
