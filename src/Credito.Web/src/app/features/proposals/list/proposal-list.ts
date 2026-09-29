import { Component, inject, OnInit } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import {
  Proposal,
  ProposalStatus
} from '../../../core/models/proposal';

import { ProposalService } from '../../../core/services/proposal.service';

@Component({
  selector: 'app-proposal-list',
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    RouterLink
  ],
  templateUrl: './proposal-list.html',
  styleUrl: './proposal-list.scss'
})
export class ProposalList implements OnInit {
  private readonly proposalService = inject(ProposalService);

  proposals: Proposal[] = [];
  selectedStatus: ProposalStatus | '' = '';

  loading = true;
  error = false;

  currentPage = 1;
  pageSize = 5;
  readonly pageSizeOptions = [5, 10, 20, 50, 100];

  ngOnInit(): void {
    this.load();
  }

  load(status: ProposalStatus | '' = this.selectedStatus): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loading = true;
    this.error = false;

    this.proposalService
      .list(status || undefined)
      .subscribe({
        next: proposals => {
          this.proposals = proposals;
          this.loading = false;
        },
        error: () => {
          this.error = true;
          this.loading = false;
        }
      });
  }

  get paginatedProposals(): Proposal[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.proposals.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.max(
      1,
      Math.ceil(this.proposals.length / this.pageSize)
    );
  }

  get pageNumbers(): number[] {
    return Array.from(
      { length: this.totalPages },
      (_, index) => index + 1
    );
  }

  get firstItem(): number {
    if (this.proposals.length === 0) {
      return 0;
    }

    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get lastItem(): number {
    return Math.min(
      this.currentPage * this.pageSize,
      this.proposals.length
    );
  }

  changePageSize(): void {
    this.currentPage = 1;
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages) {
      return;
    }

    this.currentPage = page;
  }

  previousPage(): void {
    this.goToPage(this.currentPage - 1);
  }

  nextPage(): void {
    this.goToPage(this.currentPage + 1);
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
}
