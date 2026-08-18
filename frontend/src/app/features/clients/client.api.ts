import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API = 'http://localhost:5035/api/client';

export interface Client {
  id: number;
  name: string;
  code?: string;
  phone?: string;
  email?: string;
  address?: string;
  isActive: boolean;
}
export interface Paged<T> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class ClientApi {
  private http = inject(HttpClient);

  list(search = '', page = 1, pageSize = 10): Observable<Paged<Client>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<Paged<Client>>(API, { params });
  }
  get(id: number) {
    return this.http.get<Client>(`${API}/${id}`);
  }
  create(payload: Omit<Client, 'id'>) {
    return this.http.post<Client>(API, payload);
  }
  update(id: number, payload: Omit<Client, 'id'>) {
    return this.http.put<Client>(`${API}/${id}`, payload);
  }
  toggleStatus(id: number, isActive: boolean) {
    return this.http.patch(`${API}/${id}/status`, isActive);
  }
}
