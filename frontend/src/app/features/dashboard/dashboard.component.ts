import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { OrderService } from '../../core/services/order.service';
import { environment } from '../../../environments/environment';
import { DealerInsight, PagedResult, Dealer } from '../../core/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonModule, TagModule, TableModule],
  template: `
    <div class="portal-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="pi pi-chart-bar"></i>
            Yönetici & Analitik Paneli
          </h1>
          <p class="page-subtitle">Portal genel performans göstergeleri ve yapay zeka destekli bayi içgörüleri</p>
        </div>

        <button
          pButton
          type="button"
          label="Metrikleri Yenile"
          icon="pi pi-refresh"
          class="p-button-outlined p-button-secondary"
          (click)="loadAllData()"
        ></button>
      </div>

      <!-- KPI Cards -->
      <div class="kpi-grid">
        <div class="card kpi-card">
          <div class="kpi-header">
            <span class="kpi-title">Toplam Ciro</span>
            <div class="kpi-icon blue">
              <i class="pi pi-wallet"></i>
            </div>
          </div>
          <div class="kpi-value">{{ (metrics()?.totalRevenue || 0) | number:'1.2-2' }} ₺</div>
          <div class="kpi-subtext">Faturalandırılmış net sipariş cirosu</div>
        </div>

        <div class="card kpi-card">
          <div class="kpi-header">
            <span class="kpi-title">Toplam Sipariş</span>
            <div class="kpi-icon indigo">
              <i class="pi pi-shopping-bag"></i>
            </div>
          </div>
          <div class="kpi-value">{{ metrics()?.totalOrders || 0 }}</div>
          <div class="kpi-subtext">Sistemdeki tüm sipariş adedi</div>
        </div>

        <div class="card kpi-card">
          <div class="kpi-header">
            <span class="kpi-title">Onay Bekleyenler</span>
            <div class="kpi-icon amber">
              <i class="pi pi-clock"></i>
            </div>
          </div>
          <div class="kpi-value pending-val">{{ metrics()?.pendingApproval || 0 }}</div>
          <div class="kpi-subtext">İnceleme bekleyen siparişler</div>
        </div>

        <div class="card kpi-card">
          <div class="kpi-header">
            <span class="kpi-title">Aktif Bayiler</span>
            <div class="kpi-icon emerald">
              <i class="pi pi-building"></i>
            </div>
          </div>
          <div class="kpi-value">{{ metrics()?.activeDealers || 0 }}</div>
          <div class="kpi-subtext">Kayıtlı ve aktif bayi sayısı</div>
        </div>
      </div>

      <!-- Content Grid: Top Products + Dealer Insight -->
      <div class="dashboard-content-grid mt-4">
        <!-- Top Products -->
        <div class="card">
          <div class="section-header">
            <h3 class="section-title">
              <i class="pi pi-star"></i>
              En Çok Satan Ürünler
            </h3>
          </div>

          <p-table [value]="topProducts()" responsiveLayout="stack" styleClass="p-datatable-striped">
            <ng-template pTemplate="header">
              <tr>
                <th style="width: 120px">SKU</th>
                <th>Ürün</th>
                <th style="width: 90px; text-align: center">Adet</th>
                <th style="width: 140px; text-align: right">Ciro</th>
              </tr>
            </ng-template>

            <ng-template pTemplate="body" let-prod>
              <tr>
                <td>
                  <span class="sku-badge">{{ prod.sku }}</span>
                </td>
                <td>
                  <span class="prod-name">{{ prod.productName }}</span>
                </td>
                <td style="text-align: center">
                  <span class="qty-pill">{{ prod.totalQuantity }}</span>
                </td>
                <td style="text-align: right">
                  <span class="revenue-val">{{ prod.totalGross | number:'1.2-2' }} ₺</span>
                </td>
              </tr>
            </ng-template>

            <ng-template pTemplate="emptymessage">
              <tr>
                <td colspan="4" class="text-center p-3 text-muted">Henüz faturalandırılmış satış kaydı bulunmamaktadır.</td>
              </tr>
            </ng-template>
          </p-table>
        </div>

        <!-- AI Dealer Insight Section -->
        <div class="card ai-card">
          <div class="section-header ai-header">
            <div>
              <h3 class="section-title ai-title">
                <i class="pi pi-sparkles"></i>
                Bayi Sipariş Analitiği & İçgörü
              </h3>
              <p class="ai-desc">Gelişmiş analitik algoritması ile bayi bazlı sipariş davranış raporu</p>
            </div>

            @if (dealers().length > 0) {
              <div class="dealer-selector">
                <select [(ngModel)]="selectedDealerId" (change)="onDealerSelect()" class="dealer-dropdown">
                  @for (dealer of dealers(); track dealer.id) {
                    <option [value]="dealer.id">{{ dealer.name }} ({{ dealer.city }})</option>
                  }
                </select>
              </div>
            }
          </div>

          @if (insightLoading()) {
            <div class="ai-loading">
              <i class="pi pi-spin pi-spinner" style="font-size: 2rem; color: #2563eb"></i>
              <p class="mt-2 text-muted">Bayi analitik verileri işleniyor...</p>
            </div>
          } @else {
            @if (currentInsight(); as ins) {
              <div class="insight-content">
                <div class="insight-stats">
                  <div class="stat-box">
                    <span class="stat-label">90 Günlük Ciro</span>
                    <span class="stat-val">{{ ins.totalRevenueLast90Days | number:'1.2-2' }} ₺</span>
                  </div>
                  <div class="stat-box">
                    <span class="stat-label">Sipariş Sayısı</span>
                    <span class="stat-val">{{ ins.totalOrdersLast90Days }} adet</span>
                  </div>
                  <div class="stat-box">
                    <span class="stat-label">Sipariş Trendi</span>
                    <p-tag [value]="ins.frequencyTrend" severity="info"></p-tag>
                  </div>
                </div>

                <div class="insight-section mt-3">
                  <h4 class="sub-heading">Yönetici Özeti</h4>
                  <div class="summary-box">
                    {{ ins.executiveSummary }}
                  </div>
                </div>

                @if (ins.topCategories && ins.topCategories.length > 0) {
                  <div class="insight-section mt-3">
                    <h4 class="sub-heading">Ağırlıklı Kategoriler</h4>
                    <div class="category-chips">
                      @for (cat of ins.topCategories; track cat.categoryName) {
                        <div class="cat-chip">
                          <span class="cat-name">{{ cat.categoryName }}</span>
                          <span class="cat-spent">{{ cat.totalSpent | number:'1.0-0' }} ₺ ({{ cat.orderCount }} sipariş)</span>
                        </div>
                      }
                    </div>
                  </div>
                }

                @if (ins.recommendedActions && ins.recommendedActions.length > 0) {
                  <div class="insight-section mt-3">
                    <h4 class="sub-heading">Önerilen Aksiyonlar</h4>
                    <ul class="action-list">
                      @for (act of ins.recommendedActions; track act) {
                        <li class="action-item">
                          <i class="pi pi-check-circle action-icon"></i>
                          <span>{{ act }}</span>
                        </li>
                      }
                    </ul>
                  </div>
                }
              </div>
            } @else {
              <div class="text-center p-4 text-muted">
                Analitik verisi görüntülemek için yukarıdan bir bayi seçiniz.
              </div>
            }
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    .kpi-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
      gap: 1.25rem;
    }

    .kpi-card {
      padding: 1.25rem;
      border-radius: 12px;

      .kpi-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 0.75rem;

        .kpi-title {
          font-size: 0.875rem;
          font-weight: 600;
          color: #64748b;
        }

        .kpi-icon {
          width: 36px;
          height: 36px;
          border-radius: 8px;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 1.1rem;

          &.blue { background: #eff6ff; color: #2563eb; }
          &.indigo { background: #eef2ff; color: #4f46e5; }
          &.amber { background: #fffbeb; color: #d97706; }
          &.emerald { background: #ecfdf5; color: #059669; }
        }
      }

      .kpi-value {
        font-size: 1.75rem;
        font-weight: 700;
        color: #0f172a;
        line-height: 1.2;

        &.pending-val {
          color: #d97706;
        }
      }

      .kpi-subtext {
        font-size: 0.75rem;
        color: #94a3b8;
        margin-top: 0.35rem;
      }
    }

    .dashboard-content-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;

      @media (max-width: 1024px) {
        grid-template-columns: 1fr;
      }
    }

    .section-header {
      margin-bottom: 1.25rem;

      .section-title {
        font-size: 1.1rem;
        font-weight: 700;
        color: #0f172a;
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }
    }

    .ai-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      flex-wrap: wrap;
      gap: 1rem;

      .ai-title {
        color: #1e1b4b;
      }

      .ai-desc {
        font-size: 0.8rem;
        color: #64748b;
        margin-top: 0.2rem;
      }

      .dealer-dropdown {
        padding: 0.45rem 0.75rem;
        border: 1px solid #cbd5e1;
        border-radius: 8px;
        font-size: 0.85rem;
        font-weight: 500;
        background: #ffffff;
        color: #1e293b;
        outline: none;

        &:focus {
          border-color: #2563eb;
        }
      }
    }

    .sku-badge {
      font-family: monospace;
      font-size: 0.8rem;
      font-weight: 600;
      color: #3b82f6;
      background: #eff6ff;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
    }

    .prod-name {
      font-weight: 600;
      color: #1e293b;
      font-size: 0.875rem;
    }

    .qty-pill {
      background: #f1f5f9;
      font-weight: 700;
      padding: 0.2rem 0.5rem;
      border-radius: 6px;
      font-size: 0.85rem;
    }

    .revenue-val {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.875rem;
    }

    .ai-loading {
      padding: 3rem;
      text-align: center;
    }

    .insight-content {
      .insight-stats {
        display: grid;
        grid-template-columns: repeat(3, 1fr);
        gap: 0.75rem;

        .stat-box {
          background: #f8fafc;
          border: 1px solid #e2e8f0;
          border-radius: 8px;
          padding: 0.75rem;
          display: flex;
          flex-direction: column;
          gap: 0.25rem;

          .stat-label {
            font-size: 0.75rem;
            color: #64748b;
          }

          .stat-val {
            font-size: 1rem;
            font-weight: 700;
            color: #0f172a;
          }
        }
      }

      .sub-heading {
        font-size: 0.875rem;
        font-weight: 600;
        color: #334155;
        margin-bottom: 0.5rem;
      }

      .summary-box {
        background: #f0fdf4;
        border-left: 3px solid #22c55e;
        padding: 0.75rem 1rem;
        border-radius: 0 8px 8px 0;
        font-size: 0.875rem;
        line-height: 1.5;
        color: #14532d;
      }

      .category-chips {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;

        .cat-chip {
          background: #f1f5f9;
          border: 1px solid #e2e8f0;
          padding: 0.35rem 0.7rem;
          border-radius: 6px;
          font-size: 0.8rem;
          display: flex;
          gap: 0.4rem;

          .cat-name {
            font-weight: 600;
            color: #1e293b;
          }

          .cat-spent {
            color: #64748b;
          }
        }
      }

      .action-list {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;

        .action-item {
          display: flex;
          align-items: flex-start;
          gap: 0.5rem;
          font-size: 0.85rem;
          color: #334155;
          line-height: 1.4;

          .action-icon {
            color: #2563eb;
            margin-top: 0.2rem;
            font-size: 0.9rem;
          }
        }
      }
    }

    .mt-3 { margin-top: 1rem; }
    .mt-4 { margin-top: 1.5rem; }
    .mt-2 { margin-top: 0.5rem; }
    .text-center { text-align: center; }
    .text-muted { color: #64748b; }
    .p-3 { padding: 1rem; }
    .p-4 { padding: 1.5rem; }
  `]
})
export class DashboardComponent implements OnInit {
  private orderService = inject(OrderService);
  private http = inject(HttpClient);
  private messageService = inject(MessageService);

