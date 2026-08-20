import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

export interface BranchListItem {
  id: number;
  organizationId: number;
  clinicId?: number | null;
  laboratoryId?: number | null;
  code: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive: boolean;
  clinicName?: string | null;
  laboratoryName?: string | null;
}

export interface BranchCreateRequest {
  clinicId?: number | null;
  laboratoryId?: number | null;
  code: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive?: boolean;
}

export interface BranchUpdateRequest extends BranchCreateRequest {}

export interface ClinicOption {
  id: number;
  organizationId: number;
  code: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive: boolean;
}

interface PagedClinicResponse {
  items?: ClinicOption[];
}

export interface LaboratoryOption {
  id: number;
  organizationId: number;
  code: string;
  name: string;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  isActive: boolean;
}

interface PagedLaboratoryResponse {
  items?: LaboratoryOption[];
}

export interface BranchDepartmentAssignment {
  id: number;
  branchId: number;
  departmentId: number;
  departmentName: string;
  departmentCode?: string | null;
  isActive: boolean;
}

export interface BranchTestAssignment {
  id: number;
  branchId: number;
  testId: number;
  testName: string;
  testCode?: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class BranchesService {
  private readonly base = '/api/branches';

  constructor(private readonly http: HttpClient) {}

  list(search = '', clinicId?: number | null, page = 1, pageSize = 20): Observable<any> {
    const params: Record<string, string> = {
      search,
      page: page.toString(),
      pageSize: pageSize.toString(),
    };

    if (clinicId != null) {
      params.clinicId = clinicId.toString();
    }

    return this.http.get<any>(this.base, { params });
  }

  get(id: number): Observable<BranchListItem> {
    return this.http.get<BranchListItem>(`${this.base}/${id}`);
  }

  create(payload: BranchCreateRequest): Observable<BranchListItem> {
    return this.http.post<BranchListItem>(this.base, payload);
  }

  update(id: number, payload: BranchUpdateRequest): Observable<BranchListItem> {
    return this.http.put<BranchListItem>(`${this.base}/${id}`, payload);
  }

  toggleStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, isActive);
  }

  getClinics(organizationId?: number): Observable<ClinicOption[]> {
    const params: Record<string, string> = {};
    if (organizationId != null) {
      params.organizationId = organizationId.toString();
    }
    return this.http
      .get<PagedClinicResponse>('/api/clinics', { params })
      .pipe(map((response) => response.items ?? []));
  }

  getLaboratories(organizationId?: number): Observable<LaboratoryOption[]> {
    const params: Record<string, string> = {};
    if (organizationId != null) {
      params.organizationId = organizationId.toString();
    }
    return this.http
      .get<PagedLaboratoryResponse>('/api/laboratories', { params })
      .pipe(map((response) => response.items ?? []));
  }

  getDepartments(branchId: number): Observable<BranchDepartmentAssignment[]> {
    return this.http.get<BranchDepartmentAssignment[]>(`${this.base}/${branchId}/departments`);
  }

  getAvailableDepartments(branchId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/${branchId}/departments/available`);
  }

  assignDepartment(branchId: number, departmentId: number): Observable<BranchDepartmentAssignment> {
    return this.http.post<BranchDepartmentAssignment>(
      `${this.base}/${branchId}/departments/${departmentId}`,
      {},
    );
  }

  removeDepartment(branchId: number, departmentId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${branchId}/departments/${departmentId}`);
  }

  toggleDepartmentStatus(
    branchId: number,
    departmentId: number,
    isActive: boolean,
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.base}/${branchId}/departments/${departmentId}/status`,
      isActive,
    );
  }

  getBranchTests(branchId: number): Observable<BranchTestAssignment[]> {
    return this.http.get<BranchTestAssignment[]>(`${this.base}/${branchId}/tests`);
  }

  getAvailableTests(branchId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.base}/${branchId}/tests/available`);
  }

  assignTest(branchId: number, testId: number): Observable<BranchTestAssignment> {
    return this.http.post<BranchTestAssignment>(`${this.base}/${branchId}/tests/${testId}`, {});
  }

  removeTest(assignmentId: number): Observable<void> {
    return this.http.delete<void>(`/api/branch-tests/${assignmentId}`);
  }

  toggleTestStatus(assignmentId: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`/api/branch-tests/${assignmentId}/status`, isActive);
  }
}
