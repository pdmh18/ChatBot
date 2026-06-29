import { Routes } from '@angular/router';
import { MainLayout } from './layouts/main-layout/main-layout';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },

  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'tasks',
        loadComponent: () => import('./pages/tasks/tasks').then((m) => m.Tasks),
      },
      {
        path: 'kanban',
        loadComponent: () => import('./pages/kanban/kanban').then((m) => m.Kanban),
      },
      {
        path: 'gantt',
        loadComponent: () => import('./pages/gantt/gantt').then((m) => m.Gantt),
      },
      {
        path: 'ai-alerts',
        loadComponent: () => import('./pages/ai-alerts/ai-alerts').then((m) => m.AiAlerts),
      },
      {
        path: 'assignment-compare',
        loadComponent: () => import('./pages/assignment-compare/assignment-compare').then((m) => m.AssignmentCompare),
      },
    ],
  },
];
