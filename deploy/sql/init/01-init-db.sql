-- GameConsole Database Initialization Script
-- This script sets up the basic database schema for the GameConsole application

-- Create extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create schemas
CREATE SCHEMA IF NOT EXISTS gameconsole;
CREATE SCHEMA IF NOT EXISTS audit;

-- Set search path
SET search_path = gameconsole, public;

-- Game Engine Tables
CREATE TABLE IF NOT EXISTS game_sessions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    session_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    active BOOLEAN DEFAULT true,
    metadata JSONB DEFAULT '{}'::jsonb
);

CREATE TABLE IF NOT EXISTS game_components (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    component_type VARCHAR(100) NOT NULL,
    component_name VARCHAR(255) NOT NULL,
    configuration JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Audio System Tables
CREATE TABLE IF NOT EXISTS audio_samples (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    sample_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500),
    sample_rate INTEGER,
    duration_ms INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Graphics System Tables
CREATE TABLE IF NOT EXISTS graphics_resources (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    resource_type VARCHAR(100) NOT NULL,
    resource_name VARCHAR(255) NOT NULL,
    file_path VARCHAR(500),
    dimensions VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Input System Tables
CREATE TABLE IF NOT EXISTS input_mappings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    input_type VARCHAR(50) NOT NULL,
    key_code VARCHAR(50) NOT NULL,
    action_name VARCHAR(100) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Plugin System Tables  
CREATE TABLE IF NOT EXISTS plugins (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    plugin_name VARCHAR(255) NOT NULL UNIQUE,
    plugin_version VARCHAR(50),
    enabled BOOLEAN DEFAULT true,
    configuration JSONB DEFAULT '{}'::jsonb,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Audit Tables
CREATE TABLE IF NOT EXISTS audit.event_log (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name VARCHAR(100) NOT NULL,
    operation VARCHAR(20) NOT NULL,
    old_data JSONB,
    new_data JSONB,
    user_id VARCHAR(100),
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_game_sessions_active ON game_sessions(active);
CREATE INDEX IF NOT EXISTS idx_game_sessions_created ON game_sessions(created_at);
CREATE INDEX IF NOT EXISTS idx_game_components_type ON game_components(component_type);
CREATE INDEX IF NOT EXISTS idx_plugins_enabled ON plugins(enabled);
CREATE INDEX IF NOT EXISTS idx_audit_table_timestamp ON audit.event_log(table_name, timestamp);

-- Create updated_at trigger function
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Apply updated_at triggers
CREATE TRIGGER update_game_sessions_updated_at BEFORE UPDATE ON game_sessions
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    
CREATE TRIGGER update_game_components_updated_at BEFORE UPDATE ON game_components
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
    
CREATE TRIGGER update_plugins_updated_at BEFORE UPDATE ON plugins
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert sample data for development/testing
INSERT INTO plugins (plugin_name, plugin_version, enabled, configuration) 
VALUES 
    ('AudioCore', '1.0.0', true, '{"max_channels": 16, "sample_rate": 44100}'),
    ('GraphicsCore', '1.0.0', true, '{"max_textures": 1000, "vsync": true}'),
    ('InputCore', '1.0.0', true, '{"mouse_sensitivity": 1.0, "keyboard_repeat": true}')
ON CONFLICT (plugin_name) DO NOTHING;

-- Create database user for the application (if not exists)
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'gameapp') THEN
        CREATE USER gameapp WITH PASSWORD 'gameapp123';
    END IF;
END
$$;

-- Grant permissions
GRANT USAGE ON SCHEMA gameconsole TO gameapp;
GRANT USAGE ON SCHEMA audit TO gameapp;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA gameconsole TO gameapp;
GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA audit TO gameapp;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA gameconsole TO gameapp;

-- Grant permissions on future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA gameconsole GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO gameapp;
ALTER DEFAULT PRIVILEGES IN SCHEMA audit GRANT SELECT, INSERT ON TABLES TO gameapp;
ALTER DEFAULT PRIVILEGES IN SCHEMA gameconsole GRANT USAGE, SELECT ON SEQUENCES TO gameapp;