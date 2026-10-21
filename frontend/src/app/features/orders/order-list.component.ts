import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { SelectButtonModule } from 'primeng/selectbutton';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';
import { Order, OrderStatus } from '../../core/models/models';

@Component({
  selector: 'app-order-list',
  standalone: true,
  imports: [CommonModule, RouterModule, TableModule, ButtonModule, TagModule, SelectButtonModule, FormsModule],
  template: `
    <div class="portal-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="pi pi-list"></i>
            Sipariş Yönetimi
          </h1>
          <p class="page-subtitle">Verilen tüm bayi siparişlerinin durumunu ve geçmişini takip ediniz</p>
        </div>

        <button
          pButton
          type="button"
          label="Yenile"
          icon="pi pi-refresh"
          class="p-button-outlined p-button-secondary"
          (click)="loadOrders()"
        ></button>
      </div>

      <div class="card order-card">
        <div class="filters-bar">
          <div class="status-tabs">
            <button
              class="filter-pill"
              [class.active]="selectedStatus === undefined"
              (click)="filterByStatus(undefined)"
            >
              Tümü
            </button>
            <button
              class="filter-pill"
              [class.active]="selectedStatus === 10"
              (click)="filterByStatus(10)"
            >
              Onay Bekleyenler
            </button>
            <button
              class="filter-pill"
              [class.active]="selectedStatus === 30"
              (click)="filterByStatus(30)"
            >
              Faturalananlar
            </button>
            <button
              class="filter-pill"
              [class.active]="selectedStatus === 40"
              (click)="filterByStatus(40)"
            >
              Reddedilenler
            </button>
          </div>
        </div>

        <p-table
          [value]="orders()"
          [loading]="loading()"
          [paginator]="true"
          [rows]="15"
          responsiveLayout="stack"
          styleClass="p-datatable-striped"
        >
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 170px">Sipariş No</th>
              <th style="width: 160px">Tarih</th>
              @if (!authService.isDealer()) {
                <th>Bayi Adı</th>
              }
              <th style="width: 100px; text-align: center">Kalem</th>
              <th style="width: 140px; text-align: right">Genel Toplam</th>
              <th style="width: 150px; text-align: center">Durum</th>
              <th style="width: 110px; text-align: center">İşlem</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-order>
            <tr>
              <td>
                <span class="order-no-chip">{{ order.orderNo }}</span>
              </td>
              <td>
                <span class="date-text">{{ order.createdAtUtc | date:'dd.MM.yyyy HH:mm' }}</span>
              </td>
              @if (!authService.isDealer()) {
                <td>
                  <span class="dealer-name">{{ order.dealerName || 'Bilinmeyen Bayi' }}</span>
                </td>
              }
              <td style="text-align: center">
                <span class="line-count">{{ order.lines?.length || 0 }} adet</span>
              </td>
              <td style="text-align: right">
                <span class="total-amount">{{ order.totalGross | number:'1.2-2' }} ₺</span>
              </td>
              <td style="text-align: center">
                <p-tag
                  [value]="getStatusLabel(order.status)"
                  [severity]="getStatusSeverity(order.status)"
                ></p-tag>
              </td>
              <td style="text-align: center">
                <a
                  [routerLink]="['/orders', order.id]"
                  pButton
                  icon="pi pi-eye"
                  label="İncele"
                  class="p-button-sm p-button-outlined"
                ></a>
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td [attr.colspan]="authService.isDealer() ? 6 : 7" class="text-center p-4">
                <div class="empty-orders">
                  <i class="pi pi-inbox empty-icon"></i>
                  <p>Kriterlere uygun herhangi bir sipariş kaydı bulunamadı.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>
  `,
  styles: [`
    .order-card {
      padding: 1.25rem;
    }

    .filters-bar {
      margin-bottom: 1.25rem;

      .status-tabs {
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;

        .filter-pill {
          background: #f1f5f9;
          border: 1px solid #e2e8f0;
          color: #475569;
          font-weight: 500;
          font-size: 0.85rem;
          padding: 0.45rem 1rem;
          border-radius: 9999px;
          cursor: pointer;
          transition: all 0.2s ease;

          &:hover {
            background: #e2e8f0;
            color: #0f172a;
          }

          &.active {
            background: #2563eb;
            color: #ffffff;
            border-color: #2563eb;
            box-shadow: 0 2px 4px rgba(37, 99, 235, 0.25);
          }
        }
      }
    }

    .order-no-chip {
      font-family: monospace;
      font-weight: 700;
      color: #0f172a;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      padding: 0.25rem 0.5rem;
      border-radius: 4px;
      font-size: 0.85rem;
    }

    .date-text {
      color: #64748b;
      font-size: 0.85rem;
    }

    .dealer-name {
      font-weight: 600;
      color: #1e293b;
    }

    .line-count {
      color: #64748b;
      font-size: 0.85rem;
    }

    .total-amount {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.95rem;
    }

    .empty-orders {
      padding: 3rem 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      color: #94a3b8;

      .empty-icon {
        font-size: 2.5rem;
      }
    }
  `]
})
export class OrderListComponent implements OnInit {
  private orderService = inject(OrderService);
  private messageService = inject(MessageService);
  authService = inject(AuthService);

  orders = signal<Order[]>([]);
  loading = signal(false);
  selectedStatus?: number;

  ngOnInit(): void {
    this.loadOrders();
  }

  loadOrders(): void {
    this.loading.set(true);
    this.orderService.getOrders(1, 50, this.selectedStatus).subscribe({
      next: (res) => {
        this.orders.set(res.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Hata',
          detail: 'Siparişler listelenirken bir sorun oluştu.'
        });
      }
    });
  }

  filterByStatus(status?: number): void {
    this.selectedStatus = status;
    this.loadOrders();
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
