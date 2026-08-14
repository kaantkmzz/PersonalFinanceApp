-- PersonalFinanceApp veritabanı şeması.
-- Yeni bir PostgreSQL sunucusunda sıfırdan kurulum için:
--   1. Boş bir veritabanı oluşturun (örn. `createdb PersonalFinanceDb`)
--   2. Bu dosyayı çalıştırın: `psql -h <host> -p <port> -U <kullanıcı> -d PersonalFinanceDb -f schema.sql`
--   3. PersonalFinanceApp/config.json içindeki ConnectionString'i buna göre ayarlayın.

CREATE TABLE users (
    user_id               SERIAL PRIMARY KEY,
    username              TEXT NOT NULL UNIQUE,
    email                 TEXT NOT NULL UNIQUE,
    password_hash         TEXT NOT NULL,
    created_at             TIMESTAMP NOT NULL DEFAULT NOW(),
    onboarding_completed  BOOLEAN NOT NULL DEFAULT FALSE,
    hide_amounts          BOOLEAN NOT NULL DEFAULT FALSE,
    monthly_income        NUMERIC(14,2) NOT NULL DEFAULT 0,
    wallet_balance        NUMERIC(14,2) NOT NULL DEFAULT 0,
    safe_balance           NUMERIC(14,2) NOT NULL DEFAULT 0,
    last_income_month     INTEGER,
    last_income_year      INTEGER
);

CREATE TABLE categories (
    category_id  SERIAL PRIMARY KEY,
    user_id      INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    name         TEXT NOT NULL,
    type         TEXT NOT NULL CHECK (type IN ('income', 'expense'))
);

CREATE TABLE transactions (
    transaction_id    SERIAL PRIMARY KEY,
    user_id           INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    category_id       INTEGER NOT NULL REFERENCES categories(category_id),
    amount            NUMERIC(14,2) NOT NULL,
    type              TEXT NOT NULL CHECK (type IN ('income', 'expense')),
    description       TEXT,
    transaction_date  TIMESTAMP NOT NULL,
    created_at        TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE recurring_transactions (
    recurring_id           SERIAL PRIMARY KEY,
    user_id                INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    category_id            INTEGER NOT NULL REFERENCES categories(category_id),
    amount                 NUMERIC(14,2) NOT NULL,
    type                   TEXT NOT NULL CHECK (type IN ('income', 'expense')),
    description            TEXT,
    is_active              BOOLEAN NOT NULL DEFAULT TRUE,
    last_processed_month   INTEGER,
    last_processed_year    INTEGER,
    created_at             TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE savings_goals (
    goal_id         SERIAL PRIMARY KEY,
    user_id         INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    goal_name       TEXT NOT NULL,
    target_amount   NUMERIC(14,2) NOT NULL,
    current_amount  NUMERIC(14,2) NOT NULL DEFAULT 0,
    is_achieved     BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE notes (
    note_id     SERIAL PRIMARY KEY,
    user_id     INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    title       TEXT,
    content     TEXT,
    created_at  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE reminders (
    reminder_id     SERIAL PRIMARY KEY,
    user_id         INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    title           TEXT NOT NULL,
    reminder_date   TIMESTAMP NOT NULL,
    is_completed    BOOLEAN NOT NULL DEFAULT FALSE,
    is_notified     BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE transfer_history (
    transfer_id  SERIAL PRIMARY KEY,
    user_id      INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    direction    TEXT NOT NULL,
    amount       NUMERIC(14,2) NOT NULL,
    created_at   TIMESTAMP NOT NULL DEFAULT NOW()
);
