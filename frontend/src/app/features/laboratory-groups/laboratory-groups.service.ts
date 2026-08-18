import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface LaboratoryGroupListItem {
  id: number;
  organizationId: number;
  organizationName?: string | null;
  code: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  createdDate: string;
  modifiedDate?: string | null;
  isDeleted: boolean;
  clinicCount: number;
}

export interface LaboratoryGroupClinicItem {
  id: number;
  organizationId: number;
  code: string;
  name: string;
  shortName?: string | null;
  isActive: boolean;
  laboratoryGroupId?: number | null;
}

export interface LaboratoryGroupCreateRequest {
  code: string;
  name: string;
  description?: string | null;
  isActive?: boolean;
}

export interface LaboratoryGroupUpdateRequest extends LaboratoryGroupCreateRequest {}

export interface ClinicListItem {
  id: number;
  organizationId: number;
  code: string;
  name: string;
  shortName?: string | null;
  isActive: boolean;
  laboratoryGroupId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class LaboratoryGroupsService {
  private readonly base = '/api/laboratory-groups';

  constructor(private readonly http: HttpClient) {}

  list(search = '', page = 1, pageSize = 20): Observable<any> {
    return this.http.get<any>(this.base, {
      params: {
        search,
        page: page.toString(),
        pageSize: pageSize.toString(),
      },
    });
  }

  get(id: number): Observable<LaboratoryGroupListItem> {
    return this.http.get<LaboratoryGroupListItem>(`${this.base}/${id}`);
  }

  create(payload: LaboratoryGroupCreateRequest): Observable<LaboratoryGroupListItem> {
    return this.http.post<LaboratoryGroupListItem>(this.base, payload);
  }

  update(id: number, payload: LaboratoryGroupUpdateRequest): Observable<LaboratoryGroupListItem> {
    return this.http.put<LaboratoryGroupListItem>(`${this.base}/${id}`, payload);
  }

  toggleStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, isActive);
  }

  getClinics(laboratoryGroupId: number): Observable<LaboratoryGroupClinicItem[]> {
    return this.http.get<LaboratoryGroupClinicItem[]>(`${this.base}/${laboratoryGroupId}/clinics`);
  }

  getAvailableClinics(): Observable<ClinicListItem[]> {
    return this.http.get<ClinicListItem[]>('/api/clinics');
  }

  assignClinic(laboratoryGroupId: number, clinicId: number): Observable<LaboratoryGroupClinicItem> {
    return this.http.put<LaboratoryGroupClinicItem>(`${this.base}/${laboratoryGroupId}/clinics/${clinicId}`, {});
  }

  unassignClinic(laboratoryGroupId: number, clinicId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${laboratoryGroupId}/clinics/${clinicId}`);
  }
}
