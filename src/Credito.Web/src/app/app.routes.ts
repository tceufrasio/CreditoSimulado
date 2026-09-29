import { Routes } from '@angular/router';
import { Dashboard } from './features/dashboard/dashboard';
import { ProposalList } from './features/proposals/list/proposal-list';
import { ProposalDetail } from './features/proposals/detail/proposal-detail';
import { ProposalCreate } from './features/proposals/create/proposal-create';
import { CreditRateComponent } from './features/credit-rate/credit-rate';

export const routes: Routes = [
  {
    path: 'dashboard',
    component: Dashboard,
    title: 'Dashboard | CréditoSimulado'
  },
  {
    path: 'proposals',
    component: ProposalList,
    title: 'Propostas | CréditoSimulado'
  },
  {
    path: 'proposals/new',
    component: ProposalCreate,
    title: 'Nova proposta | CréditoSimulado'
  },
  {
    path: 'proposals/:id',
    component: ProposalDetail,
    title: 'Detalhes da proposta | CréditoSimulado'
  },
  {
    path: 'credit-rate',
    component: CreditRateComponent,
    title: 'Taxa de Crédito | CréditoSimulado'
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'dashboard'
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
