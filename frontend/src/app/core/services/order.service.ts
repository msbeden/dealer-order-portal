import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DealerInsight, Order, PagedResult } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private http = inject(HttpClient);

  getOrders(page = 1, pageSize = 20, status?: number): Observable<PagedResult<Order>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status !== undefined) {
      params = params.set('status', status);
    }
    return this.http.get<PagedResult<Order>>(`${environment.apiUrl}/orders`, { params });
  }

  getOrderById(id: string): Observable<Order> {
    return this.http.get<Order>(`${environment.apiUrl}/orders/${id}`);
  }

  createOrder(dealerId: string): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders`, { dealerId });
  }

  addLine(orderId: string, productId: string, quantity: number): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders/${orderId}/lines`, { productId, quantity });
  }

  submitOrder(orderId: string): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders/${orderId}/submit`, {});
  }

  approveOrder(orderId: string): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders/${orderId}/approve`, {});
  }

  rejectOrder(orderId: string, reason: string): Observable<Order> {
    return this.http.post<Order>(`${environment.apiUrl}/orders/${orderId}/reject`, { reason });
  }

  getDealerInsight(dealerId: string): Observable<DealerInsight> {
    return this.http.post<DealerInsight>(`${environment.apiUrl}/dealers/${dealerId}/insight`, {});
  }

  getDashboardMetrics(): Observable<any> {
    return this.http.get<any>(`${environment.apiUrl}/reports/dashboard`);
  }

  getTopProducts(): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/reports/top-products`);
  }
}
