import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
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

  updateProfile(displayName: string): Observable<CurrentUser> {
    return this.http
      .put<AuthResponse>(`${API_BASE_URL}/auth/profile`, { displayName })
      .pipe(
        tap((res) => this.persistSession(res)),
        map((res) => ({ userId: res.userId, email: res.email, displayName: res.displayName })),
      );
  }

  updateEmail(newEmail: string, currentPassword: string): Observable<CurrentUser> {
    return this.http
      .put<AuthResponse>(`${API_BASE_URL}/auth/email`, { newEmail, currentPassword })
      .pipe(
        tap((res) => this.persistSession(res)),
        map((res) => ({ userId: res.userId, email: res.email, displayName: res.displayName })),
      );
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http
      .put<{ changed: boolean }>(`${API_BASE_URL}/auth/password`, { currentPassword, newPassword })
      .pipe(map(() => undefined));
  }

  forgotPassword(email: string): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${API_BASE_URL}/auth/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string): Observable<void> {
    return this.http
      .post<{ reset: boolean }>(`${API_BASE_URL}/auth/reset-password`, { email, token, newPassword })
      .pipe(map(() => undefined));
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
