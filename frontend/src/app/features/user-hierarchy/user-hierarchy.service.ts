import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface HierarchyUser {
  id: number;
  userName: string;
  displayName: string;
  email: string;
  isActive: boolean;
  branchId: number | null;
  roles: string[];
  lastLoginAt: string | null;
}

export interface HierarchyBranch {
  id: number;
  code: string;
  name: string;
  isActive: boolean;
  users: HierarchyUser[];
}

export interface HierarchyResponse {
  organization: { id: number; code: string; name: string };
  branches: HierarchyBranch[];
  unassigned: HierarchyUser[];
}

export interface LoginHistoryItem {
  id: number;
  userId: number;
  userName: string | null;
  displayName: string | null;
  success: boolean;
  ipAddress: string | null;
  userAgent: string | null;
  failureReason: string | null;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class UserHierarchyService {
  private readonly base = '/api/users';

  constructor(private readonly http: HttpClient) {}

  getHierarchy(): Observable<HierarchyResponse> {
    return this.http.get<HierarchyResponse>(`${this.base}/hierarchy`);
  }

  getLoginHistory(limit = 100, success?: boolean | null): Observable<LoginHistoryItem[]> {
    const params: Record<string, string> = { limit: limit.toString() };
    if (success != null) params.success = success.toString();
    return this.http.get<LoginHistoryItem[]>(`${this.base}/login-history`, { params });
  }
}
