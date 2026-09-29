import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  PagedResponse,
  Proposal,
  ProposalStatus
} from '../models/proposal';

export interface CreateProposalRequest {
  customerReference: string;
  amount: number;
  termMonths: number;
  monthlyIncome: number;
}

export interface ManualDecisionRequest {
  decision: 'Approved' | 'Rejected';
  reason: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProposalService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/proposals';

  list(
    status?: ProposalStatus,
    page = 1,
    pageSize = 5
  ): Observable<PagedResponse<Proposal>> {
    const params: Record<string, string> = {
      page: page.toString(),
      pageSize: pageSize.toString()
    };

    if (status) {
      params['status'] = status;
    }

    return this.http.get<PagedResponse<Proposal>>(
      this.apiUrl,
      { params }
    );
  }

  getById(id: string): Observable<Proposal> {
    return this.http.get<Proposal>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateProposalRequest): Observable<Proposal> {
    return this.http.post<Proposal>(this.apiUrl, request);
  }

  analyze(id: string): Observable<Proposal> {
    return this.http.post<Proposal>(
      `${this.apiUrl}/${id}/analyze`,
      {}
    );
  }

  manualDecision(
    id: string,
    request: ManualDecisionRequest
  ): Observable<Proposal> {
    return this.http.post<Proposal>(
      `${this.apiUrl}/${id}/manual-decision`,
      request
    );
  }
}
