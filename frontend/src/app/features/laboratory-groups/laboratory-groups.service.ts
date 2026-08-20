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
  laboratoryCount: number;
}

export interface LaboratoryGroupLaboratoryItem {
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

export interface LaboratoryListItem {
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

  getLaboratories(laboratoryGroupId: number): Observable<LaboratoryGroupLaboratoryItem[]> {
    return this.http.get<LaboratoryGroupLaboratoryItem[]>(`${this.base}/${laboratoryGroupId}/laboratories`);
  }

  getAvailableLaboratories(): Observable<LaboratoryListItem[]> {
    return this.http.get<LaboratoryListItem[]>('/api/laboratories');
  }

  assignLaboratory(laboratoryGroupId: number, laboratoryId: number): Observable<LaboratoryGroupLaboratoryItem> {
    return this.http.put<LaboratoryGroupLaboratoryItem>(`${this.base}/${laboratoryGroupId}/laboratories/${laboratoryId}`, {});
  }

  unassignLaboratory(laboratoryGroupId: number, laboratoryId: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${laboratoryGroupId}/laboratories/${laboratoryId}`);
  }
}
