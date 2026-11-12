import { Routes } from '@angular/router';
import { Credits } from './features/credits/credits';
import { Home } from './features/home/home';

export const routes: Routes = [
  { path: '', title: 'Home', component: Home },
  // Lazy, so the fireworks library loads only when someone opens a room.
  { path: 'room/:id', title: 'Room', loadComponent: () => import('./features/room/room').then((m) => m.Room) },
  { path: 'credits', title: 'Credits', component: Credits },
  // The Operator's management page: lazy, so none of it is in the public bundle, and nothing in the public UI links to it.
  { path: 'admin', loadChildren: () => import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES) },
  // Links from the previous Razor app used capitalised paths.
  { path: 'Room/:id', redirectTo: 'room/:id' },
  { path: 'Credits', redirectTo: 'credits' },
  { path: '**', redirectTo: '' },
];
