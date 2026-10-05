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
    path: '**',
    loadComponent: () =>
      import('./shared/pages/not-found/not-found')
        .then(component => component.NotFound)
  }
];
