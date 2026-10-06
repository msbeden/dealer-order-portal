export interface UserInfo {
  userId: string;
  email: string;
  fullName: string;
  tenantId: string;
  role: 'Admin' | 'SalesRep' | 'Dealer';
  dealerId?: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  user: UserInfo;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface Product {
  id: string;
  tenantId: string;
  sku: string;
  name: string;
  categoryId: string;
  categoryName?: string;
  unitPrice: number;
  vatRate: number;
  stockQuantity: number;
  rowVersion: string;
}

export interface CartItem {
  product: Product;
  quantity: number;
  lineTotalNet: number;
  lineTotalVat: number;
  lineTotalGross: number;
}

export enum OrderStatus {
  Draft = 0,
  Submitted = 10,
  Approved = 20,
  Invoiced = 30,
  Rejected = 40,
  Cancelled = 50
}

export interface OrderLine {
  id: string;
  productId: string;
  sku: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  vatRate: number;
  lineTotalNet: number;
  lineTotalVat: number;
  lineTotalGross: number;
}

export interface Order {
  id: string;
  tenantId: string;
  orderNo: string;
  dealerId: string;
  dealerName?: string;
  status: OrderStatus;
  approvedBy?: string;
  approvedAtUtc?: string;
  rejectionReason?: string;
  cancellationReason?: string;
  totalNet: number;
  totalVat: number;
  totalGross: number;
  createdAtUtc: string;
  lines: OrderLine[];
}

export interface Dealer {
  id: string;
  tenantId: string;
  code: string;
  name: string;
  city: string;
  creditLimit: number;
  currentBalance: number;
  availableCredit: number;
  salesRepId?: string;
}

export interface DealerInsight {
  dealerId: string;
  dealerName: string;
  totalRevenueLast90Days: number;
  totalOrdersLast90Days: number;
  topCategories: { categoryName: string; totalSpent: number; orderCount: number }[];
  frequencyTrend: string;
  executiveSummary: string;
  recommendedActions: string[];
  generatedAtUtc: string;
}
