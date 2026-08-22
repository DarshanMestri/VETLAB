import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

export interface ClinicListItem {
  id: number;
  organizationId: number;
  organizationName?: string | null;
  code: string;
  name: string;
  shortName?: string;
  phone?: string;
  email?: string;
  address?: string;
  logo?: string;
  licenseNumber?: string;
  isActive: boolean;
}

export interface ClinicCreateRequest {
  code: string;
  name: string;
  organizationId?: number;
  shortName?: string;
  phone?: string;
  email?: string;
  address?: string;
  logo?: string;
  licenseNumber?: string;
  isActive?: boolean;
}

export interface PagedClinicResponse {
  total: number;
  page: number;
  pageSize: number;
  items: ClinicListItem[];
}

@Injectable({ providedIn: 'root' })
export class ClinicsService {
  private baseUrl = '/api/clinics';

  constructor(private http: HttpClient) {}

  list(params?: { search?: string; page?: number; pageSize?: number }): Observable<PagedClinicResponse> {
    const query = new URLSearchParams();
    if (params?.search) query.set('search', params.search);
    if (params?.page) query.set('page', params.page.toString());
    if (params?.pageSize) query.set('pageSize', params.pageSize.toString());
    return this.http.get<PagedClinicResponse>(`${this.baseUrl}?${query}`);
  }

  get(id: number): Observable<ClinicListItem> {
    return this.http.get<ClinicListItem>(`${this.baseUrl}/${id}`);
  }

  create(data: ClinicCreateRequest): Observable<ClinicListItem> {
    return this.http.post<ClinicListItem>(this.baseUrl, data);
  }

  update(id: number, data: ClinicCreateRequest): Observable<ClinicListItem> {
    return this.http.put<ClinicListItem>(`${this.baseUrl}/${id}`, data);
  }

  toggleStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${id}/status`, isActive);
  }
}
