import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OrganizationListItem {
  id: number;
  code: string;
  name: string;
  legalName?: string | null;
  email?: string | null;
  phone?: string | null;
  address?: string | null;
  isActive: boolean;
}

@Injectable({ providedIn: 'root' })
export class OrganizationsService {
  private base = '/api/organizations';
  constructor(private http: HttpClient) {}

  list(search = '', page = 1, pageSize = 20): Observable<any> {
    return this.http.get<any>(`${this.base}`, { params: { search, page: page.toString(), pageSize: pageSize.toString() } });
  }

  get(id: number) {
    return this.http.get<OrganizationListItem>(`${this.base}/${id}`);
  }

  create(payload: any) {
    return this.http.post(this.base, payload);
  }

  update(id: number, payload: any) {
    return this.http.put(`${this.base}/${id}`, payload);
  }

  toggleStatus(id: number, isActive: boolean) {
    return this.http.patch(`${this.base}/${id}/status`, isActive);
  }
}
