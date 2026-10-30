import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { MessageService } from 'primeng/api';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, InputTextModule, PasswordModule],
  template: `
    <div class="login-wrapper">
      <div class="login-card">
        <div class="card-header">
          <div class="brand-avatar">
            <i class="pi pi-box"></i>
          </div>
          <h2>B2B Bayi Portalı</h2>
          <p>Lütfen kurumsal hesabınızla giriş yapınız</p>
        </div>

        <form (ngSubmit)="onSubmit()" class="login-form">
          <div class="form-field">
            <label for="email">E-Posta Adresi</label>
            <input
              pInputText
              id="email"
              type="email"
              [(ngModel)]="email"
              name="email"
              required
              placeholder="ornek@bayi.com"
              class="w-full"
            />
          </div>

          <div class="form-field">
            <label for="password">Şifre</label>
            <p-password
              id="password"
              [(ngModel)]="password"
              name="password"
              [feedback]="false"
              [toggleMask]="true"
              required
              placeholder="••••••••"
              styleClass="w-full"
              inputStyleClass="w-full"
            ></p-password>
          </div>

          <button
            pButton
            type="submit"
            label="Giriş Yap"
            icon="pi pi-sign-in"
            [loading]="isLoading()"
            class="p-button-primary w-full submit-btn"
          ></button>
        </form>

        <div class="quick-login-section">
          <div class="divider">
            <span>Hızlı Demo Girişi</span>
          </div>
          <div class="quick-buttons">
            <button
              pButton
              type="button"
              label="Bayi (Dealer)"
              icon="pi pi-user"
              class="p-button-outlined p-button-sm p-button-success"
              (click)="quickLogin('dealer@dealerportal.com', 'Dealer123!')"
            ></button>
            <button
              pButton
              type="button"
              label="Satış Temsilcisi"
              icon="pi pi-briefcase"
              class="p-button-outlined p-button-sm p-button-info"
              (click)="quickLogin('salesrep@dealerportal.com', 'SalesRep123!')"
            ></button>
            <button
              pButton
              type="button"
              label="Yönetici (Admin)"
              icon="pi pi-shield"
              class="p-button-outlined p-button-sm p-button-danger"
              (click)="quickLogin('admin@dealerportal.com', 'Admin123!')"
            ></button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .login-wrapper {
      min-height: calc(100vh - 70px);
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 2rem 1rem;
      background: linear-gradient(135deg, #f8fafc 0%, #e2e8f0 100%);
    }

    .login-card {
      background: #ffffff;
      border-radius: 16px;
      padding: 2.5rem;
      width: 100%;
      max-width: 440px;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.08), 0 8px 10px -6px rgba(0, 0, 0, 0.04);
      border: 1px solid #e2e8f0;

      .card-header {
        text-align: center;
        margin-bottom: 2rem;

        .brand-avatar {
          width: 56px;
          height: 56px;
          background: linear-gradient(135deg, #2563eb, #1d4ed8);
          border-radius: 14px;
          display: inline-flex;
          align-items: center;
          justify-content: center;
          color: #ffffff;
          font-size: 1.75rem;
          margin-bottom: 1rem;
          box-shadow: 0 4px 10px rgba(37, 99, 235, 0.3);
        }

        h2 {
          font-size: 1.5rem;
          font-weight: 700;
          color: #0f172a;
          margin-bottom: 0.25rem;
        }

        p {
          font-size: 0.875rem;
          color: #64748b;
        }
      }

      .login-form {
        display: flex;
        flex-direction: column;
        gap: 1.25rem;

        .form-field {
          display: flex;
          flex-direction: column;
          gap: 0.5rem;

          label {
            font-size: 0.875rem;
            font-weight: 600;
            color: #334155;
          }

          input, :host ::ng-deep .p-password {
            width: 100%;
          }
        }

        .submit-btn {
          margin-top: 0.5rem;
          padding: 0.75rem;
          font-weight: 600;
          border-radius: 8px;
        }
      }

      .quick-login-section {
        margin-top: 2rem;

        .divider {
          text-align: center;
          position: relative;
          margin-bottom: 1rem;

          &::before {
            content: '';
            position: absolute;
            top: 50%;
            left: 0;
            right: 0;
            height: 1px;
            background: #e2e8f0;
          }

          span {
            position: relative;
            background: #ffffff;
            padding: 0 0.75rem;
            font-size: 0.75rem;
            color: #94a3b8;
            font-weight: 500;
            text-transform: uppercase;
            letter-spacing: 0.05em;
          }
        }

        .quick-buttons {
          display: flex;
          flex-direction: column;
          gap: 0.5rem;

          button {
            justify-content: center;
            font-size: 0.8rem;
            border-radius: 6px;
          }
        }
      }
    }

    .w-full {
      width: 100%;
    }
  `]
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private messageService = inject(MessageService);

  email = '';
  password = '';
  isLoading = signal(false);

  quickLogin(email: string, pass: string): void {
    this.email = email;
    this.password = pass;
    this.onSubmit();
  }

  onSubmit(): void {
    if (!this.email || !this.password) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Eksik Bilgi',
        detail: 'Lütfen e-posta ve şifrenizi giriniz.'
      });
      return;
    }

    this.isLoading.set(true);
    this.authService.login(this.email, this.password).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        this.messageService.add({
          severity: 'success',
          summary: 'Hoş Geldiniz',
          detail: `Giriş başarılı: ${res.user.fullName}`
        });

        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        if (returnUrl) {
          this.router.navigateByUrl(returnUrl);
        } else if (res.user.role === 'Dealer') {
          this.router.navigate(['/catalog']);
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.isLoading.set(false);
        const errorMsg = err?.error?.detail || 'Giriş yapılamadı. Lütfen bilgilerinizi kontrol ediniz.';
        this.messageService.add({
          severity: 'error',
          summary: 'Giriş Hatası',
          detail: errorMsg
        });
      }
    });
  }
}
