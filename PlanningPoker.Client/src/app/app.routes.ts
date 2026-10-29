import { Routes } from '@angular/router';
import { Credits } from './features/credits/credits';
import { Home } from './features/home/home';
import { Room } from './features/room/room';

export const routes: Routes = [
  { path: '', component: Home },
  { path: 'room/:id', component: Room },
  { path: 'credits', component: Credits },
  // Links from the previous Razor app used capitalised paths.
  { path: 'Room/:id', redirectTo: 'room/:id' },
  { path: 'Credits', redirectTo: 'credits' },
  { path: '**', redirectTo: '' },
];
