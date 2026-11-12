import { Routes } from '@angular/router';
import { Credits } from './features/credits/credits';
import { Home } from './features/home/home';

export const routes: Routes = [
  { path: '', title: 'Home', component: Home },
  // Lazy, so the fireworks library loads only when someone opens a room.
  { path: 'room/:id', title: 'Room', loadComponent: () => import('./features/room/room').then((m) => m.Room) },
  { path: 'credits', title: 'Credits', component: Credits },
  // PROTOTYPE (issue 78): throwaway management page layouts, ?variant=A|B|C. Wipe me.
  { path: 'admin-prototype', title: 'Management prototype', loadComponent: () => import('./features/admin-prototype/admin-prototype').then((m) => m.AdminPrototype) },
  // Links from the previous Razor app used capitalised paths.
  { path: 'Room/:id', redirectTo: 'room/:id' },
  { path: 'Credits', redirectTo: 'credits' },
  { path: '**', redirectTo: '' },
];
