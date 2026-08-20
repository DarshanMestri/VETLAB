import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';

export interface LaboratoryListItem {
  id: number;
  organizationId: number;
  laboratoryGroupId?: number | null;
  code: string;
  name: string;
  logo?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  gst?: string | null;
  registrationNumber?: string | null;
  whatsAppNumber?: string | null;
  reportHeader?: string | null;
  reportFooter?: string | null;
  digitalSignature?: string | null;
  isActive: boolean;
}

export interface LaboratoryGroupOption {
  id: number;
  code: string;
  name: string;
}

export interface LaboratoryCreateRequest {
  code: string;
  name: string;
  laboratoryGroupId?: number | null;
  logo?: string | null;
  address?: string | null;
  phone?: string | null;
  email?: string | null;
  gst?: string | null;
  registrationNumber?: string | null;
  whatsAppNumber?: string | null;
  reportHeader?: string | null;
  reportFooter?: string | null;
  digitalSignature?: string | null;
  isActive?: boolean;
}

export interface LaboratoryUpdateRequest extends LaboratoryCreateRequest {}

@Injectable({ providedIn: 'root' })
export class LaboratoriesService {
  private readonly base = '/api/laboratories';

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

  create(payload: LaboratoryCreateRequest): Observable<LaboratoryListItem> {
    return this.http.post<LaboratoryListItem>(this.base, payload);
  }

  update(id: number, payload: LaboratoryUpdateRequest): Observable<LaboratoryListItem> {
    return this.http.put<LaboratoryListItem>(`${this.base}/${id}`, payload);
  }

  toggleStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/status`, isActive);
  }

  getGroups(): Observable<LaboratoryGroupOption[]> {
    return this.http
      .get<any>('/api/laboratory-groups', {
        params: { page: '1', pageSize: '100' },
      })
      .pipe(map((res: any) => (res?.items ?? []) as LaboratoryGroupOption[]));
  }
}
