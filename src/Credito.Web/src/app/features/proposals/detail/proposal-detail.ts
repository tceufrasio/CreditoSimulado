import { Component, inject, OnInit } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';

import {
  DecisionSource,
  Proposal,
  ProposalStatus
} from '../../../core/models/proposal';

import { ProposalService } from '../../../core/services/proposal.service';

@Component({
  selector: 'app-proposal-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    FormsModule,
    RouterLink
  ],
  templateUrl: './proposal-detail.html',
  styleUrl: './proposal-detail.scss'
})
export class ProposalDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly proposalService = inject(ProposalService);

  proposal: Proposal | null = null;

  loading = true;
  analyzing = false;
  deciding = false;
  error = false;

  operationError = '';

  manualReason = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.loading = false;
      this.error = true;
      return;
    }

    this.load(id);
  }

  load(id: string): void {
    this.loading = true;
    this.error = false;
    this.operationError = '';

    this.proposalService.getById(id).subscribe({
      next: proposal => {
        this.proposal = proposal;
        this.loading = false;
      },
      error: () => {
        this.error = true;
        this.loading = false;
      }
    });
  }

  analyze(): void {
    if (!this.proposal || this.analyzing) {
      return;
    }

    this.analyzing = true;
    this.error = false;
    this.operationError = '';

    this.proposalService.analyze(this.proposal.id).subscribe({
      next: proposal => {
        this.proposal = proposal;
        this.analyzing = false;
      },
      error: () => {
        this.analyzing = false;
        this.operationError =
          'Não foi possível executar a análise de crédito.';
      }
    });
  }

  approveManually(): void {
    this.submitManualDecision('Approved');
  }

  rejectManually(): void {
    this.submitManualDecision('Rejected');
  }

  private submitManualDecision(
    decision: 'Approved' | 'Rejected'
  ): void {
    if (!this.proposal || this.deciding) {
      return;
    }

    const reason = this.manualReason.trim();

    if (reason.length < 5) {
      this.operationError =
        'Informe uma justificativa com pelo menos 5 caracteres.';
      return;
    }

    if (reason.length > 250) {
      this.operationError =
        'A justificativa deve possuir no máximo 250 caracteres.';
      return;
    }

    this.deciding = true;
    this.operationError = '';

    this.proposalService.manualDecision(
      this.proposal.id,
      {
        decision,
        reason
      }
    ).subscribe({
      next: proposal => {
        this.proposal = proposal;
        this.manualReason = '';
        this.deciding = false;
      },
      error: (error: HttpErrorResponse) => {
        this.deciding = false;

        if (error.status === 409) {
          this.operationError =
            error.error?.detail ??
            'Esta proposta não está mais disponível para decisão manual.';
          return;
        }

        if (error.status === 400) {
          this.operationError =
            error.error?.detail ??
            'Verifique os dados informados.';
          return;
        }

        this.operationError =
          'Não foi possível registrar a decisão manual.';
      }
    });
  }

  statusLabel(status: ProposalStatus): string {
    const labels: Record<ProposalStatus, string> = {
      Pending: 'Pendente',
      Approved: 'Aprovada',
      ManualReview: 'Revisão manual',
      Rejected: 'Rejeitada'
    };

    return labels[status];
  }

  sourceLabel(source: DecisionSource): string {
    const labels: Record<DecisionSource, string> = {
      Automatic: 'Automática',
      Manual: 'Manual'
    };

    return labels[source];
  }
}
