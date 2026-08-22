import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

export interface UserListItem {
  id: number;
  userName: string;
  email: string;
  displayName: string;
  organizationId: number | null;
  branchId: number | null;
  isActive: boolean;
  roles: string[];
}

export interface RoleOption {
  id: number;
  name: string;
}

export interface OrganizationOption {
  id: number;
  code: string;
  name: string;
}

export interface OrgBranch {
  id: number;
  code: string | null;
  name: string;
  clinicId: number | null;
  laboratoryId: number | null;
  isActive: boolean;
}

export interface BranchAccessItem {
  branchId: number;
  isActive: boolean;
  hasClinicAccess: boolean;
  hasLaboratoryAccess: boolean;
  isDefault: boolean;
}

export interface BranchAccessRow extends BranchAccessItem {
  branchName: string;
  laboratoryName: string;
  clinicName: string;
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  displayName: string;
  password: string;
  roleName: string;
  organizationId: number;
  isActive: boolean;
}

export interface UpdateUserRequest {
  userName: string;
  email: string;
  displayName: string;
  roleName: string;
  organizationId: number;
  isActive: boolean;
  newPassword?: string;
}

interface PagedResponse<T> {
  items?: T[];
}

@Injectable({ providedIn: 'root' })
export class UsersService {
  private readonly baseUrl = '/api/users';

  constructor(private readonly http: HttpClient) {}

  list(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(this.baseUrl);
  }

  create(request: CreateUserRequest): Observable<UserListItem> {
    return this.http.post<UserListItem>(this.baseUrl, request);
  }

  update(userId: number, request: UpdateUserRequest): Observable<UserListItem> {
    return this.http.put<UserListItem>(`${this.baseUrl}/${userId}`, request);
  }

  toggleActive(userId: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${userId}/active`, { isActive });
  }

  listRoles(): Observable<RoleOption[]> {
    return this.http.get<RoleOption[]>('/api/roles');
  }

  listOrganizations(): Observable<OrganizationOption[]> {
    return this.http
      .get<PagedResponse<OrganizationOption>>('/api/organizations')
      .pipe(map((res) => res.items ?? []));
  }

  /** All active branches of the signed-in admin's organization. */
  listOrgBranches(): Observable<OrgBranch[]> {
    return this.http
      .get<PagedResponse<OrgBranch>>('/api/branches', { params: { pageSize: '500' } })
      .pipe(map((res) => (res.items ?? []).filter((b) => b.isActive)));
  }

  getLocations(userId: number): Observable<BranchAccessRow[]> {
    return this.http.get<BranchAccessRow[]>(`${this.baseUrl}/${userId}/locations`);
  }

  saveLocations(userId: number, accesses: BranchAccessItem[]): Observable<BranchAccessRow[]> {
    return this.http.put<BranchAccessRow[]>(`${this.baseUrl}/${userId}/locations`, { accesses });
  }
}
