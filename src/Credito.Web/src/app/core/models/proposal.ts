export type ProposalStatus =
  | 'Pending'
  | 'Approved'
  | 'ManualReview'
  | 'Rejected';

export type DecisionSource =
  | 'Automatic'
  | 'Manual';

export interface CreditDecision {
  status: ProposalStatus;
  monthlyPayment: number;
  incomeCommitmentPercent: number;
  reason: string;
  source: DecisionSource;
}

export interface Proposal {
  id: string;
  customerReference: string;
  amount: number;
  termMonths: number;
  monthlyIncome: number;
  createdAtUtc: string;
  status: ProposalStatus;
  decision: CreditDecision | null;
}
export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}