import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';

export interface LinkedAccount {
  accountId: number;
  name: string;
  institutionName: string;
  type: string;
  subtype: string | null;
  ownerUserId: string;
  ownerDisplayName: string;
  isMine: boolean;
}

@Injectable({ providedIn: 'root' })
export class AccountsService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<LinkedAccount[]> {
    return this.http.get<LinkedAccount[]>(`${API_BASE_URL}/plaid/accounts`);
  }
}
