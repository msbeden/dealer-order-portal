import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageService } from 'primeng/api';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { AuthService } from '../../core/services/auth.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TableModule, ButtonModule, InputNumberModule],
  template: `
    <div class="portal-container">
      <div class="page-header">
        <div>
          <h1 class="page-title">
            <i class="pi pi-shopping-cart"></i>
            Sipariş Sepeti
          </h1>
          <p class="page-subtitle">Sepetinizdeki ürünleri kontrol edip siparişinizi tamamlayabilirsiniz</p>
        </div>

        @if (cartService.items().length > 0) {
          <button
            pButton
            type="button"
            label="Sepeti Temizle"
            icon="pi pi-trash"
            class="p-button-outlined p-button-danger p-button-sm"
            (click)="clearCart()"
          ></button>
        }
      </div>

      @if (cartService.items().length === 0) {
        <div class="card empty-cart-card">
          <div class="empty-cart-content">
            <div class="empty-icon-wrap">
              <i class="pi pi-shopping-cart"></i>
            </div>
            <h3>Sepetiniz Boş</h3>
            <p>Kataloğumuzdan ürün seçerek siparişinizi oluşturmaya başlayabilirsiniz.</p>
            <button
              pButton
              type="button"
              label="Ürün Kataloğuna Git"
              icon="pi pi-arrow-left"
              routerLink="/catalog"
              class="p-button-primary mt-3"
            ></button>
          </div>
        </div>
      } @else {
        <div class="cart-grid">
          <div class="card cart-items-card">
            <p-table [value]="cartService.items()" responsiveLayout="stack" styleClass="p-datatable-striped">
              <ng-template pTemplate="header">
                <tr>
                  <th style="width: 120px">SKU</th>
                  <th>Ürün</th>
                  <th style="width: 130px; text-align: right">Birim Fiyat</th>
                  <th style="width: 140px; text-align: center">Adet</th>
                  <th style="width: 130px; text-align: right">Net Tutar</th>
                  <th style="width: 60px"></th>
                </tr>
              </ng-template>

              <ng-template pTemplate="body" let-item>
                <tr>
                  <td>
                    <span class="sku-badge">{{ item.product.sku }}</span>
                  </td>
                  <td>
                    <div class="product-name">{{ item.product.name }}</div>
                    <div class="vat-info">KDV: %{{ item.product.vatRate }}</div>
                  </td>
                  <td style="text-align: right">
                    <span class="price-val">{{ item.product.unitPrice | number:'1.2-2' }} ₺</span>
                  </td>
                  <td style="text-align: center">
                    <div class="qty-control">
                      <button
                        pButton
                        type="button"
                        icon="pi pi-minus"
                        class="p-button-rounded p-button-text p-button-sm qty-btn"
                        (click)="decrement(item.product.id, item.quantity)"
                      ></button>
                      <span class="qty-val">{{ item.quantity }}</span>
                      <button
                        pButton
                        type="button"
                        icon="pi pi-plus"
                        class="p-button-rounded p-button-text p-button-sm qty-btn"
                        [disabled]="item.quantity >= item.product.stockQuantity"
                        (click)="increment(item.product.id, item.quantity)"
                      ></button>
                    </div>
                  </td>
                  <td style="text-align: right">
                    <span class="line-total">{{ item.lineTotalGross | number:'1.2-2' }} ₺</span>
                  </td>
                  <td style="text-align: center">
                    <button
                      pButton
                      type="button"
                      icon="pi pi-trash"
                      class="p-button-text p-button-danger p-button-sm"
                      (click)="removeItem(item.product.id)"
                      title="Ürünü Çıkar"
                    ></button>
                  </td>
                </tr>
              </ng-template>
            </p-table>
          </div>

          <div class="card summary-card">
            <h3 class="summary-title">Sipariş Özeti</h3>

            <div class="summary-lines">
              <div class="summary-line">
                <span>Ara Toplam (Net)</span>
                <span>{{ cartService.totalNet() | number:'1.2-2' }} ₺</span>
              </div>
              <div class="summary-line">
                <span>Hesaplanan KDV</span>
                <span>{{ cartService.totalVat() | number:'1.2-2' }} ₺</span>
              </div>
              <div class="divider"></div>
              <div class="summary-line grand-total">
                <span>Genel Toplam (Brüt)</span>
                <span>{{ cartService.totalGross() | number:'1.2-2' }} ₺</span>
              </div>
            </div>

            <div class="order-notice">
              <i class="pi pi-info-circle"></i>
              <span>Siparişiniz onaylandığında faturanız oluşturulacak ve cari bakiyenize işlenecektir.</span>
            </div>

            <button
              pButton
              type="button"
              label="Siparişi Gönder"
              icon="pi pi-check-circle"
              class="p-button-primary w-full checkout-btn"
              [loading]="isSubmitting()"
              (click)="submitOrder()"
            ></button>
          </div>
        </div>
      }
    </div>
  `,
  styles: [`
    .empty-cart-card {
      padding: 4rem 2rem;
      text-align: center;

      .empty-icon-wrap {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        background: #f1f5f9;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        font-size: 2.5rem;
        color: #94a3b8;
        margin-bottom: 1.5rem;
      }

      h3 {
        font-size: 1.25rem;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 0.5rem;
      }

      p {
        color: #64748b;
        max-width: 400px;
        margin: 0 auto;
      }
    }

    .cart-grid {
      display: grid;
      grid-template-columns: 1fr 360px;
      gap: 1.5rem;
      align-items: start;

      @media (max-width: 960px) {
        grid-template-columns: 1fr;
      }
    }

    .sku-badge {
      font-family: monospace;
      font-size: 0.8rem;
      font-weight: 600;
      color: #3b82f6;
      background: #eff6ff;
      padding: 0.2rem 0.5rem;
      border-radius: 4px;
    }

    .product-name {
      font-weight: 600;
      color: #1e293b;
    }

    .vat-info {
      font-size: 0.75rem;
      color: #94a3b8;
    }

    .price-val {
      font-weight: 600;
      color: #475569;
    }

    .line-total {
      font-weight: 700;
      color: #0f172a;
    }

    .qty-control {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      background: #f8fafc;
      border: 1px solid #e2e8f0;
      border-radius: 8px;
      padding: 0.2rem 0.4rem;

      .qty-btn {
        width: 26px;
        height: 26px;
      }

      .qty-val {
        font-weight: 700;
        min-width: 24px;
        text-align: center;
      }
    }

    .summary-card {
      padding: 1.5rem;

      .summary-title {
        font-size: 1.15rem;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 1.25rem;
      }

      .summary-lines {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;

        .summary-line {
          display: flex;
          justify-content: space-between;
          font-size: 0.9rem;
          color: #475569;

          &.grand-total {
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

      .order-notice {
        display: flex;
        gap: 0.5rem;
        align-items: flex-start;
        background: #f0fdf4;
        border: 1px solid #bbf7d0;
        color: #166534;
        padding: 0.75rem;
        border-radius: 8px;
        font-size: 0.8rem;
        margin: 1.25rem 0;

        i {
          margin-top: 0.15rem;
          font-size: 0.95rem;
        }
      }

      .checkout-btn {
        padding: 0.85rem;
        font-weight: 600;
        border-radius: 8px;
      }
    }

    .w-full {
      width: 100%;
    }

    .mt-3 {
      margin-top: 1rem;
    }
  `]
})
export class CartComponent {
  cartService = inject(CartService);
  private orderService = inject(OrderService);
  private authService = inject(AuthService);
  private messageService = inject(MessageService);
  private router = inject(Router);

