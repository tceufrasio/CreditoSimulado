import { CommonModule } from '@angular/common';
import { Component, inject, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { CreditRateService } from '../../core/services/credit-rate.service';

@Component({
  selector: 'app-credit-rate',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './credit-rate.html',
  styleUrl: './credit-rate.css'
})
export class CreditRateComponent implements OnInit {
  private readonly service = inject(CreditRateService);

  monthlyRatePercent: number | null = null;
  createdAtUtc: string | null = null;

  loading = true;
  saving = false;
  successMessage = '';
  errorMessage = '';

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading = true;
    this.errorMessage = '';

    this.service.getCurrent().subscribe({
      next: rate => {
        this.monthlyRatePercent = rate.monthlyRatePercent;
        this.createdAtUtc = rate.createdAtUtc;
        this.loading = false;
      },
      error: () => {
        this.errorMessage = 'Não foi possível carregar a taxa de crédito.';
        this.loading = false;
      }
    });
  }

  save(): void {
    this.successMessage = '';
    this.errorMessage = '';

    if (
      this.monthlyRatePercent === null ||
      this.monthlyRatePercent <= 0 ||
      this.monthlyRatePercent > 100
    ) {
      this.errorMessage = 'Informe uma taxa mensal válida.';
      return;
    }

    this.saving = true;

    this.service.update({
      monthlyRatePercent: this.monthlyRatePercent
    }).subscribe({
      next: rate => {
        this.monthlyRatePercent = rate.monthlyRatePercent;
        this.createdAtUtc = rate.createdAtUtc;
        this.successMessage = 'Taxa de crédito atualizada com sucesso.';
        this.saving = false;
      },
      error: () => {
        this.errorMessage = 'Não foi possível atualizar a taxa de crédito.';
        this.saving = false;
      }
    });
  }
}
