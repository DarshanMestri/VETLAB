import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

// Match your backend port
const API = 'http://localhost:5035/api/species';

export interface Species {
  id: number;
  code: string;
  name: string;
}
export interface Paged<T> {
  total: number;
  items: T[];
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class SpeciesApi {
  private http = inject(HttpClient);

  list(search = '', page = 1, pageSize = 10): Observable<Paged<Species>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<Paged<Species>>(API, { params });
  }

  get(id: number) {
    return this.http.get<Species>(`${API}/${id}`);
  }
  create(payload: { code: string; name: string }) {
    return this.http.post<Species>(API, payload);
  }
  update(id: number, payload: { code: string; name: string }) {
    return this.http.put<Species>(`${API}/${id}`, payload);
  }
  delete(id: number) {
    return this.http.delete(`${API}/${id}`);
  }
}
