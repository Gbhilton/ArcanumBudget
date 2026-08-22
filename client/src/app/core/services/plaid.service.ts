import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';

@Injectable({ providedIn: 'root' })
export class PlaidService {
  constructor(private readonly http: HttpClient) {}

  createLinkToken(): Observable<{ linkToken: string }> {
    return this.http.post<{ linkToken: string }>(`${API_BASE_URL}/plaid/link-token`, {});
  }

  exchangePublicToken(publicToken: string, institutionName: string): Observable<{ itemId: number }> {
    return this.http.post<{ itemId: number }>(`${API_BASE_URL}/plaid/exchange`, {
      publicToken,
      institutionName,
    });
  }
}
