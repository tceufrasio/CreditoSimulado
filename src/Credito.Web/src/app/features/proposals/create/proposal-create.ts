import { Component, inject } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { ProposalService } from '../../../core/services/proposal.service';
import { CurrencyInputDirective } from '../../../shared/directives/currency-input.directive';

@Component({
  selector: 'app-proposal-create',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyInputDirective
  ],
  templateUrl: './proposal-create.html',
  styleUrl: './proposal-create.scss'
})
export class ProposalCreate {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly proposalService = inject(ProposalService);

  submitting = false;
  apiError = '';

  readonly form = this.fb.nonNullable.group({
    customerReference: [
      '',
      [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(30),
        Validators.pattern(/^[a-zA-Z0-9]+$/)
      ]
    ],
    amount: [
      10000,
      [
        Validators.required,
        Validators.min(1000),
        Validators.max(100000)
      ]
    ],
    termMonths: [
      12,
      [
        Validators.required,
        Validators.min(6),
        Validators.max(48)
      ]
    ],
    monthlyIncome: [
      5000,
      [
        Validators.required,
        Validators.min(1000)
      ]
    ]
  });

  get customerReference() {
    return this.form.controls.customerReference;
  }

  get amount() {
    return this.form.controls.amount;
  }

  get termMonths() {
    return this.form.controls.termMonths;
  }

  get monthlyIncome() {
    return this.form.controls.monthlyIncome;
  }

  submit(): void {
    this.apiError = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting = true;

    this.proposalService.create(this.form.getRawValue()).subscribe({
      next: proposal => {
        this.router.navigate(['/proposals', proposal.id]);
      },
      error: (error: HttpErrorResponse) => {
        this.submitting = false;

        this.apiError =
          error.error?.detail ??
          error.error?.title ??
          'Não foi possível cadastrar a proposta.';
      }
    });
  }
}
