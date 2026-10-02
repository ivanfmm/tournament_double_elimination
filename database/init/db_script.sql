-- podman run -d --replace --name=tournament_db --network development -e POSTGRES_PASSWORD=password -p 5432:5432 postgres:17.6-alpine3.22
-- podman exec -i tournament_db psql -U postgres -d postgres < db_script.sql

CREATE USER tournament_svc WITH PASSWORD 'password';
CREATE USER tournament_admin WITH PASSWORD 'password';

CREATE DATABASE tournament_db;

\connect tournament_db

grant all privileges on database tournament_db to tournament_admin;
grant all privileges on database tournament_db to tournament_svc;
grant usage on schema public to tournament_admin;
grant usage on schema public to tournament_svc;

GRANT SELECT ON ALL TABLES IN SCHEMA public TO tournament_admin;
GRANT DELETE ON ALL TABLES IN SCHEMA public TO tournament_admin;
GRANT UPDATE ON ALL TABLES IN SCHEMA public TO tournament_admin;
GRANT INSERT ON ALL TABLES IN SCHEMA public TO tournament_admin;
GRANT CREATE ON SCHEMA public TO tournament_admin;

\connect tournament_db tournament_admin

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE TABLE TEAMS (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    name TEXT NOT NULL,
    last_update_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ DEFAULT NULL
);
CREATE UNIQUE INDEX team_unique_name_idx ON teams (name) WHERE deleted_at IS NULL;

CREATE TABLE TOURNAMENTS (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    name TEXT NOT NULL,
    format_type TEXT NOT NULL,
    last_update_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ DEFAULT NULL
);
CREATE UNIQUE INDEX tournament_unique_name_idx ON TOURNAMENTS (name) WHERE deleted_at IS NULL;

CREATE TABLE GROUPS (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    tournament_id UUID not null references TOURNAMENTS(id),
    name TEXT NOT NULL,
    last_update_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ DEFAULT NULL
);
CREATE UNIQUE INDEX tournament_group_unique_name_idx ON GROUPS (tournament_id, name) WHERE deleted_at IS NULL;

CREATE TABLE GROUP_TEAMS (
    group_id UUID NOT NULL REFERENCES groups(id),
    team_id  UUID NOT NULL REFERENCES teams(id),
    PRIMARY KEY (group_id, team_id)
);


CREATE TABLE MATCHES (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    tournament_id UUID NOT NULL REFERENCES tournaments(id),
    group_id UUID NULL REFERENCES groups(id) ON DELETE SET NULL,
    home_team_id UUID NOT NULL REFERENCES teams(id),
    visitor_team_id UUID NOT NULL REFERENCES teams(id),
    home_team_score INT NOT NULL DEFAULT 0,
    visitor_team_score INT NOT NULL DEFAULT 0,
    winner TEXT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    last_update_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMPTZ DEFAULT NULL,

    CONSTRAINT matches_teams_different_check CHECK (home_team_id <> visitor_team_id),
    CONSTRAINT matches_winner_check CHECK (winner IN ('Home', 'Visitor')),
    CONSTRAINT matches_scores_non_negative_check CHECK (COALESCE(home_team_score, 0) >= 0 AND COALESCE(visitor_team_score, 0) >= 0),
    CONSTRAINT matches_score_both_or_none_check CHECK ((home_team_score IS NULL) = (visitor_team_score IS NULL))
);

GRANT SELECT ON ALL TABLES IN SCHEMA public TO tournament_svc;
GRANT DELETE ON ALL TABLES IN SCHEMA public TO tournament_svc;
GRANT UPDATE ON ALL TABLES IN SCHEMA public TO tournament_svc;
GRANT INSERT ON ALL TABLES IN SCHEMA public TO tournament_svc;
