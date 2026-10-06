import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { TagModule } from 'primeng/tag';
import { AuthService } from '../core/services/auth.service';
import { CartService } from '../core/services/cart.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule, ButtonModule, BadgeModule, TagModule],
  template: `
    <header class="navbar-wrapper">
      <div class="navbar-container">
        <div class="nav-brand" routerLink="/">
          <div class="brand-icon">
            <i class="pi pi-box"></i>
          </div>
          <div class="brand-text">
            <span class="brand-title">B2B Portal</span>
            <span class="brand-subtitle">Bayi Sipariş Sistemi</span>
          </div>
        </div>

        @if (authService.isAuthenticated()) {
          <nav class="nav-links">
            <a routerLink="/catalog" routerLinkActive="active-link" class="nav-item">
              <i class="pi pi-shopping-bag"></i>
              <span>Katalog</span>
            </a>
            <a routerLink="/orders" routerLinkActive="active-link" class="nav-item">
              <i class="pi pi-list"></i>
              <span>Siparişler</span>
            </a>
            @if (authService.isAdmin() || authService.isSalesRep()) {
              <a routerLink="/dashboard" routerLinkActive="active-link" class="nav-item">
                <i class="pi pi-chart-line"></i>
                <span>Yönetici Paneli</span>
              </a>
            }
          </nav>

          <div class="nav-actions">
            @if (authService.isDealer()) {
              <button
                pButton
                type="button"
                routerLink="/cart"
                class="p-button-outlined p-button-secondary cart-btn"
                [attr.aria-label]="'Sepet (' + cartService.itemCount() + ')'"
              >
                <i class="pi pi-shopping-cart"></i>
                <span class="cart-label">Sepetim</span>
                @if (cartService.itemCount() > 0) {
                  <p-badge [value]="cartService.itemCount()" severity="danger"></p-badge>
                }
              </button>
            }

            <div class="user-profile">
              <div class="user-info">
                <span class="user-name">{{ authService.currentUser()?.fullName }}</span>
                <div class="user-meta">
                  <p-tag [value]="authService.currentUser()?.role" [severity]="getRoleSeverity()"></p-tag>
                  <span class="tenant-badge">{{ authService.currentUser()?.tenantId }}</span>
                </div>
              </div>
              <button
                pButton
                icon="pi pi-sign-out"
                class="p-button-text p-button-secondary logout-btn"
                (click)="logout()"
                title="Çıkış Yap"
              ></button>
            </div>
          </div>
        }
      </div>
    </header>
  `,
  styles: [`
    .navbar-wrapper {
      background: #ffffff;
      border-bottom: 1px solid var(--surface-border);
      position: sticky;
      top: 0;
      z-index: 1000;
      box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
    }

    .navbar-container {
      max-width: 1400px;
      margin: 0 auto;
      padding: 0.75rem 1.5rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1.5rem;
    }

    .nav-brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      cursor: pointer;
      text-decoration: none;
      color: inherit;

      .brand-icon {
        width: 40px;
        height: 40px;
        background: linear-gradient(135deg, #2563eb, #1d4ed8);
        border-radius: 10px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: #ffffff;
        font-size: 1.25rem;
        box-shadow: 0 2px 4px rgba(37, 99, 235, 0.25);
      }

      .brand-title {
        font-size: 1.125rem;
        font-weight: 700;
        color: #0f172a;
        line-height: 1.2;
        display: block;
      }

      .brand-subtitle {
        font-size: 0.75rem;
        color: var(--text-muted);
        line-height: 1;
        display: block;
      }
    }

    .nav-links {
      display: flex;
      align-items: center;
      gap: 0.5rem;

      .nav-item {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.5rem 0.875rem;
        border-radius: 8px;
        color: #475569;
        text-decoration: none;
        font-weight: 500;
        font-size: 0.875rem;
        transition: all 0.2s ease;

        i {
          font-size: 0.95rem;
        }

        &:hover {
          background: #f1f5f9;
          color: #0f172a;
        }

        &.active-link {
          background: #eff6ff;
          color: #2563eb;
          font-weight: 600;
        }
      }
    }

    .nav-actions {
      display: flex;
      align-items: center;
      gap: 1.25rem;

      .cart-btn {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        padding: 0.45rem 0.9rem;
        border-radius: 8px;

        .cart-label {
          font-weight: 500;
          font-size: 0.875rem;
        }
      }

      .user-profile {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding-left: 1.25rem;
        border-left: 1px solid var(--surface-border);

        .user-info {
          display: flex;
          flex-direction: column;
          align-items: flex-end;
          gap: 0.15rem;

          .user-name {
            font-size: 0.875rem;
            font-weight: 600;
            color: #0f172a;
          }

          .user-meta {
            display: flex;
            align-items: center;
            gap: 0.4rem;

            .tenant-badge {
              font-size: 0.7rem;
              font-weight: 600;
              background: #f1f5f9;
              color: #475569;
              padding: 0.1rem 0.35rem;
              border-radius: 4px;
            }
          }
        }

        .logout-btn {
          width: 34px;
          height: 34px;
          border-radius: 8px;
        }
      }
    }
  `]
})
export class NavbarComponent {
  authService = inject(AuthService);
  cartService = inject(CartService);
  private router = inject(Router);

  getRoleSeverity(): 'danger' | 'info' | 'success' | 'warn' | 'secondary' {
    const role = this.authService.currentUser()?.role;
    if (role === 'Admin') return 'danger';
    if (role === 'SalesRep') return 'info';
    if (role === 'Dealer') return 'success';
    return 'secondary';
  }

  logout(): void {
    this.authService.logout();
  }
}
