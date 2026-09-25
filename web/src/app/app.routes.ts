import { Routes } from '@angular/router';
import { Overview } from './overview.component';
import { NotFound } from './not-found';

export const routes: Routes = [
  { path: '', component: Overview, title: 'Clash Insights' },
  { path: '**', component: NotFound, title: 'Page not found · Clash Insights' },
];
