import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CreditRate,
  UpdateCreditRateRequest
} from '../models/credit-rate';

@Injectable({
  providedIn: 'root'
})
export class CreditRateService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5000/api/credit-rate';

  getCurrent(): Observable<CreditRate> {
    return this.http.get<CreditRate>(this.apiUrl);
  }

  update(request: UpdateCreditRateRequest): Observable<CreditRate> {
    return this.http.put<CreditRate>(this.apiUrl, request);
  }
}
