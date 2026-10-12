import { Routes } from '@angular/router';
import { authGuard } from './shared/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'documents',
    pathMatch: 'full'
  },
  {
    path: 'documents',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/documents/dashboard/dashboard')
        .then(component => component.Dashboard)

  },
  {
    path: 'documents/upload',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/documents/upload/upload').then(component => component.Upload)
  },
  {
    path: 'documents/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/documents/details/details').then(component => component.Details)
  },
  {
    path: 'collections',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/documents/collections/collections')
        .then(component => component.Collections)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./shared/pages/login/login')
        .then(component => component.Login)
  },
  {
    path: '**',
    loadComponent: () =>
      import('./shared/pages/not-found/not-found')
        .then(component => component.NotFound)
  },
];
