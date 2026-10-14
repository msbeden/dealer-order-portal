import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { AuthService } from '../../core/services/auth.service';
import { Product } from '../../core/models/models';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, FormsModule, TableModule, ButtonModule, InputTextModule, TagModule],
  template: `
    <div class="portal-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="pi pi-th-large"></i>
            Ürün Kataloğu
          </h1>
          <p class="page-subtitle">Sipariş vermek istediğiniz ürünleri seçip sepetinize ekleyiniz</p>
        </div>
      </div>

      <div class="card catalog-card">
        <div class="table-toolbar">
          <div class="search-box">
            <span class="p-input-icon-left w-full">
              <input
                pInputText
                type="text"
                [(ngModel)]="searchQuery"
                (input)="onSearchChange()"
                placeholder="SKU veya Ürün adı ile ara..."
                class="search-input"
              />
            </span>
          </div>

          <div class="filter-actions">
            <button
              pButton
              type="button"
              label="Yenile"
              icon="pi pi-refresh"
              class="p-button-outlined p-button-secondary"
              (click)="loadProducts()"
            ></button>
          </div>
        </div>

        <p-table
          [value]="products()"
          [lazy]="true"
          (onLazyLoad)="onLazyLoad($event)"
          [paginator]="true"
          [rows]="pageSize"
          [totalRecords]="totalRecords()"
          [loading]="loading()"
          [showCurrentPageReport]="true"
          currentPageReportTemplate="{totalRecords} üründen {first} - {last} arası gösteriliyor"
          [rowsPerPageOptions]="[10, 20, 50]"
          responsiveLayout="stack"
          styleClass="p-datatable-striped"
        >
          <ng-template pTemplate="header">
            <tr>
              <th style="width: 140px">SKU</th>
              <th>Ürün Adı</th>
              <th style="width: 160px">Kategori</th>
              <th style="width: 140px; text-align: right">Birim Fiyat</th>
              <th style="width: 90px; text-align: center">KDV</th>
              <th style="width: 140px; text-align: center">Stok Durumu</th>
              <th style="width: 220px; text-align: center">İşlem</th>
            </tr>
          </ng-template>

          <ng-template pTemplate="body" let-product>
            <tr>
              <td>
                <span class="sku-chip">{{ product.sku }}</span>
              </td>
              <td>
                <div class="product-title">{{ product.name }}</div>
              </td>
              <td>
                <span class="category-name">{{ product.categoryName || 'Genel' }}</span>
              </td>
              <td style="text-align: right">
                <span class="price-text">{{ product.unitPrice | number:'1.2-2' }} ₺</span>
              </td>
              <td style="text-align: center">
                <span class="vat-tag">%{{ product.vatRate }}</span>
              </td>
              <td style="text-align: center">
                <p-tag
                  [value]="getStockLabel(product.stockQuantity)"
                  [severity]="getStockSeverity(product.stockQuantity)"
                ></p-tag>
              </td>
              <td style="text-align: center">
                @if (product.stockQuantity > 0) {
                  <div class="action-cell">
                    <input
                      type="number"
                      [min]="1"
                      [max]="product.stockQuantity"
                      [(ngModel)]="quantities[product.id]"
                      class="qty-input"
                    />
                    <button
                      pButton
                      type="button"
                      icon="pi pi-cart-plus"
                      label="Sepete Ekle"
                      class="p-button-sm p-button-primary"
                      (click)="addToCart(product)"
                    ></button>
                  </div>
                } @else {
                  <span class="out-of-stock-text">Tükendi</span>
                }
              </td>
            </tr>
          </ng-template>

          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="7" class="text-center p-4">
                <div class="empty-state">
                  <i class="pi pi-inbox empty-icon"></i>
                  <p>Aradığınız kriterlere uygun ürün bulunamadı.</p>
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </div>
    </div>
  `,
  styles: [`
    .catalog-card {
      padding: 1.25rem;
    }

    .table-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1.25rem;
      gap: 1rem;
      flex-wrap: wrap;

      .search-box {
        flex: 1;
        min-width: 260px;
        max-width: 480px;

        .search-input {
          width: 100%;
          border-radius: 8px;
        }
      }
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

    .category-name {
      color: #64748b;
      font-size: 0.875rem;
    }

    .price-text {
      font-weight: 700;
      color: #0f172a;
      font-size: 0.95rem;
    }

    .vat-tag {
      font-size: 0.75rem;
      font-weight: 600;
      color: #64748b;
      background: #f1f5f9;
      padding: 0.15rem 0.4rem;
      border-radius: 4px;
    }

    .action-cell {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;

      .qty-input {
        width: 60px;
        height: 34px;
        text-align: center;
        border: 1px solid #cbd5e1;
        border-radius: 6px;
        font-weight: 600;

        &:focus {
          outline: none;
          border-color: #3b82f6;
        }
      }
    }

    .out-of-stock-text {
      color: #ef4444;
      font-weight: 600;
      font-size: 0.85rem;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.75rem;
      color: #94a3b8;
      padding: 2rem 0;

      .empty-icon {
        font-size: 2.5rem;
      }
    }
  `]
})
export class CatalogComponent implements OnInit {
  private productService = inject(ProductService);
  private cartService = inject(CartService);
  private messageService = inject(MessageService);
  authService = inject(AuthService);

  products = signal<Product[]>([]);
  totalRecords = signal(0);
  loading = signal(false);

  page = 1;
  pageSize = 10;
  searchQuery = '';
  quantities: { [key: string]: number } = {};

  private searchDebounceTimer?: any;

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.productService.getProducts(this.page, this.pageSize, this.searchQuery).subscribe({
      next: (res) => {
        this.products.set(res.items);
        this.totalRecords.set(res.totalCount);
        this.loading.set(false);

        // Initialize quantities default to 1
        res.items.forEach(p => {
          if (!this.quantities[p.id]) {
            this.quantities[p.id] = 1;
          }
        });
      },
      error: () => {
        this.loading.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Hata',
          detail: 'Ürün kataloğu yüklenirken bir sorun oluştu.'
        });
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const first = event.first ?? 0;
    const rows = event.rows ?? 10;
    this.page = Math.floor(first / rows) + 1;
    this.pageSize = rows;
    this.loadProducts();
  }

  onSearchChange(): void {
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }
    this.searchDebounceTimer = setTimeout(() => {
      this.page = 1;
      this.loadProducts();
    }, 350);
  }

  addToCart(product: Product): void {
    const qty = this.quantities[product.id] || 1;
    if (qty <= 0) return;

    if (qty > product.stockQuantity) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Yetersiz Stok',
        detail: `Talep edilen miktar mevcut stoğu (${product.stockQuantity}) aşamaz.`
      });
      return;
    }

    this.cartService.addToCart(product, qty);
    this.messageService.add({
      severity: 'success',
      summary: 'Sepete Eklendi',
      detail: `${product.name} (${qty} adet) sepete eklendi.`
    });
  }

  getStockLabel(qty: number): string {
    if (qty <= 0) return 'Tükendi';
    if (qty <= 20) return `Kritik (${qty})`;
    return `Stokta (${qty})`;
  }

  getStockSeverity(qty: number): 'danger' | 'warn' | 'success' {
    if (qty <= 0) return 'danger';
    if (qty <= 20) return 'warn';
    return 'success';
  }
}
