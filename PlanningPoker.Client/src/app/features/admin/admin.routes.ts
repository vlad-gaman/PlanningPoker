import { Routes } from '@angular/router';
import { adminGuard, signedOutGuard } from '../../core/admin.guard';
import { AdminHome } from './admin-home';
import { AdminSignIn } from './admin-sign-in';

/** The management page. Loaded lazily from app.routes.ts, so none of it is in the public bundle, and nothing in the public UI links here. */
export const ADMIN_ROUTES: Routes = [
  { path: 'sign-in', title: 'Operator sign-in', canActivate: [signedOutGuard], component: AdminSignIn },
  { path: '', title: 'Management', canActivate: [adminGuard], component: AdminHome },
  { path: '**', redirectTo: '' },
];
