import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Dashboard } from './pages/dashboard/dashboard';
import { Tasks } from './pages/tasks/tasks';
import { AiAlerts } from './pages/ai-alerts/ai-alerts';
import { Kanban } from './pages/kanban/kanban';
import { Gantt } from './pages/gantt/gantt';
import { AssignmentCompare } from './pages/assignment-compare/assignment-compare';
import { MainLayout } from './layouts/main-layout/main-layout';
import { authGuard } from './guards/auth-guard';

export const routes: Routes = [
  { path: 'login', component: Login },

  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: Dashboard },
      { path: 'tasks', component: Tasks },
      { path: 'kanban', component: Kanban },
      { path: 'gantt', component: Gantt },
      { path: 'ai-alerts', component: AiAlerts },
      { path: 'assignment-compare', component: AssignmentCompare },
    ],
  },
];
