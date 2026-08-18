import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API = 'http://localhost:5035/api/units';

export interface Unit {
  id: number;
  name: string;
  symbol?: string;
  isActive: boolean;
}
export interface Paged<T> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class UnitApi {
  private http = inject(HttpClient);

  list(search = '', page = 1, pageSize = 20): Observable<Paged<Unit>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<Paged<Unit>>(API, { params });
  }
  get(id: number) {
    return this.http.get<Unit>(`${API}/${id}`);
  }
  create(payload: Omit<Unit, 'id'>) {
    return this.http.post<Unit>(API, payload);
  }
  update(id: number, payload: Omit<Unit, 'id'>) {
    return this.http.put<Unit>(`${API}/${id}`, payload);
  }
  toggleStatus(id: number, isActive: boolean) {
    return this.http.patch(`${API}/${id}/status`, isActive);
  }
}
