import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API = 'http://localhost:5035/api/departments';

export interface Department {
  id: number;
  name: string;
  code?: string;
  isActive: boolean;
}
export interface Paged<T> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class DepartmentApi {
  private http = inject(HttpClient);

  list(search = '', page = 1, pageSize = 20): Observable<Paged<Department>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<Paged<Department>>(API, { params });
  }
  get(id: number) {
    return this.http.get<Department>(`${API}/${id}`);
  }
  create(payload: Omit<Department, 'id'>) {
    return this.http.post<Department>(API, payload);
  }
  update(id: number, payload: Omit<Department, 'id'>) {
    return this.http.put<Department>(`${API}/${id}`, payload);
  }
  delete(id: number) {
    return this.http.delete(`${API}/${id}`);
  }
}
