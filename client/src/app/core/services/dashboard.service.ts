import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';

export interface CategorySlice {
  category: string;
  total: number;
  transactionCount: number;
}

export interface MerchantSlice {
  merchant: string;
  total: number;
  transactionCount: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  spendByCategory(accountIds?: number[], from?: string, to?: string): Observable<CategorySlice[]> {
    const params = this.buildFilterParams(accountIds, from, to);
    return this.http.get<CategorySlice[]>(`${API_BASE_URL}/dashboard/spend-by-category`, { params });
  }

  spendByMerchant(
    category: string,
    accountIds?: number[],
    from?: string,
    to?: string,
  ): Observable<MerchantSlice[]> {
    const params = this.buildFilterParams(accountIds, from, to).set('category', category);
    return this.http.get<MerchantSlice[]>(`${API_BASE_URL}/dashboard/spend-by-merchant`, { params });
  }

  private buildFilterParams(accountIds?: number[], from?: string, to?: string): HttpParams {
    let params = new HttpParams();
    for (const id of accountIds ?? []) {
      params = params.append('accountIds', id);
    }
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return params;
  }
}
