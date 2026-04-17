import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/landing/landing-page.component').then((m) => m.LandingPageComponent)
  },
  {
    path: 'upload',
    loadComponent: () =>
      import('./features/upload/upload-page.component').then((m) => m.UploadPageComponent)
  },
  {
    path: 'files/:token',
    loadComponent: () =>
      import('./features/public-download/public-download-page.component').then(
        (m) => m.PublicDownloadPageComponent
      )
  },
  {
    path: '**',
    redirectTo: ''
  }
];
