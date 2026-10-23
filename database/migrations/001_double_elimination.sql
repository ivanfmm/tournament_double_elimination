-- Para bases que ya existen (el volumen tournament_pgdata no vuelve a correr init/).
-- Bases nuevas no lo necesitan: db_script.sql ya trae estas columnas.
-- podman exec -i tournament_db psql -U tournament_admin -d tournament_db < database/migrations/001_double_elimination.sql

ALTER TABLE TOURNAMENTS
    ADD COLUMN IF NOT EXISTS status TEXT NOT NULL DEFAULT 'Created',
    ADD COLUMN IF NOT EXISTS champion_team_id UUID NULL REFERENCES teams(id),
    ADD COLUMN IF NOT EXISTS seeded_team_ids TEXT[] NOT NULL DEFAULT '{}';

ALTER TABLE MATCHES
    ADD COLUMN IF NOT EXISTS bracket_key TEXT NULL,
    ADD COLUMN IF NOT EXISTS result_order INT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS match_unique_bracket_key_idx ON MATCHES (tournament_id, bracket_key)
    WHERE bracket_key IS NOT NULL AND deleted_at IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS match_unique_result_order_idx ON MATCHES (tournament_id, result_order)
    WHERE result_order IS NOT NULL AND deleted_at IS NULL;
