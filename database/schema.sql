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
    invest_balance         NUMERIC(14,2) NOT NULL DEFAULT 0,
    last_income_month     INTEGER,
    last_income_year      INTEGER,
    full_name                    TEXT NOT NULL DEFAULT '',
    cleanup_frequency            VARCHAR(10) NOT NULL DEFAULT 'never',
    cleanup_period_start         TIMESTAMP NOT NULL DEFAULT NOW(),
    cleanup_export_before_clear  BOOLEAN NOT NULL DEFAULT FALSE,
    avatar_color                 VARCHAR(7) NOT NULL DEFAULT '#6366F1'
);

CREATE TABLE categories (
    category_id  SERIAL PRIMARY KEY,
    user_id      INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    name         TEXT NOT NULL,
    type         TEXT NOT NULL CHECK (type IN ('income', 'expense', 'goal', 'invest'))
);

CREATE TABLE transactions (
    transaction_id    SERIAL PRIMARY KEY,
    user_id           INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    category_id       INTEGER NOT NULL REFERENCES categories(category_id),
    amount            NUMERIC(14,2) NOT NULL,
    type              TEXT NOT NULL CHECK (type IN ('income', 'expense', 'goal', 'invest')),
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
    frequency              VARCHAR(10) NOT NULL DEFAULT 'monthly',
    last_processed_month   INTEGER,
    last_processed_year    INTEGER,
    last_processed_date    DATE,
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

-- Hedeflerim: bir hedefe yapılan tekil yatırım/katkı geçmişi
CREATE TABLE savings_goal_investments (
    investment_id  SERIAL PRIMARY KEY,
    goal_id        INTEGER NOT NULL REFERENCES savings_goals(goal_id) ON DELETE CASCADE,
    user_id        INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    amount         NUMERIC(14,2) NOT NULL,
    invested_at    TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE notes (
    note_id     SERIAL PRIMARY KEY,
    user_id     INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    title       TEXT,
    content     TEXT,
    created_at  TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMP NOT NULL DEFAULT NOW()
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

-- Varlıklarım: kullanıcının döviz/altın/kripto pozisyonları (miktar + ağırlıklı ortalama maliyet)
CREATE TABLE user_holdings (
    holding_id    SERIAL PRIMARY KEY,
    user_id       INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    symbol        TEXT NOT NULL,
    asset_type    TEXT NOT NULL CHECK (asset_type IN ('crypto', 'currency', 'gold')),
    quantity      NUMERIC(20,8) NOT NULL DEFAULT 0,
    avg_cost_try  NUMERIC(14,4) NOT NULL DEFAULT 0,
    created_at    TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, symbol)
);

-- Varlıklarım: alım/satım işlem geçmişi
CREATE TABLE asset_transactions (
    asset_tx_id  SERIAL PRIMARY KEY,
    user_id      INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    symbol       TEXT NOT NULL,
    asset_type   TEXT NOT NULL,
    direction    TEXT NOT NULL CHECK (direction IN ('buy', 'sell')),
    quantity     NUMERIC(20,8) NOT NULL,
    price_try    NUMERIC(14,4) NOT NULL,
    total_try    NUMERIC(14,2) NOT NULL,
    created_at   TIMESTAMP NOT NULL DEFAULT NOW()
);

-- Varlıklarım: Rapor'daki "Portföy Değeri Gelişimi" çizgi grafiği için günlük toplam değer anlık görüntüsü
CREATE TABLE portfolio_value_snapshots (
    snapshot_id      SERIAL PRIMARY KEY,
    user_id          INTEGER NOT NULL REFERENCES users(user_id) ON DELETE CASCADE,
    snapshot_date    DATE NOT NULL,
    total_value_try  NUMERIC(16,2) NOT NULL,
    created_at       TIMESTAMP NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, snapshot_date)
);
