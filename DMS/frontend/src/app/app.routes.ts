import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'documents',
    pathMatch: 'full'
  },
  {
    path: 'documents',
    loadComponent: () =>
      import('./features/documents/dashboard/dashboard')
        .then(component => component.Dashboard)

  },
  {
    path: 'documents/upload',
    loadComponent: () =>
      import('./features/documents/upload/upload').then(component => component.Upload)
  },
  {
    path: 'documents/:id',
    loadComponent: () =>
      import('./features/documents/details/details').then(component => component.Details)
  },
  {
    path: 'collections',
    loadComponent: () =>
      import('./features/documents/collections/collections')
        .then(component => component.Collections)
  },
  {
    path: '**',
    loadComponent: () =>
      import('./shared/pages/not-found/not-found')
        .then(component => component.NotFound)
  },
];
