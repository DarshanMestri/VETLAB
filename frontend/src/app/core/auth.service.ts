// src/app/core/auth.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, Subscription, timer } from 'rxjs';
import { tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode'; // ✅ v4 named import

const API_URL = '/api/auth';
type Decoded = Record<string, any>;
interface LoginResponse {
  token: string;
  username?: string;
  clinicId?: number;
  branchId?: number | null;
}
export interface LabBranchOption {
  branchId: number;
  branchCode: string;
  branchName: string;
}
export interface LabClinicOption {
  clinicId: number;
  clinicCode: string;
  clinicName: string;
  branches: LabBranchOption[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private tokenKey = 'token';
  private clinicKey = 'selectedClinicId';
  private branchKey = 'selectedBranchId';

  private _token$ = new BehaviorSubject<string | null>(null);
  private _roles$ = new BehaviorSubject<string[]>([]);
  private _username$ = new BehaviorSubject<string | null>(null);
  private _selectedClinicId$ = new BehaviorSubject<number | null>(null);
  private _selectedBranchId$ = new BehaviorSubject<number | null>(null);
  private expiryTimer?: Subscription;

  token$ = this._token$.asObservable();
  roles$ = this._roles$.asObservable();
  username$ = this._username$.asObservable();
  selectedClinicId$ = this._selectedClinicId$.asObservable();
  selectedBranchId$ = this._selectedBranchId$.asObservable();

  constructor() {
    const t = this.safeGet(this.tokenKey);
    if (t) this.setSession(t);
    this._selectedClinicId$.next(this.parseStoredNumber(this.safeGet(this.clinicKey)));
    this._selectedBranchId$.next(this.parseStoredNumber(this.safeGet(this.branchKey)));
  }

  login(
    usernameOrEmail: string,
    password: string,
    clinicId: number,
    branchId: number | null
  ): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${API_URL}/login`, { usernameOrEmail, password, clinicId, branchId })
      .pipe(
        tap((res) => {
          this.setSession(res.token);
          this.setLabLocation(res.clinicId ?? clinicId, res.branchId ?? branchId);
          this.router.navigate(['/dashboard']);
        })
      );
  }

  getLabLocations(): Observable<LabClinicOption[]> {
    return this.http.get<LabClinicOption[]>(`${API_URL}/lab-locations`);
  }

  logout() {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    const token = this._token$.value ?? this.safeGet(this.tokenKey);
    if (!token) return false;
    const d = this.decode(token);
    const exp = d?.exp as number | undefined; // unix seconds
    if (!exp) return true;
    return exp * 1000 > Date.now() + 30_000; // 30s skew
  }

  getToken(): string | null {
    return this._token$.value ?? this.safeGet(this.tokenKey);
  }

  hasAnyRole(roles: string[]): boolean {
    const current = this._roles$.value;
    return roles.some((r) => current.includes(r));
  }
  hasRole(...roles: string[]) {
    return this.hasAnyRole(roles);
  }
  getUsername(): string | null {
    return this._username$.value;
  }
  getSelectedClinicId(): number | null {
    return this._selectedClinicId$.value;
  }
  getSelectedBranchId(): number | null {
    return this._selectedBranchId$.value;
  }
  setLabLocation(clinicId: number | null, branchId: number | null) {
    this._selectedClinicId$.next(clinicId);
    this._selectedBranchId$.next(branchId);
    if (clinicId == null) this.safeRemove(this.clinicKey);
    else this.safeSet(this.clinicKey, String(clinicId));
    if (branchId == null) this.safeRemove(this.branchKey);
    else this.safeSet(this.branchKey, String(branchId));
  }

  // ---- internals ----
  private setSession(token: string) {
    this.safeSet(this.tokenKey, token);
    this._token$.next(token);

    const d = this.decode(token);
    const roles = this.extractRoles(d);
    this._roles$.next(roles);

    const name = d?.unique_name ?? d?.name ?? d?.username ?? d?.sub ?? d?.email ?? null;
    this._username$.next(name);

    this.scheduleAutoLogout(d?.exp);
  }

  private clearSession() {
    this.safeRemove(this.tokenKey);
    this._token$.next(null);
    this._roles$.next([]);
    this._username$.next(null);
    this.expiryTimer?.unsubscribe();
  }

  private scheduleAutoLogout(exp?: number) {
    this.expiryTimer?.unsubscribe();
    if (!exp) return;
    const delay = Math.max(exp * 1000 - Date.now() - 2000, 0);
    this.expiryTimer = timer(delay).subscribe(() => this.logout());
  }

  private decode(token: string): Decoded | null {
    try {
      return jwtDecode<Decoded>(token);
    } catch {
      return null;
    }
  }

  private extractRoles(d: Decoded | null): string[] {
    if (!d) return [];
    const raw =
      d['role'] ??
      d['roles'] ??
      d['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
      [];
    if (Array.isArray(raw)) return raw.filter((x): x is string => typeof x === 'string');
    if (typeof raw === 'string') return [raw];
    return [];
  }

  private safeGet(key: string) {
    try {
      return typeof localStorage !== 'undefined' ? localStorage.getItem(key) : null;
    } catch {
      return null;
    }
  }
  private safeSet(key: string, val: string) {
    try {
      if (typeof localStorage !== 'undefined') localStorage.setItem(key, val);
    } catch {}
  }
  private safeRemove(key: string) {
    try {
      if (typeof localStorage !== 'undefined') localStorage.removeItem(key);
    } catch {}
  }
  private parseStoredNumber(value: string | null): number | null {
    if (!value) return null;
    const parsed = Number(value);
    return Number.isInteger(parsed) && parsed > 0 ? parsed : null;
  }
}
