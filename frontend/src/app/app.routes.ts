import { Routes } from '@angular/router';
import { authGuard, roleGuard } from '@core/auth';
import { LoginComponent } from '@features/auth/login/login.component';
import { ShellComponent } from '@layout/shell/shell.component';

/** Top-level routes. Each feature is lazy loaded; multi-page features own their child routes. */
export const routes: Routes = [
  { path: 'login', component: LoginComponent, title: 'Sign in · Accrue' },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        title: 'Dashboard · Accrue',
        loadComponent: () => import('@features/dashboard/dashboard.component').then(m => m.DashboardComponent),
      },
      {
        path: 'invoices',
        loadChildren: () => import('@features/invoices/invoices.routes').then(m => m.INVOICE_ROUTES),
      },
      {
        path: 'approvals',
        title: 'Approvals · Accrue',
        canActivate: [roleGuard('Approver', 'Controller')],
        loadComponent: () => import('@features/approvals/approval-queue/approval-queue.component').then(m => m.ApprovalQueueComponent),
      },
      {
        path: 'payment-runs',
        loadChildren: () => import('@features/payment-runs/payment-runs.routes').then(m => m.PAYMENT_RUN_ROUTES),
      },
      {
        path: 'vendors',
        title: 'Vendors · Accrue',
        loadComponent: () => import('@features/vendors/vendor-list/vendor-list.component').then(m => m.VendorListComponent),
      },
      {
        path: 'reports',
        title: 'Reports · Accrue',
        loadComponent: () => import('@features/reports/report-center/report-center.component').then(m => m.ReportCenterComponent),
      },
      {
        path: 'jobs',
        title: 'Background jobs · Accrue',
        canActivate: [roleGuard('Controller')],
        loadComponent: () => import('@features/jobs/job-list/job-list.component').then(m => m.JobListComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
