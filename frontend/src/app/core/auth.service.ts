// src/app/core/auth.service.ts
import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, Subscription, timer } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { jwtDecode } from 'jwt-decode';

const API_URL = '/api/auth';
type Decoded = Record<string, any>;

export interface LoginStartResponse {
  requiresBranchSelection: boolean;
  branches: BranchOption[];
  contextToken?: string;
}

export interface BranchOption {
  id: number;
  name: string;
  organizationId?: number;
  organizationName?: string | null;
  code: string | null;
  hasClinicAccess: boolean;
  hasLaboratoryAccess: boolean;
  isDefault: boolean;
}

interface SelectBranchResponse {
  token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private tokenKey = 'token';
  private orgKey = 'selectedOrganizationId';
  private branchKey = 'selectedBranchId';

  // Short-lived token issued by step 1 while the user picks a branch.
  private _contextToken: string | null = null;

  private _token$ = new BehaviorSubject<string | null>(null);
  private _roles$ = new BehaviorSubject<string[]>([]);
  private _username$ = new BehaviorSubject<string | null>(null);
  private _selectedOrganizationId$ = new BehaviorSubject<number | null>(null);
  private _selectedBranchId$ = new BehaviorSubject<number | null>(null);
  private expiryTimer?: Subscription;

  token$ = this._token$.asObservable();
  roles$ = this._roles$.asObservable();
  username$ = this._username$.asObservable();
  selectedOrganizationId$ = this._selectedOrganizationId$.asObservable();
  selectedBranchId$ = this._selectedBranchId$.asObservable();

  constructor() {
    const t = this.safeGet(this.tokenKey);
    if (t && this.isExpired(t)) {
      this.safeRemove(this.tokenKey);
    } else if (t) {
      this.setSession(t, { persistLocation: false });
    }
    this._selectedOrganizationId$.next(this.parseStoredNumber(this.safeGet(this.orgKey)));
    this._selectedBranchId$.next(this.parseStoredNumber(this.safeGet(this.branchKey)));
  }

  /** Step 1: credentials only. Returns the branch list scoped to the user's access. */
  login(usernameOrEmail: string, password: string): Observable<LoginStartResponse> {
    return this.http
      .post<LoginStartResponse>(`${API_URL}/login`, { usernameOrEmail, password })
      .pipe(tap((res) => (this._contextToken = res.contextToken ?? null)));
  }

  /** Step 2: exchange the context token (or an existing full token) for a branch-scoped JWT. */
  selectBranch(branchId: number): Observable<void> {
    const token = this._contextToken ?? this.getToken();
    const headers = new HttpHeaders(token ? { Authorization: `Bearer ${token}` } : {});
    return this.http
      .post<SelectBranchResponse>(`${API_URL}/select-branch`, { branchId }, { headers })
      .pipe(
        tap((res) => {
          this._contextToken = null;
          this.setSession(res.token);
          this.router.navigate(['/dashboard']);
        }),
        map(() => void 0)
      );
  }

  /** Branches available to the signed-in user (used by the header switcher). */
  myBranches(): Observable<BranchOption[]> {
    return this.http.get<BranchOption[]>(`${API_URL}/my-branches`);
  }

  logout() {
    this.clearSession();
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    const token = this._token$.value ?? this.safeGet(this.tokenKey);
    if (!token) return false;
    return !this.isExpired(token);
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
  getSelectedOrganizationId(): number | null {
    return this._selectedOrganizationId$.value;
  }
  getSelectedBranchId(): number | null {
    return this._selectedBranchId$.value;
  }

  getJwtPayload(): Decoded | null {
    const token = this.getToken();
    if (!token) return null;
    return this.decode(token);
  }

  getOrganizationName(): string | null {
    const d = this.getJwtPayload();
    return d?.organization_name ?? null;
  }

  getBranchName(): string | null {
    const d = this.getJwtPayload();
    return d?.branch_name ?? null;
  }

  hasClinicAccess(): boolean {
    const payload = this.getJwtPayload();
    return payload?.clinic_access === 'true';
  }

  hasLaboratoryAccess(): boolean {
    const payload = this.getJwtPayload();
    return payload?.laboratory_access === 'true';
  }

  setSessionLocation(organizationId: number | null, branchId: number | null) {
    this._selectedOrganizationId$.next(organizationId);
    this._selectedBranchId$.next(branchId);
    if (organizationId == null) this.safeRemove(this.orgKey);
    else this.safeSet(this.orgKey, String(organizationId));
    if (branchId == null) this.safeRemove(this.branchKey);
    else this.safeSet(this.branchKey, String(branchId));
  }

  // ---- internals ----
  private setSession(token: string, opts: { persistLocation?: boolean } = {}) {
    this.safeSet(this.tokenKey, token);
    this._token$.next(token);

    const d = this.decode(token);
    const roles = this.extractRoles(d);
    this._roles$.next(roles);

    const name =
      d?.display_name ?? d?.displayName ?? d?.unique_name ?? d?.name ?? d?.sub ?? d?.email ?? null;
    this._username$.next(name);

    const orgId = typeof d?.organization_id === 'number' ? d.organization_id : null;
    const branchId = typeof d?.branch_id === 'number' ? d.branch_id : null;
    if (opts.persistLocation !== false) {
      this.setSessionLocation(orgId, branchId);
    } else {
      this._selectedOrganizationId$.next(orgId);
      this._selectedBranchId$.next(branchId);
    }

    this.scheduleAutoLogout(d?.exp);
  }

  private clearSession() {
    this.safeRemove(this.tokenKey);
    this.safeRemove(this.orgKey);
    this.safeRemove(this.branchKey);
    this._contextToken = null;
    this._token$.next(null);
    this._roles$.next([]);
    this._username$.next(null);
    this._selectedOrganizationId$.next(null);
    this._selectedBranchId$.next(null);
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
      d['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role'] ??
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

  private isExpired(token: string): boolean {
    const d = this.decode(token);
    const exp = d?.exp as number | undefined;
    if (!exp) return false;
    return exp * 1000 <= Date.now() + 30_000;
  }
}
