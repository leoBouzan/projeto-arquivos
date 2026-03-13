import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/upload/upload-page.component').then((module) => module.UploadPageComponent)
  },
  {
    path: 'files/:token',
    loadComponent: () =>
      import('./features/public-download/public-download-page.component').then(
        (module) => module.PublicDownloadPageComponent
      )
  },
  {
    path: '**',
    redirectTo: ''
  }
];
