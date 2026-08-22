import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface LaboratoryListItem {
  id: number;
  organizationId: number;
  organizationName?: string | null;
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

export interface LaboratoryCreateRequest {
  code: string;
  name: string;
  organizationId?: number;
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
}
