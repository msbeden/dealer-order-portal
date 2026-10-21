import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'catalog',
    loadComponent: () => import('./features/catalog/catalog.component').then(m => m.CatalogComponent),
    canActivate: [authGuard]
  },
  {
    path: 'cart',
    loadComponent: () => import('./features/cart/cart.component').then(m => m.CartComponent),
    canActivate: [authGuard]
  },
  {
    path: 'orders',
    loadComponent: () => import('./features/orders/order-list.component').then(m => m.OrderListComponent),
    canActivate: [authGuard]
  },
  {
    path: 'orders/:id',
    loadComponent: () => import('./features/orders/order-detail.component').then(m => m.OrderDetailComponent),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent),
    canActivate: [authGuard]
  },
  {
    path: '',
    redirectTo: 'catalog',
    pathMatch: 'full'
  },
  {
    path: '**',
    redirectTo: 'catalog'
  }
];
