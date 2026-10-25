import { Routes } from '@angular/router';
import { roleGuard } from '@core/auth';

export const PAYMENT_RUN_ROUTES: Routes = [
  {
    path: '',
    title: 'Payment runs · Accrue',
    loadComponent: () => import('./payment-run-list/payment-run-list.component').then(m => m.PaymentRunListComponent),
  },
  {
    path: 'new',
    title: 'New payment run · Accrue',
    canActivate: [roleGuard('Clerk', 'Controller')],
    loadComponent: () => import('./payment-run-new/payment-run-new.component').then(m => m.PaymentRunNewComponent),
  },
  {
    path: ':id',
    title: 'Payment run · Accrue',
    loadComponent: () => import('./payment-run-detail/payment-run-detail.component').then(m => m.PaymentRunDetailComponent),
  },
];
