export interface CreditRate {
  monthlyRatePercent: number;
  createdAtUtc: string;
}

export interface UpdateCreditRateRequest {
  monthlyRatePercent: number;
}
