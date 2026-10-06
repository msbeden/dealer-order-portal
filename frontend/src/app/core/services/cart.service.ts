import { Injectable, computed, signal } from '@angular/core';
import { CartItem, Product } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class CartService {
  private readonly CART_KEY = 'portal_cart_items';

  items = signal<CartItem[]>(this.loadCartFromStorage());

  itemCount = computed(() => this.items().reduce((acc, item) => acc + item.quantity, 0));
  totalNet = computed(() => this.items().reduce((acc, item) => acc + item.lineTotalNet, 0));
  totalVat = computed(() => this.items().reduce((acc, item) => acc + item.lineTotalVat, 0));
  totalGross = computed(() => this.items().reduce((acc, item) => acc + item.lineTotalGross, 0));

  addToCart(product: Product, quantity = 1): void {
    const current = this.items();
    const existingIndex = current.findIndex(i => i.product.id === product.id);

    let updated: CartItem[];
    if (existingIndex > -1) {
      updated = current.map((item, idx) => {
        if (idx === existingIndex) {
          const newQty = item.quantity + quantity;
          return this.calculateItemTotals(item.product, newQty);
        }
        return item;
      });
    } else {
      updated = [...current, this.calculateItemTotals(product, quantity)];
    }

    this.items.set(updated);
    this.saveCartToStorage(updated);
  }

  updateQuantity(productId: string, quantity: number): void {
    if (quantity <= 0) {
      this.removeFromCart(productId);
      return;
    }

    const updated = this.items().map(item => {
      if (item.product.id === productId) {
        return this.calculateItemTotals(item.product, quantity);
      }
      return item;
    });

    this.items.set(updated);
    this.saveCartToStorage(updated);
  }

  removeFromCart(productId: string): void {
    const updated = this.items().filter(item => item.product.id !== productId);
    this.items.set(updated);
    this.saveCartToStorage(updated);
  }

  clearCart(): void {
    this.items.set([]);
    localStorage.removeItem(this.CART_KEY);
  }

  private calculateItemTotals(product: Product, quantity: number): CartItem {
    const lineTotalNet = Math.round(product.unitPrice * quantity * 100) / 100;
    const lineTotalVat = Math.round(lineTotalNet * (product.vatRate / 100) * 100) / 100;
    const lineTotalGross = lineTotalNet + lineTotalVat;

    return {
      product,
      quantity,
      lineTotalNet,
      lineTotalVat,
      lineTotalGross
    };
  }

  private loadCartFromStorage(): CartItem[] {
    const raw = localStorage.getItem(this.CART_KEY);
    if (!raw) return [];
    try {
      return JSON.parse(raw) as CartItem[];
    } catch {
      return [];
    }
  }

  private saveCartToStorage(items: CartItem[]): void {
    localStorage.setItem(this.CART_KEY, JSON.stringify(items));
  }
}
