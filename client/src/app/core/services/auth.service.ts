import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_BASE_URL } from '../config';

export interface CurrentUser {
  userId: string;
  email: string;
  displayName: string;
}

interface AuthResponse {
  token: string;
  userId: string;
  email: string;
  displayName: string;
}

const STORAGE_KEY = 'arcanum.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal = signal<CurrentUser | null>(this.readStoredUser());

  readonly currentUser = this.userSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.userSignal() !== null);

  constructor(private readonly http: HttpClient) {}

  get token(): string | null {
    return localStorage.getItem(`${STORAGE_KEY}.token`);
  }

  register(email: string, password: string, displayName: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE_URL}/auth/register`, { email, password, displayName })
      .pipe(tap((res) => this.persistSession(res)));
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE_URL}/auth/login`, { email, password })
      .pipe(tap((res) => this.persistSession(res)));
  }

  logout(): void {
    localStorage.removeItem(`${STORAGE_KEY}.token`);
    localStorage.removeItem(`${STORAGE_KEY}.user`);
    this.userSignal.set(null);
  }

  private persistSession(res: AuthResponse): void {
    const user: CurrentUser = { userId: res.userId, email: res.email, displayName: res.displayName };
    localStorage.setItem(`${STORAGE_KEY}.token`, res.token);
    localStorage.setItem(`${STORAGE_KEY}.user`, JSON.stringify(user));
    this.userSignal.set(user);
  }

  private readStoredUser(): CurrentUser | null {
    const raw = localStorage.getItem(`${STORAGE_KEY}.user`);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }
}
