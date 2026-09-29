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
  CONSTRAINT ck_credit_status CHECK (status IN ('Pending', 'Approved', 'ManualReview', 'Rejected')),
  CONSTRAINT ck_credit_amount CHECK (amount BETWEEN 1000 AND 100000),
  CONSTRAINT ck_credit_term CHECK (term_months BETWEEN 6 AND 48),
  CONSTRAINT ck_credit_income CHECK (monthly_income >= 1000)
);