  isSubmitting = signal(false);

  increment(productId: string, currentQty: number): void {
    this.cartService.updateQuantity(productId, currentQty + 1);
  }

  decrement(productId: string, currentQty: number): void {
    if (currentQty > 1) {
      this.cartService.updateQuantity(productId, currentQty - 1);
    } else {
      this.removeItem(productId);
    }
  }

  removeItem(productId: string): void {
    this.cartService.removeFromCart(productId);
    this.messageService.add({
      severity: 'info',
      summary: 'Sepetten Çıkarıldı',
      detail: 'Ürün sepetinizden kaldırıldı.'
    });
  }

  clearCart(): void {
    this.cartService.clearCart();
    this.messageService.add({
      severity: 'info',
      summary: 'Sepet Temizlendi',
      detail: 'Sepetinizdeki tüm ürünler kaldırıldı.'
    });
  }

  async submitOrder(): Promise<void> {
    const items = this.cartService.items();
    if (items.length === 0) return;

    const user = this.authService.currentUser();
    const dealerId = user?.dealerId || '00000000-0000-0000-0000-000000000000';

    this.isSubmitting.set(true);

    try {
      // 1. Create order
      const order = await firstValueFrom(this.orderService.createOrder(dealerId));

      // 2. Add each cart item
      for (const item of items) {
        await firstValueFrom(this.orderService.addLine(order.id, item.product.id, item.quantity));
      }

      // 3. Submit order for approval
      const submittedOrder = await firstValueFrom(this.orderService.submitOrder(order.id));

      this.isSubmitting.set(false);
      this.cartService.clearCart();

      this.messageService.add({
        severity: 'success',
        summary: 'Sipariş İletildi',
        detail: `Siparişiniz (${submittedOrder.orderNo}) başarıyla oluşturuldu ve onay sürecine alındı.`
      });

      this.router.navigate(['/orders', submittedOrder.id]);
    } catch (err: any) {
      this.isSubmitting.set(false);
      const detail = err?.error?.detail || err?.error?.message || 'Sipariş iletilirken bir hata oluştu.';
      this.messageService.add({
        severity: 'error',
        summary: 'Sipariş Hatası',
        detail: detail
      });
    }
  }
}