  metrics = signal<any>(null);
  topProducts = signal<any[]>([]);
  dealers = signal<Dealer[]>([]);
  selectedDealerId = '';
  currentInsight = signal<DealerInsight | null>(null);
  insightLoading = signal(false);

  ngOnInit(): void {
    this.loadAllData();
  }

  loadAllData(): void {
    this.orderService.getDashboardMetrics().subscribe({
      next: (data) => this.metrics.set(data),
      error: () => {}
    });

    this.orderService.getTopProducts().subscribe({
      next: (data) => this.topProducts.set(data),
      error: () => {}
    });

    this.http.get<PagedResult<Dealer>>(`${environment.apiUrl}/dealers`).subscribe({
      next: (res) => {
        this.dealers.set(res.items);
        if (res.items.length > 0 && !this.selectedDealerId) {
          this.selectedDealerId = res.items[0].id;
          this.loadDealerInsight(this.selectedDealerId);
        }
      },
      error: () => {}
    });
  }

  onDealerSelect(): void {
    if (this.selectedDealerId) {
      this.loadDealerInsight(this.selectedDealerId);
    }
  }

  private loadDealerInsight(dealerId: string): void {
    this.insightLoading.set(true);
    this.orderService.getDealerInsight(dealerId).subscribe({
      next: (ins) => {
        this.currentInsight.set(ins);
        this.insightLoading.set(false);
      },
      error: () => {
        this.insightLoading.set(false);
      }
    });
  }
}
