import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../config';

export interface HouseholdMemberSummary {
  userId: string;
  displayName: string;
  email: string;
  status: 'Pending' | 'Verified' | 'Declined';
}

export interface HouseholdSummary {
  hasHousehold: boolean;
  members: HouseholdMemberSummary[];
}

@Injectable({ providedIn: 'root' })
export class HouseholdService {
  constructor(private readonly http: HttpClient) {}

  getMyHousehold(): Observable<HouseholdSummary> {
    return this.http.get<HouseholdSummary>(`${API_BASE_URL}/household/me`);
  }

  invite(email: string): Observable<{ memberId: number; status: string }> {
    return this.http.post<{ memberId: number; status: string }>(`${API_BASE_URL}/household/invite`, { email });
  }
}
