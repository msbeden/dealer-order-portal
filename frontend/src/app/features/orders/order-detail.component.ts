import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { Textarea } from 'primeng/inputtextarea';
import { MessageService } from 'primeng/api';
import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';
import { Order, OrderStatus } from '../../core/models/models';

@Component({
  selector: 'app-order-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, TableModule, ButtonModule, TagModule, DialogModule, Textarea],
  template: `
    <div class="portal-container">
      <div class="page-header">
        <div>
          <div class="back-link" routerLink="/orders">
            <i class="pi pi-arrow-left"></i>
            <span>Sipariş Listesine Dön</span>
          </div>
          <h1 class="page-title mt-2">
            Sipariş Detayı: <span class="order-no">{{ order()?.orderNo }}</span>
          </h1>
        </div>

        <div class="header-actions">
          @if (canApproveOrReject()) {
            <button
              pButton
              type="button"
              label="Siparişi Reddet"
              icon="pi pi-times"
              class="p-button-outlined p-button-danger"
              (click)="openRejectDialog()"
            ></button>
            <button
              pButton
              type="button"
              label="Siparişi Onayla & Faturalandır"
              icon="pi pi-check"
              class="p-button-success"
              [loading]="actionLoading()"
              (click)="approveOrder()"
            ></button>
          }
        </div>
      </div>

      @if (loading()) {
        <div class="card p-5 text-center">
          <i class="pi pi-spin pi-spinner" style="font-size: 2rem; color: #3b82f6"></i>
          <p class="mt-2 text-muted">Sipariş detayları yükleniyor...</p>
        </div>
      } @else {
        @if (order(); as o) {
          @if (o.status === OrderStatus.Rejected) {
            <div class="alert-box alert-danger">
              <i class="pi pi-exclamation-triangle"></i>
              <div>
                <strong>Bu sipariş reddedilmiştir.</strong>
                <p>Gerekçe: {{ o.rejectionReason || 'Gerekçe belirtilmedi.' }}</p>
              </div>
            </div>
          }

          @if (o.status === OrderStatus.Invoiced || o.status === OrderStatus.Approved) {
            <div class="alert-box alert-success">
              <i class="pi pi-check-circle"></i>
              <div>
                <strong>Bu sipariş onaylanmış ve faturalandırılmıştır.</strong>
                <p>Onaylayan: {{ o.approvedBy }} | Tarih: {{ o.approvedAtUtc | date:'dd.MM.yyyy HH:mm' }}</p>
              </div>
            </div>
          }

          <div class="detail-grid">
            <div class="card">
              <h3 class="section-title">Sipariş Kalemleri</h3>
              <p-table [value]="o.lines" responsiveLayout="stack" styleClass="p-datatable-striped">
                <ng-template pTemplate="header">
                  <tr>
                    <th style="width: 140px">SKU</th>
                    <th>Ürün Adı</th>
                    <th style="width: 100px; text-align: center">Adet</th>
                    <th style="width: 140px; text-align: right">Birim Fiyat</th>
                    <th style="width: 90px; text-align: center">KDV</th>
                    <th style="width: 150px; text-align: right">Toplam</th>
                  </tr>
                </ng-template>

                <ng-template pTemplate="body" let-line>
                  <tr>
                    <td>
                      <span class="sku-chip">{{ line.sku }}</span>
                    </td>
                    <td>
                      <span class="product-title">{{ line.productName }}</span>
                    </td>
                    <td style="text-align: center">
                      <span class="qty-badge">{{ line.quantity }}</span>
                    </td>
                    <td style="text-align: right">
                      <span>{{ line.unitPrice | number:'1.2-2' }} ₺</span>
                    </td>
                    <td style="text-align: center">
                      <span class="vat-chip">%{{ line.vatRate }}</span>
                    </td>
                    <td style="text-align: right">
                      <span class="line-gross">{{ line.lineTotalGross | number:'1.2-2' }} ₺</span>
                    </td>
                  </tr>
                </ng-template>
              </p-table>
            </div>

            <div class="side-panel">
              <div class="card meta-card">
                <h3 class="section-title">Genel Bilgiler</h3>
                <div class="meta-row">
                  <span class="meta-label">Sipariş Durumu</span>
                  <p-tag [value]="getStatusLabel(o.status)" [severity]="getStatusSeverity(o.status)"></p-tag>
                </div>
                <div class="meta-row">
                  <span class="meta-label">Bayi</span>
                  <span class="meta-val">{{ o.dealerName || 'Bilinmeyen Bayi' }}</span>
                </div>
                <div class="meta-row">
                  <span class="meta-label">Oluşturulma</span>
                  <span class="meta-val">{{ o.createdAtUtc | date:'dd.MM.yyyy HH:mm' }}</span>
                </div>
                <div class="meta-row">
                  <span class="meta-label">Kiracı (Tenant)</span>
                  <span class="meta-val">{{ o.tenantId }}</span>
                </div>
              </div>

              <div class="card financial-card mt-3">
                <h3 class="section-title">Finansal Toplam</h3>
                <div class="financial-line">
                  <span>Ara Toplam (Net)</span>
                  <span>{{ o.totalNet | number:'1.2-2' }} ₺</span>
                </div>
                <div class="financial-line">
                  <span>Hesaplanan KDV</span>
                  <span>{{ o.totalVat | number:'1.2-2' }} ₺</span>
                </div>
                <div class="divider"></div>
                <div class="financial-line grand-line">
                  <span>Genel Toplam (Brüt)</span>
                  <span>{{ o.totalGross | number:'1.2-2' }} ₺</span>
                </div>
              </div>
            </div>
          </div>
        }
      }
    </div>

    <!-- Rejection Dialog -->
    <p-dialog
      header="Siparişi Reddet"
      [(visible)]="rejectDialogVisible"
      [modal]="true"
      [style]="{ width: '480px' }"
    >
      <div class="dialog-body">
        <p class="dialog-desc">Lütfen siparişi reddetme gerekçenizi belirtiniz:</p>
        <textarea
          pInputTextarea
          [(ngModel)]="rejectionReason"
          rows="4"
          class="w-full reject-textarea"
          placeholder="Örn: Stok yetersizliği veya kredi limiti aşımı..."
        ></textarea>
      </div>
      <ng-template pTemplate="footer">
        <button
          pButton
          label="Vazgeç"
          icon="pi pi-times"
          class="p-button-text p-button-secondary"
          (click)="rejectDialogVisible = false"
        ></button>
        <button
          pButton
          label="Reddi Onayla"
          icon="pi pi-check"
          class="p-button-danger"
          [loading]="actionLoading()"
          (click)="confirmReject()"
        ></button>
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .back-link {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      color: #64748b;
      font-size: 0.85rem;
      cursor: pointer;
      text-decoration: none;
      font-weight: 500;

      &:hover {
        color: #2563eb;
      }
    }

    .order-no {
      color: #2563eb;
    }

    .header-actions {
      display: flex;
      gap: 0.75rem;
      align-items: center;
    }

    .alert-box {
      display: flex;
      gap: 1rem;
      padding: 1rem 1.25rem;
      border-radius: 10px;
      margin-bottom: 1.5rem;
      align-items: flex-start;

      i {
        font-size: 1.5rem;
        margin-top: 0.15rem;
      }

      p {
        margin-top: 0.25rem;
        font-size: 0.9rem;
      }

      &.alert-danger {
        background: #fef2f2;
        border: 1px solid #fecaca;
        color: #991b1b;
      }

      &.alert-success {
        background: #f0fdf4;
        border: 1px solid #bbf7d0;
        color: #166534;
      }
    }

    .detail-grid {
      display: grid;
      grid-template-columns: 1fr 340px;
      gap: 1.5rem;
      align-items: start;

      @media (max-width: 960px) {
        grid-template-columns: 1fr;
      }
    }

    .section-title {
      font-size: 1.1rem;
      font-weight: 700;
      color: #0f172a;
      margin-bottom: 1.25rem;
    }

    .sku-chip {
      font-family: monospace;
      font-size: 0.8rem;
      font-weight: 600;
      color: #3b82f6;
      background: #eff6ff;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
    }

    .product-title {
      font-weight: 600;
      color: #1e293b;
    }

    .qty-badge {
      font-weight: 700;
      background: #f1f5f9;
      padding: 0.2rem 0.6rem;
      border-radius: 6px;
    }

    .vat-chip {
      font-size: 0.75rem;
      color: #64748b;
    }

    .line-gross {
      font-weight: 700;
      color: #0f172a;
    }

    .meta-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0.6rem 0;
      border-bottom: 1px solid #f1f5f9;

      &:last-child {
        border-bottom: none;
      }

      .meta-label {
        font-size: 0.85rem;
        color: #64748b;
      }

      .meta-val {
        font-size: 0.875rem;
        font-weight: 600;
        color: #1e293b;
      }
    }

    .financial-card {
      .financial-line {
        display: flex;
        justify-content: space-between;
        padding: 0.4rem 0;
        font-size: 0.9rem;
        color: #475569;

        &.grand-line {
          font-size: 1.15rem;
          font-weight: 700;
          color: #0f172a;
        }
      }

      .divider {
        height: 1px;
        background: #e2e8f0;
        margin: 0.5rem 0;
      }
    }

    .dialog-body {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;

      .dialog-desc {
        font-size: 0.875rem;
        color: #475569;
      }

      .reject-textarea {
        width: 100%;
        border-radius: 8px;
        padding: 0.75rem;
      }
    }

    .mt-2 { margin-top: 0.5rem; }
    .mt-3 { margin-top: 1rem; }
    .w-full { width: 100%; }
    .text-center { text-align: center; }
    .text-muted { color: #64748b; }
  `]
})
export class OrderDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  private messageService = inject(MessageService);

  order = signal<Order | null>(null);
  loading = signal(true);
  actionLoading = signal(false);

  rejectDialogVisible = false;
  rejectionReason = '';

  readonly OrderStatus = OrderStatus;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadOrderDetail(id);
    }
  }

  loadOrderDetail(id: string): void {
    this.loading.set(true);
    this.orderService.getOrderById(id).subscribe({
      next: (data) => {
        this.order.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Hata',
          detail: 'Sipariş detayları alınamadı.'
        });
      }
    });
  }

  canApproveOrReject(): boolean {
    const o = this.order();
    if (!o) return false;
    const isSubmitted = o.status === OrderStatus.Submitted;
    const isAuthorized = this.authService.isAdmin() || this.authService.isSalesRep();
    return isSubmitted && isAuthorized;
  }

  approveOrder(): void {
    const o = this.order();
    if (!o) return;

    this.actionLoading.set(true);
    this.orderService.approveOrder(o.id).subscribe({
      next: (updated) => {
        this.order.set(updated);
        this.actionLoading.set(false);
        this.messageService.add({
          severity: 'success',
          summary: 'Sipariş Onaylandı',
          detail: 'Sipariş onaylandı, fatura kesildi ve stoklar düşüldü.'
        });
      },
      error: (err) => {
        this.actionLoading.set(false);
        const detail = err?.error?.detail || 'Sipariş onaylanırken bir hata oluştu.';
        this.messageService.add({
          severity: 'error',
          summary: 'Onay Hatası',
          detail: detail
        });
      }
    });
  }

  openRejectDialog(): void {
    this.rejectionReason = '';
    this.rejectDialogVisible = true;
  }

  confirmReject(): void {
    const o = this.order();
    if (!o || !this.rejectionReason.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Gerekçe Eksik',
        detail: 'Lütfen bir ret gerekçesi giriniz.'
      });
      return;
    }

    this.actionLoading.set(true);
    this.orderService.rejectOrder(o.id, this.rejectionReason).subscribe({
      next: (updated) => {
        this.order.set(updated);
        this.actionLoading.set(false);
        this.rejectDialogVisible = false;
        this.messageService.add({
          severity: 'info',
          summary: 'Sipariş Reddedildi',
          detail: 'Sipariş başarıyla reddedildi.'
        });
      },
      error: (err) => {
        this.actionLoading.set(false);
        const detail = err?.error?.detail || 'Sipariş reddedilirken bir hata oluştu.';
        this.messageService.add({
          severity: 'error',
          summary: 'Hata',
          detail: detail
        });
      }
    });
  }

  getStatusLabel(status: OrderStatus): string {
    switch (status) {
      case OrderStatus.Draft: return 'Taslak';
      case OrderStatus.Submitted: return 'Onay Bekliyor';
      case OrderStatus.Approved: return 'Onaylandı';
      case OrderStatus.Invoiced: return 'Faturalandı';
      case OrderStatus.Rejected: return 'Reddedildi';
      case OrderStatus.Cancelled: return 'İptal Edildi';
      default: return 'Bilinmiyor';
    }
  }

  getStatusSeverity(status: OrderStatus): 'secondary' | 'warn' | 'info' | 'success' | 'danger' {
    switch (status) {
      case OrderStatus.Draft: return 'secondary';
      case OrderStatus.Submitted: return 'warn';
      case OrderStatus.Approved: return 'info';
      case OrderStatus.Invoiced: return 'success';
      case OrderStatus.Rejected: return 'danger';
      case OrderStatus.Cancelled: return 'secondary';
      default: return 'secondary';
    }
  }
}
