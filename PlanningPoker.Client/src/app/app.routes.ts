import { Routes } from '@angular/router';
import { Credits } from './features/credits/credits';
import { Home } from './features/home/home';

export const routes: Routes = [
  { path: '', component: Home },
  // Lazy, so the charting and fireworks libraries load only when someone opens a room.
  { path: 'room/:id', loadComponent: () => import('./features/room/room').then((m) => m.Room) },
  { path: 'credits', component: Credits },
  // Links from the previous Razor app used capitalised paths.
  { path: 'Room/:id', redirectTo: 'room/:id' },
  { path: 'Credits', redirectTo: 'credits' },
  { path: '**', redirectTo: '' },
];
