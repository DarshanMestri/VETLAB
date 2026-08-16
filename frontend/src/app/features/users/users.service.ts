import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface UserListItem {
  id: number;
  userName: string;
  email: string;
  displayName: string;
  organizationId: number | null;
  clinicId: number | null;
  branchId: number | null;
  isActive: boolean;
  roles: string[];
}

export interface RoleOption {
  id: number;
  name: string;
}

export interface BranchOption {
  branchId: number;
  branchCode: string;
  branchName: string;
}

export interface ClinicOption {
  clinicId: number;
  clinicCode: string;
  clinicName: string;
  branches: BranchOption[];
}

export interface CreateUserRequest {
  userName: string;
  email: string;
  displayName: string;
  password: string;
  roleName: string;
  clinicId: number | null;
  branchId: number | null;
  isActive: boolean;
}

export interface UpdateUserRequest {
  userName: string;
  email: string;
  displayName: string;
  roleName: string;
  clinicId: number | null;
  branchId: number | null;
  isActive: boolean;
  newPassword?: string;
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

  listLocations(): Observable<ClinicOption[]> {
    return this.http.get<ClinicOption[]>('/api/auth/lab-locations');
  }
}
