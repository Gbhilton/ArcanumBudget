import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';

export interface CategorySlice {
  category: string;
  total: number;
  transactionCount: number;
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  spendByCategory(accountIds?: number[]): Observable<CategorySlice[]> {
    let params = new HttpParams();
    for (const id of accountIds ?? []) {
      params = params.append('accountIds', id);
    }
    return this.http.get<CategorySlice[]>(`${API_BASE_URL}/dashboard/spend-by-category`, { params });
  }
}
