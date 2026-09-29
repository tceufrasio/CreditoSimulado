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
  totalItems = 0;
  totalPages = 0;

  readonly pageSizeOptions = [5, 10, 20, 50, 100];

  ngOnInit(): void {
    this.loadPage();
  }

  load(status: ProposalStatus | '' = this.selectedStatus): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadPage();
  }

  private loadPage(): void {
    this.loading = true;
    this.error = false;

    this.proposalService
      .list(
        this.selectedStatus || undefined,
        this.currentPage,
        this.pageSize
      )
      .subscribe({
        next: result => {
          this.proposals = result.items;
          this.currentPage = result.page;
          this.pageSize = result.pageSize;
          this.totalItems = result.totalItems;
          this.totalPages = result.totalPages;
          this.loading = false;
        },
        error: () => {
          this.error = true;
          this.loading = false;
        }
      });
  }

  get pageNumbers(): number[] {
    return Array.from(
      { length: this.totalPages },
      (_, index) => index + 1
    );
  }

  get firstItem(): number {
    if (this.totalItems === 0) {
      return 0;
    }

    return (this.currentPage - 1) * this.pageSize + 1;
  }

  get lastItem(): number {
    return Math.min(
      this.currentPage * this.pageSize,
      this.totalItems
    );
  }

  changePageSize(): void {
    this.currentPage = 1;
    this.loadPage();
  }

  goToPage(page: number): void {
    if (
      page < 1 ||
      page > this.totalPages ||
      page === this.currentPage
    ) {
      return;
    }

    this.currentPage = page;
    this.loadPage();
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
