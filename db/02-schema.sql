CREATE TABLE IF NOT EXISTS credit_proposals (
  id uuid PRIMARY KEY,
  customer_reference varchar(30) NOT NULL,
  amount numeric(12,2) NOT NULL,
  term_months integer NOT NULL,
  monthly_income numeric(12,2) NOT NULL,
  created_at_utc timestamptz NOT NULL,
  status varchar(20) NOT NULL,
  monthly_payment numeric(12,2),
  commitment_percent numeric(6,2),
  reason varchar(250),
  decision_source varchar(20),
  interest_rate_percent numeric(7,4),
  CONSTRAINT ck_credit_status CHECK (
    status IN ('Pending', 'Approved', 'ManualReview', 'Rejected')
  ),
  CONSTRAINT ck_credit_decision_source CHECK (
    decision_source IS NULL
    OR decision_source IN ('Automatic', 'Manual')
  ),
  CONSTRAINT ck_credit_amount CHECK (
    amount BETWEEN 1000 AND 100000
  ),
  CONSTRAINT ck_credit_term CHECK (
    term_months BETWEEN 6 AND 48
  ),
  CONSTRAINT ck_credit_income CHECK (
    monthly_income >= 1000
  )
);

CREATE TABLE IF NOT EXISTS credit_rates (
  id uuid PRIMARY KEY,
  monthly_rate_percent numeric(7,4) NOT NULL,
  created_at_utc timestamptz NOT NULL,
  is_active boolean NOT NULL,
  CONSTRAINT ck_credit_rate_positive CHECK (
    monthly_rate_percent > 0 AND monthly_rate_percent <= 100
  )
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_credit_rates_active
  ON credit_rates (is_active)
  WHERE is_active = true;

INSERT INTO credit_rates (
  id,
  monthly_rate_percent,
  created_at_utc,
  is_active
)
SELECT
  '00000000-0000-0000-0000-000000000001'::uuid,
  1.5000,
  NOW(),
  true
WHERE NOT EXISTS (
  SELECT 1
  FROM credit_rates
  WHERE is_active = true
);
