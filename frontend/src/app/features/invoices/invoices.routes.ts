import { Routes } from '@angular/router';
import { roleGuard } from '@core/auth';

export const INVOICE_ROUTES: Routes = [
  {
    path: '',
    title: 'Invoices · Accrue',
    loadComponent: () => import('./invoice-list/invoice-list.component').then(m => m.InvoiceListComponent),
  },
  {
    path: 'new',
    title: 'New invoice · Accrue',
    canActivate: [roleGuard('Clerk', 'Controller')],
    loadComponent: () => import('./invoice-form/invoice-form.component').then(m => m.InvoiceFormComponent),
  },
  {
    path: ':id/edit',
    title: 'Edit invoice · Accrue',
    canActivate: [roleGuard('Clerk', 'Controller')],
    loadComponent: () => import('./invoice-form/invoice-form.component').then(m => m.InvoiceFormComponent),
  },
  {
    path: ':id',
    title: 'Invoice · Accrue',
    loadComponent: () => import('./invoice-detail/invoice-detail.component').then(m => m.InvoiceDetailComponent),
  },
];
